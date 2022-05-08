using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;

namespace StaticStateMachine.Test;

readonly partial struct SourceGeneratorRunner
{
    public static SourceGeneratorRunner Create(Config config, IIncrementalGenerator generator)
    {
        return new(config, generator);
    }

    public static SourceGeneratorRunner Create<T>(Config config)
        where T : IIncrementalGenerator, new()
    {
        return new(config, new T());

    }
    public static SourceGeneratorRunner Create<T>() where T : IIncrementalGenerator, new() => Create<T>(Config.Default);


    private SourceGeneratorRunner(Config config, IIncrementalGenerator generator)
    {
        this.config = config;
        this.generator = generator;
    }

    readonly Config config;
    readonly IIncrementalGenerator generator;

    public RunnerResult Run(string source)
    {
        var config = this.config;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, config.ParseOptions);
        var compilation = CSharpCompilation.Create(config.AssemblyName, new[] { syntaxTree }, config.References, config.CompilationOptions);
        var driver = CSharpGeneratorDriver.Create(this.generator);
        if (syntaxTree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) throw new ArgumentException("Source has syntax error", nameof(source));
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _).GetRunResult();
        return new RunnerResult(config, syntaxTree, outputCompilation, result.Results.First());
    }
}