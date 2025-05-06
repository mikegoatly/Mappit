using System;
using System.IO;

namespace Mappit.Generator.CodeSnippets
{
    internal static class CodeSnippetProvider
    {
        public static string GetCodeSnippet(string fileName)
        {
            var thisType = typeof(CodeSnippetProvider);
            using var stream = thisType.Assembly
                .GetManifestResourceStream(thisType, $"{fileName}");

            if (stream == null)
            {
                throw new InvalidOperationException($"Resource '{fileName}' not found.");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}