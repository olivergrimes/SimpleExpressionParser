using System;
using System.Collections.Generic;

namespace SimpleExpressionParser
{
    public static class ExpressionParser
    {
        private const char OpenParen = '(';
        private const char CloseParen = ')';
        private const char ArgumentSeparator = ',';
        private const char NegativeToken = '-';
        private const string NegativeTokenStr = "-";

        public static SyntaxTreeNode Parse(string expression, ParseOptions? options = null)
        {
            options ??= new ParseOptions();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return new SyntaxTreeNode(
                     NodeType.Scope,
                     string.Empty,
                     false,
                     new List<SyntaxTreeNode>().AsReadOnly(),
                     new List<Operator>().AsReadOnly());
            }

            var stack = new Stack<ExpressionParserNode>();
            var chars = expression.ToCharArray();
            var startIndex = 0;
            var index = 0;
            var negated = false;
            char currentChar;

            void popAndPush()
            {
                var popped = stack.Pop();
                stack.Peek().AddChild(popped);
            }

            void closeFunctionParameter()
            {
                if (stack.Peek().ChildNodes.Count == 1)
                {
                    //Unwrap single parameter container context
                    var container = stack.Pop();

                    //These containers can never be negative
                    stack.Peek().AddChild(container.ChildNodes[0]);
                }
                else
                {
                    //Close off parameter container context
                    popAndPush();
                }
            }

            void readWhile(Func<bool> predicate)
            {
                startIndex = index;

                while (index < chars.Length && predicate())
                {
                    index++;
                };
            }

            //Create base context
            stack.Push(new ExpressionParserNode { Type = NodeType.Scope });

            while (true)
            {
                currentChar = chars[index];

                switch (currentChar)
                {
                    case OpenParen:
                        //Create a new empty context
                        stack.Push(new ExpressionParserNode
                        {
                            Type = NodeType.Scope,
                            IsParenthesis = true,
                            IsNegative = negated
                        });
                        negated = false;
                        index++;
                        break;
                    case CloseParen:
                        if (stack.Peek().IsRaisedPrecedence)
                        {
                            popAndPush(); //Close off raised precedence context
                            popAndPush(); //Close off parenthesis context
                        }
                        else if (stack.Peek().IsFunction && !stack.Peek().IsParenthesis)
                        {
                            if (stack.Peek().ChildNodes == null)
                            {
                                stack.Pop(); //Discard the empty function parameter container context
                            }
                            else
                            {
                                closeFunctionParameter();
                            }

                            popAndPush(); //Close off function context
                        }
                        else
                        {
                            if (stack.Peek().ChildNodes.Count == 1)
                            {
                                //Unwrap single node parenthesis generated context
                                var container = stack.Pop();
                                container.ChildNodes[0].IsNegative = container.IsNegative;
                                stack.Peek().AddChild(container.ChildNodes[0]);
                            }
                            else
                            {
                                popAndPush(); //Close off parenthesis generated context
                            }
                        }

                        index++;
                        break;
                    case ArgumentSeparator:

                        closeFunctionParameter();

                        //Container context for parameter expressions, will be removed if unused
                        stack.Push(new ExpressionParserNode
                        {
                            Type = NodeType.Scope,
                            IsFunction = true
                            //Not possible for container context to be negative
                        });

                        index++;
                        break;
                    case NegativeToken:
                        //Same number of operators as nodes; treat additional '-' as unary -ve
                        if (stack.Peek().Operators?.Count == stack.Peek().ChildNodes?.Count)
                        {
                            negated = !negated;
                        }
                        else //Treat as operator
                        {
                            //Close the raised precedence context
                            if (stack.Peek().IsRaisedPrecedence)
                            {
                                popAndPush();
                            }

                            //Append operator onto current context
                            stack.Peek().AddOperator(new Operator(NegativeTokenStr));
                        }
                        index++;
                        break;
                    default:
                        if (IsOperatorCharacter(currentChar))
                        {
                            readWhile(() => IsOperatorCharacter(chars[index]));
                            string @operator = SubstringFromCharArray(chars, startIndex, index - startIndex);

                            //Close the raised precedence context
                            if (!OperatorsWithRaisedPrecedence.Contains(@operator) &&
                                stack.Peek().IsRaisedPrecedence)
                            {
                                popAndPush();
                            }

                            //Should the operator have raised precedence
                            if (IsRaisedPrecedenceOperator(stack.Peek(), @operator))
                            {
                                var raisedPrecedence = new ExpressionParserNode
                                {
                                    Type = NodeType.Scope,
                                    IsRaisedPrecedence = true
                                    //Not possible for generated context to be negative
                                };

                                //Pull previous node into raised context
                                raisedPrecedence.AddChild(stack.Peek().PopChild());
                                raisedPrecedence.AddOperator(new Operator(@operator));
                                stack.Push(raisedPrecedence);
                            }
                            else
                            {
                                //Append operator onto current context
                                stack.Peek().AddOperator(new Operator(@operator));
                            }
                        }
                        else if (char.IsNumber(currentChar))
                        {
                            readWhile(() => char.IsNumber(chars[index]) || chars[index] == options.DecimalMarker);
                            stack.Peek().AddChild(new ExpressionParserNode
                            {
                                Value = SubstringFromCharArray(chars, startIndex, index - startIndex),
                                Type = NodeType.Constant,
                                IsNegative = negated
                            });

                            negated = false;
                        }
                        else if (char.IsWhiteSpace(currentChar))
                        {
                            //Skip whitespace
                            index++;
                        }
                        else
                        {
                            //If we get to this branch we can assume this is the start of a function or variable
                            readWhile(() => IsParameterCharacter(chars[index]));

                            if (index < chars.Length && chars[index] == OpenParen)
                            {
                                //Create a new context for the function
                                stack.Push(new ExpressionParserNode
                                {
                                    Value = SubstringFromCharArray(chars, startIndex, index - startIndex),
                                    Type = NodeType.Function,
                                    IsNegative = negated,
                                    IsFunction = true
                                });

                                //Container context for parameter expressions, will be removed if unused
                                stack.Push(new ExpressionParserNode
                                {
                                    Type = NodeType.Scope,
                                    IsFunction = true
                                    //Not possible for container context to be negative
                                });

                                negated = false;
                                index++;
                            }
                            else
                            {
                                //Variable
                                stack.Peek().AddChild(new ExpressionParserNode
                                {
                                    Value = SubstringFromCharArray(chars, startIndex, index - startIndex),
                                    Type = NodeType.Variable,
                                    IsNegative = negated
                                });

                                negated = false;
                                break;
                            }
                        }
                        break;
                }

                //Check for end of expression
                if (index >= chars.Length)
                {
                    while (stack.Count > 1)
                    {
                        popAndPush();
                    }

                    var rootNode = stack.Pop();

                    return ConvertTree(rootNode);
                }
            }
        }

