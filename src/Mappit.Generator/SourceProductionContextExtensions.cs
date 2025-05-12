using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal static class SourceProductionContextExtensions
    {
        internal static void ReportDiagnostic(
            this SourceProductionContext context,
            MappitErrorCode errorCode,
            string message,
            SyntaxNode node)
        {
            var (id, title) = ErrorCodes.GetError(errorCode);
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: message,
                category: "Mappit",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            var location = Location.Create(node.SyntaxTree, node.Span);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
        }
    }
}
