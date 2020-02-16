using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SimpleExpressionParser.Tests
{
    public static class ExpressionComponentTestExtensions
    {
        public static SyntaxTreeNode Component(this SyntaxTreeNode component, int index, Action<SyntaxTreeNode> childTest)
        {
            if (component.ChildNodes.Count <= index)
            {
                throw new Exception($"No component found at {index}");
            }

            childTest(component.ChildNodes[index]);
            return component;
        }

        public static SyntaxTreeNode OperatorsShouldBe(this SyntaxTreeNode component, params string[] operators)
        {
            int i = 0;
            if (component.Operators.Count != operators.Length)
            {
                throw new Exception($"Operators: {string.Join(",", operators)} expected, actual: {string.Join(",", component.Operators)}");
            }
            foreach (var @operator in component.Operators)
            {
                Assert.AreEqual(operators[i], @operator.Symbol);
                i++;
            }

            return component;
        }

        public static SyntaxTreeNode ShouldHave(this SyntaxTreeNode component,
            int? childCountOf = null,
            string tokenOf = "",
            NodeType? typeOf = null,
            bool? negativeOf = null)
        {
            if (childCountOf != null)
            {
                if (component.ChildNodes == null && childCountOf > 0)
                {
                    throw new Exception($"No components available.");
                }
                else
                {
                    Assert.AreEqual(childCountOf, component.ChildNodes?.Count);
                }
            }

            if (!string.IsNullOrEmpty(tokenOf))
            {
                Assert.AreEqual(tokenOf, component.Value);
            }

            if (typeOf != null)
            {
                Assert.AreEqual(typeOf, component.Type);
            }

            if (negativeOf != null)
            {
                Assert.AreEqual(negativeOf, component.IsNegated);
            }

            return component;
        }
    }

    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void ParametersAndOperator_Simple()
        {
            var expression = "A+B";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("+")
                .ShouldHave(typeOf: NodeType.Scope, childCountOf: 2)
                .Component(0, c => c.ShouldHave(tokenOf: "A", typeOf: NodeType.Variable))
                .Component(1, c => c.ShouldHave(tokenOf: "B", typeOf: NodeType.Variable));
        }

        [TestMethod]
        public void ParametersAndOperator_WithPrecedence()
        {
            var expression = "A+B*C";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("+")
                .ShouldHave(childCountOf: 2, typeOf: NodeType.Scope)
                .Component(0, c => c.ShouldHave(tokenOf: "A", typeOf: NodeType.Variable))
                .Component(1, c => c.ShouldHave(childCountOf: 2, typeOf: NodeType.Scope)
                    .OperatorsShouldBe("*")
                    .Component(0, c2 => c2.ShouldHave(tokenOf: "B", typeOf: NodeType.Variable))
                    .Component(1, c2 => c2.ShouldHave(tokenOf: "C", typeOf: NodeType.Variable)));
        }

        [TestMethod]
        public void ParametersPrecedenceFunctionWithExpression()
        {
            var expression = "A+B*test(C+D)";
            var parsed = ExpressionParser.Parse(expression);

            parsed.OperatorsShouldBe("+")
                .ShouldHave(typeOf: NodeType.Scope, childCountOf: 2)
                .Component(0, c => c.ShouldHave(typeOf: NodeType.Variable, tokenOf: "A"))
                .Component(1, c => c.OperatorsShouldBe("*")
                    .ShouldHave(childCountOf: 2, typeOf: NodeType.Scope)
                    .Component(0, c2 => c2.ShouldHave(typeOf: NodeType.Variable, tokenOf: "B"))
                    .Component(1, c2 => c2.ShouldHave(typeOf: NodeType.Function, tokenOf: "test", childCountOf: 1)
                        .Component(0, c3 => c3.ShouldHave(typeOf: NodeType.Scope, childCountOf: 2)
                            .OperatorsShouldBe("+")
                            .Component(0, c4 => c4.ShouldHave(tokenOf: "C", typeOf: NodeType.Variable))
                            .Component(1, c4 => c4.ShouldHave(tokenOf: "D", typeOf: NodeType.Variable)))));
        }

        [TestMethod]
        public void FunctionTwoExpressionArguments()
        {
            var expression = "test(C-D,E*6)";
            var parsed = ExpressionParser.Parse(expression);

            parsed.Component(0, c => c
                .ShouldHave(typeOf: NodeType.Function, childCountOf: 2, tokenOf: "test")
                .Component(0, c2 => c2
                    .ShouldHave(typeOf: NodeType.Scope, childCountOf: 2)
                    .OperatorsShouldBe("-"))
                .Component(1, c2 => c2
                    .ShouldHave(typeOf: NodeType.Scope, childCountOf: 2)
                    .OperatorsShouldBe("*")));
        }

        [TestMethod]
        public void FunctionNoArguments()
        {
            var expression = "test()";
            var parsed = ExpressionParser.Parse(expression);

            parsed.Component(0, c => c
                .ShouldHave(childCountOf: 1, tokenOf: "test", typeOf: NodeType.Function));
        }

        [TestMethod]
        public void FunctionsWithinFunctions()
        {
            var expression = "test(func(within(func()),2.1))";
            var parsed = ExpressionParser.Parse(expression);

            parsed.Component(0, c => c.ShouldHave(1, "test", NodeType.Function)
                .Component(0, c2 => c2.ShouldHave(2, "func", NodeType.Function)
                    .Component(0, c3 => c3.ShouldHave(1, "within", NodeType.Function)
                        .Component(0, c4 => c4.ShouldHave(null, "func", NodeType.Function)))
                    .Component(1, c3 => c3.ShouldHave(null, "2.1", NodeType.Constant))));
        }

        [TestMethod]
        public void ExpressionWithConstantAtEnd()
        {
            var expression = "A--2.333";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("-")
                .Component(0, c => c.ShouldHave(null, "A", NodeType.Variable))
                .Component(1, c => c.ShouldHave(null, "2.333", NodeType.Constant, true));
        }

        [TestMethod]
        public void ParenthesisOverridingPrecedence()
        {
            var expression = "1*(1-1)*1";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("*", "*")
                .ShouldHave(childCountOf: 3, typeOf: NodeType.Scope)
                .Component(0, c => c.ShouldHave(null, "1", NodeType.Constant))
                .Component(1, c => c
                    .OperatorsShouldBe("-")
                    .ShouldHave(childCountOf: 2, typeOf: NodeType.Scope)
                    .Component(0, c2 => c2.ShouldHave(null, "1", NodeType.Constant))
                    .Component(1, c2 => c2.ShouldHave(null, "1", NodeType.Constant)));
        }

        [TestMethod]
        public void FunctionMultipleParametersInclParameterlessFunction()
        {
            var expression = "func1(func2(),2)";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .Component(0, c => c.ShouldHave(2, "func1", NodeType.Function)
                    .Component(0, c2 => c2.ShouldHave(null, "func2", NodeType.Function))
                    .Component(1, c2 => c2.ShouldHave(null, "2", NodeType.Constant)));
        }

        [TestMethod]
        public void NumericAfterVariable_NoOperator()
        {
            var expression = "7vari";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe(new string[0])
                .ShouldHave(childCountOf: 2, typeOf: NodeType.Scope)
                    .Component(0, c => c.ShouldHave(null, "7", NodeType.Constant))
                    .Component(1, c => c.ShouldHave(null, "vari", NodeType.Variable));
        }

        [TestMethod]
        public void RepeatedNonNegativeOperatorsConcatenated()
        {
            var expression = "5+6++7";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("+", "++")
                .ShouldHave(childCountOf: 3, typeOf: NodeType.Scope);
        }

        [TestMethod]
        public void TrailingOperatorsIncluded()
        {
            var expression = "5+6++7+";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("+", "++", "+")
                .ShouldHave(childCountOf: 3, typeOf: NodeType.Scope);
        }

        [TestMethod]
        public void MultipleLevelsOfPrecedence()
        {
            var expression = "1+2*3*4+5*6/7";
            var parsed = ExpressionParser.Parse(expression);

            parsed.OperatorsShouldBe("+", "+")
                .ShouldHave(childCountOf: 3, typeOf: NodeType.Scope)
                .Component(1, c => c.OperatorsShouldBe("*", "*")
                    .ShouldHave(childCountOf: 3, typeOf: NodeType.Scope))
                .Component(2, c => c.OperatorsShouldBe("*", "/")
                    .ShouldHave(childCountOf: 3, typeOf: NodeType.Scope));
        }

        [TestMethod]
        public void UnrequiredParensIgnored()
        {
            var expression = "1+-((2))";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("+")
                .ShouldHave(childCountOf: 2, typeOf: NodeType.Scope)
                .Component(1, c => c
                    .ShouldHave(null, "2", NodeType.Constant, true));
        }

        [TestMethod]
        public void EmptyExpressionReturnsNull()
        {
            var expression = "";
            var parsed = ExpressionParser.Parse(expression);

            Assert.AreEqual(NodeType.Scope, parsed.Type);
            Assert.AreEqual(string.Empty, parsed.Value);

        }

        [TestMethod]
        public void RandomTest()
        {
            string expression = @"10    %2+ 1*(32+45/(AAA+9)) /324.234-
                func(-ello,(5+2)* 3)*(func2()+func3(1     ,2,3))";

            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("%", "+", "-")
                .ShouldHave(4, string.Empty, NodeType.Scope)
                .Component(0, c => c.ShouldHave(tokenOf: "10", typeOf: NodeType.Constant))
                .Component(1, c => c.ShouldHave(tokenOf: "2", typeOf: NodeType.Constant))
                .Component(2, c => c
                    .OperatorsShouldBe("*", "/")
                    .ShouldHave(3, string.Empty, NodeType.Scope)
                    .Component(0, c2 => c2.ShouldHave(null, "1", NodeType.Constant))
                    .Component(1, c2 => c2
                        .OperatorsShouldBe("+")
                        .ShouldHave(2, string.Empty, NodeType.Scope)
                        .Component(0, c3 => c3.ShouldHave(null, "32", NodeType.Constant))
                        .Component(1, c3 => c3
                            .OperatorsShouldBe("/")
                            .ShouldHave(2, string.Empty, NodeType.Scope)
                            .Component(0, c4 => c4.ShouldHave(null, "45", NodeType.Constant))
                            .Component(1, c4 => c4
                                .OperatorsShouldBe("+")
                                .ShouldHave(2, string.Empty, NodeType.Scope)
                                .Component(0, c5 => c5.ShouldHave(null, "AAA", NodeType.Variable))
                                .Component(1, c5 => c5.ShouldHave(null, "9", NodeType.Constant)))))
                    .Component(2, c2 => c2.ShouldHave(null, "324.234", NodeType.Constant)))
                .Component(3, c => c
                    .OperatorsShouldBe("*")
                    .ShouldHave(2, string.Empty, NodeType.Scope)
                    .Component(0, c2 => c2
                        .ShouldHave(2, "func", NodeType.Function)
                        .Component(0, c3 => c3.ShouldHave(null, "ello", NodeType.Variable, true))
                        .Component(1, c3 => c3
                            .OperatorsShouldBe("*")
                            .ShouldHave(2, string.Empty, NodeType.Scope)
                            .Component(0, c4 => c4
                                .OperatorsShouldBe("+")
                                .ShouldHave(2, string.Empty, NodeType.Scope)
                                .Component(0, c5 => c5.ShouldHave(null, "5", NodeType.Constant))
                                .Component(1, c5 => c5.ShouldHave(null, "2", NodeType.Constant)))
                            .Component(1, c4 => c4.ShouldHave(null, "3", NodeType.Constant))))
                    .Component(1, c2 => c2
                        .OperatorsShouldBe("+")
                        .ShouldHave(2, string.Empty, NodeType.Scope)
                        .Component(0, c3 => c3.ShouldHave(null, "func2", NodeType.Function))
                        .Component(1, c3 => c3
                            .ShouldHave(3, "func3", NodeType.Function)
                            .Component(0, c4 => c4.ShouldHave(null, "1", NodeType.Constant))
                            .Component(1, c4 => c4.ShouldHave(null, "2", NodeType.Constant))
                            .Component(2, c4 => c4.ShouldHave(null, "3", NodeType.Constant)))));
        }

        [TestMethod]
        public void ExpressionWithParensAsFunctionArgument()
        {
            //TODO: Better way of removing contexts created for function parameter expressions
            string expression = "-func(A,(1+1)*1)";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .Component(0, c => c.ShouldHave(2, "func", NodeType.Function, true)
                    .Component(0, c2 => c2.ShouldHave(null, "A", NodeType.Variable))
                    .Component(1, c2 => c2
                        .OperatorsShouldBe("*")
                        .ShouldHave(2, string.Empty, NodeType.Scope)
                        .Component(0, c3 => c3
                            .OperatorsShouldBe("+")
                            .Component(0, c4 => c4.ShouldHave(null, "1", NodeType.Constant))
                            .Component(1, c4 => c4.ShouldHave(null, "1", NodeType.Constant)))
                        .Component(1, c3 => c3.ShouldHave(null, "1", NodeType.Constant))));
        }

        [TestMethod]
        public void DoubleEndingParenthesis()
        {
            string expression = "1+-(2+(3+--(4+5)))";
            var parsed = ExpressionParser.Parse(expression);

            parsed
                .OperatorsShouldBe("+")
                .ShouldHave(2, string.Empty, NodeType.Scope)
                .Component(0, c => c.ShouldHave(null, "1", NodeType.Constant))
                .Component(1, c => c
                    .OperatorsShouldBe("+")
                    .ShouldHave(2, string.Empty, NodeType.Scope, true)
                        .Component(0, c2 => c2.ShouldHave(null, "2", NodeType.Constant))
                        .Component(1, c2 => c2
                            .OperatorsShouldBe("+")
                            .ShouldHave(2, string.Empty, NodeType.Scope)
                                .Component(0, c3 => c3.ShouldHave(null, "3", NodeType.Constant))
                                .Component(1, c3 => c3
                                    .OperatorsShouldBe("+")
                                    .ShouldHave(2, string.Empty, NodeType.Scope, false)
                                    .Component(0, c4 => c4.ShouldHave(null, "4", NodeType.Constant))
                                    .Component(1, c4 => c4.ShouldHave(null, "5", NodeType.Constant)))));
        }
    }
}
