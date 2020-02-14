using System.Collections.ObjectModel;

namespace SimpleExpressionParser
{
    public sealed class SyntaxTreeNode
    {
        public string Value { get; }

        public NodeType Type { get; }

        public bool IsNegated { get; }

        public ReadOnlyCollection<SyntaxTreeNode> ChildNodes { get; }

        public ReadOnlyCollection<Operator> Operators { get; }

        public SyntaxTreeNode(
            NodeType type,
            string value,
            bool isNegated,
            ReadOnlyCollection<SyntaxTreeNode> childNodes,
            ReadOnlyCollection<Operator> operators)
        {
            Type = type;
            Value = value;
            IsNegated = isNegated;
            ChildNodes = childNodes;
            Operators = operators;
        }
    }
}
