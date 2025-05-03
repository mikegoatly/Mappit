using System;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about property mapping
    /// </summary>
    internal sealed class MappingMemberInfo
    {
        public MappingMemberInfo(string? sourceName, string? targetName, AttributeSyntax attributeSyntaxNode)
        {
            if (string.IsNullOrEmpty(sourceName))
            {
                throw new ArgumentException("Source name cannot be null or empty.", nameof(sourceName));
            }

            if (string.IsNullOrEmpty(targetName))
            {
                throw new ArgumentException("Target name cannot be null or empty.", nameof(targetName));
            }

            SourceName = sourceName!;
            TargetName = targetName!;
            AttributeSyntaxNode = attributeSyntaxNode;
            SourceArgument = attributeSyntaxNode.ArgumentList!.Arguments[0];
            TargetArgument = attributeSyntaxNode.ArgumentList!.Arguments[1];
        }
        
        public string SourceName { get; }
        public string TargetName { get; }
        public AttributeSyntax AttributeSyntaxNode { get; }
        public AttributeArgumentSyntax SourceArgument { get; }
        public AttributeArgumentSyntax TargetArgument { get; }
    }
}