# SimpleExpressionParser

Fast, simple netstandard2.0 numeric expression parser.  Parses expressions into syntax trees for further processing.  

### Supported Syntax

- **Numeric constants:** `1+1`
- **Common numeric operators:** `+, -, *, /, %`
- **Precedence:** `2+2*2`
- **Parenthesis:** `(2+2)*2`
- **Variables:** `1+variableName`
- **Functions:** `1+functionName(args)`
