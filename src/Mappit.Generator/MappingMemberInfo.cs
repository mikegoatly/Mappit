using System;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about property mapping
    /// </summary>
    internal sealed record MappingMemberInfo
    {
        public MappingMemberInfo(string sourceName, string targetName, AttributeSyntax attributeSyntaxNode)
        {
            SourceName = sourceName;
            TargetName = targetName;
            SyntaxNode = attributeSyntaxNode;

            SourceArgument = attributeSyntaxNode.ArgumentList!.Arguments[0];
            TargetArgument = attributeSyntaxNode.ArgumentList!.Arguments[1];
        }

        public MappingMemberInfo(string sourceName, string targetName, SyntaxNode syntaxNode)
        {
            SourceName = sourceName!;
            TargetName = targetName!;
            SyntaxNode = syntaxNode;
            SourceArgument = syntaxNode;
            TargetArgument = syntaxNode;
        }

        public string SourceName { get; init; }
        public string TargetName { get; init; }

        /// <summary>
        /// The main syntax node associated to this mapping - this may be an attribute containing a custom
        /// mapping, or the property itself if its for an implicit mapping where property names match.
        /// </summary>
        public SyntaxNode SyntaxNode { get; init; }

        /// <summary>
        /// The syntax node associated to the source mapping. For implicit mappings this is the same
        /// as <see cref="SyntaxNode"/>.
        /// </summary>
        public SyntaxNode SourceArgument { get; init; }

        /// <summary>
        /// The syntax node associated to the target mapping. For implicit mappings this is the same
        /// as <see cref="SyntaxNode"/>.
        /// </summary>
        public SyntaxNode TargetArgument { get; init; }

        /// <summary>
        /// The name of the method to use for converting the source value to the target value.
        /// </summary>
        public IMethodSymbol? ValueConversionMethod { get; internal set; }
    }
}