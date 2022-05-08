using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;
namespace StaticStateMachine.Test;

readonly partial struct SourceGeneratorRunner
{
    public readonly struct Config
    {
        public static Config Default
        {
            get
            {
                return new Config()
                {
                    ParseOptions = CSharpParseOptions.Default,
                    //see https://gist.github.com/chsienki/2955ed9336d7eb22bcb246840bfeb05c#file-generatortests-cs
                    References = new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                    CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    AssemblyName = "SourceGeneratorTest",
                };
            }
        }

        public CSharpParseOptions? ParseOptions { get; init; }
        public IEnumerable<MetadataReference> References { get; init; }
        public CSharpCompilationOptions? CompilationOptions { get; init; }
        public string AssemblyName { get; init; }
    }
}
