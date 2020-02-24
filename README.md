# SimpleExpressionParser

[![Build Status](https://olivergrimes.visualstudio.com/olivergrimes-github-ci/_apis/build/status/olivergrimes.SimpleExpressionParser?branchName=master)](https://olivergrimes.visualstudio.com/olivergrimes-github-ci/_build/latest?definitionId=7&branchName=master) [![Nuget](https://img.shields.io/nuget/v/SimpleExpressionParser)](https://www.nuget.org/packages/SimpleExpressionParser/)

Fast, simple netstandard2.0 numeric expression parser.  Parses expressions into syntax trees that can be used for further processing.  

### Supported Syntax

- **Numeric constants (with decimals):** `1, 1.1`
- **Common numeric operators:** `+, -, *, /, %`
- **Unary negative:** `-`
- **Precedence:** `2+2*2`
- **Parenthesis:** `(2+2)*2`
- **Variables:** `1+variableName`
- **Functions:** `1+functionName(args)`
