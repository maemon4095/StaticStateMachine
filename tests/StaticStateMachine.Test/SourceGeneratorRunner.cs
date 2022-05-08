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
    public static SourceGeneratorRunner Create(Config config, ISourceGenerator generator)
    {
        return new(config, generator);
    }
    public static SourceGeneratorRunner Create(IIncrementalGenerator generator)
    {
        return Create(Config.Default, generator);
    }
    public static SourceGeneratorRunner Create(ISourceGenerator generator)
    {
        return Create(Config.Default, generator);
    }

    private SourceGeneratorRunner(Config config, object generator)
    {
        this.config = config;
        this.generator = generator;
    }

    readonly Config config;
    readonly object generator;

    public RunnerResult Run(string source)
    {
        var config = this.config;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, config.ParseOptions);
        var compilation = CSharpCompilation.Create(config.AssemblyName, new[] { syntaxTree }, config.References, config.CompilationOptions);
        var driver = this.generator switch
        {
            IIncrementalGenerator g => CSharpGeneratorDriver.Create(g),
            ISourceGenerator g => CSharpGeneratorDriver.Create(g),
            _=> throw new InvalidOperationException()
        };
        if (syntaxTree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) throw new ArgumentException("Source has syntax error", nameof(source));
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _).GetRunResult();
        return new RunnerResult(config, syntaxTree, outputCompilation, result.Results.First());
    }
}