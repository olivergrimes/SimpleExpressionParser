namespace SimpleExpressionParser
{
    public class Operator
    {
        public string Symbol { get; }

        public Operator(string symbol)
        {
            Symbol = symbol;
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}