        private static SyntaxTreeNode ConvertTree(ExpressionParserNode parsingNode)
        {
            List<SyntaxTreeNode> childNodes = new List<SyntaxTreeNode>();

            if (parsingNode.ChildNodes != null)
            {
                foreach (var childNode in parsingNode.ChildNodes)
                {
                    childNodes.Add(ConvertTree(childNode));
                }
            }

            return new SyntaxTreeNode(
                type: parsingNode.Type,
                value: parsingNode.Value,
                isNegated: parsingNode.IsNegative,
                operators: parsingNode.Operators,
                childNodes: childNodes.AsReadOnly());
        }

        private static string SubstringFromCharArray(char[] chars, int startIndex, int len)
        {
            char[] substr = new char[len];
            for (int i = 0; i < len; i++)
            {
                substr[i] = chars[startIndex + i];
            }

            return new string(substr);
        }

        private static readonly HashSet<char> OperatorCharacters = new HashSet<char>(new[] { '+', '/', '*', '%' });
        private static bool IsOperatorCharacter(char character)
        {
            return OperatorCharacters.Contains(character);
        }

        private static readonly HashSet<char> ReservedCharacters = new HashSet<char>(new[] { OpenParen, CloseParen, ArgumentSeparator, NegativeToken });
        private static bool IsParameterCharacter(char character)
        {
            return !OperatorCharacters.Contains(character) && !ReservedCharacters.Contains(character);
        }

        private static readonly HashSet<string> OperatorsWithRaisedPrecedence = new HashSet<string>(new[] { "*", "/", "%" });
        private static bool IsRaisedPrecedenceOperator(ExpressionParserNode current, string @operator)
        {
            var hasPreceedingLower = current.Operators?.Count > 0 &&
                !OperatorsWithRaisedPrecedence.Contains(current.Operators[current.Operators.Count - 1].Symbol);

            return hasPreceedingLower && OperatorsWithRaisedPrecedence.Contains(@operator);
        }
    }
}
