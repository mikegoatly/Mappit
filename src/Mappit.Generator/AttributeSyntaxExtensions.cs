using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mappit.Generator
{
    internal static class AttributeSyntaxExtensions
    {
        internal static string? GetArgumentString(
            this AttributeSyntax? attribute,
            SemanticModel semanticModel,
            string argumentName,
            int? position = null)
        {
            var arguments = attribute?.ArgumentList;
            var argument = arguments?.Arguments.FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == argumentName || a.NameColon?.Name.Identifier.Text == argumentName);
            argument ??= position is { } pos ? arguments?.Arguments.Where(x => x.NameEquals is null).ElementAtOrDefault(pos) : null;
            if (argument is null)
            {
                return null;
            }

            // If it's a simple literal, just return its string representation
            if (argument.Expression is LiteralExpressionSyntax literal)
            {
                return literal.Token.ValueText;
            }

            // Otherwise, try to get the constant value from semantic model
            var constantValue = semanticModel.GetConstantValue(argument.Expression);
            if (constantValue.HasValue && constantValue.Value is string stringValue)
            {
                return stringValue;
            }

            // If we can't determine the value, return the expression text as a fallback
            return argument.Expression.ToString();
        }
    }
}
