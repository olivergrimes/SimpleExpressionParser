using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SimpleExpressionParser
{
    internal sealed class ExpressionParserNode
    {
        private List<ExpressionParserNode> _childNodes = new List<ExpressionParserNode>();

        private List<Operator> _operators = new List<Operator>();

        internal bool IsRaisedPrecedence { get; set; }

        internal bool IsParenthesis { get; set; }

        internal bool IsFunction { get; set; }

        internal string Value { get; set; } = string.Empty;

        internal NodeType Type { get; set; }

        internal bool IsNegative { get; set; }

        internal ReadOnlyCollection<ExpressionParserNode> ChildNodes
        {
            get
            {
                return _childNodes.AsReadOnly();
            }
        }

        internal ReadOnlyCollection<Operator> Operators
        {
            get
            {
                return _operators.AsReadOnly();
            }
        }

        internal void AddChild(ExpressionParserNode node)
        {
            if (_childNodes == null)
            {
                _childNodes = new List<ExpressionParserNode>();
            }

            _childNodes.Add(node);
        }

        internal void AddOperator(Operator op)
        {
            if (_operators == null)
            {
                _operators = new List<Operator>();
            }

            _operators.Add(op);
        }

        internal ExpressionParserNode PopChild()
        {
            if (_childNodes == null)
            {
                throw new Exception("Error parsing expression.");
            }

            var lastIndex = _childNodes.Count - 1;
            var last = _childNodes[lastIndex];

            _childNodes.RemoveAt(lastIndex);

            return last;
        }

        public override string ToString()
        {
            return $"{Type}: {Value}";
        }
    }
}
