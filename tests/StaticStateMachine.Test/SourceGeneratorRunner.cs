using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;

namespace StaticStateMachine.Test;

readonly partial struct SourceGeneratorRunner
{
    public static SourceGeneratorRunner Create(Config config, Func<IIncrementalGenerator> generatorSource)
    {
        return new(config, generatorSource);
    }
    public static SourceGeneratorRunner Create(Config config, Func<ISourceGenerator> generatorSource)
    {
        return new(config, generatorSource);
    }
    public static SourceGeneratorRunner Create(Func<IIncrementalGenerator> generatorSource)
    {
        return Create(Config.Default, generatorSource);
    }
    public static SourceGeneratorRunner Create(Func<ISourceGenerator> generatorSource)
    {
        return Create(Config.Default, generatorSource);
    }

    private SourceGeneratorRunner(Config config, Func<object> generator)
    {
        this.config = config;
        this.generatorSource = generator;
    }

    readonly Config config;
    readonly Func<object> generatorSource;

    public RunnerResult Run(string source)
    {
        var config = this.config;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, config.ParseOptions);
        var compilation = CSharpCompilation.Create(config.AssemblyName, new[] { syntaxTree }, config.References, config.CompilationOptions);
        var driver = this.generatorSource() switch
        {
            IIncrementalGenerator g => CSharpGeneratorDriver.Create(g),
            ISourceGenerator g => CSharpGeneratorDriver.Create(g),
            _ => throw new InvalidOperationException()
        };
        if (syntaxTree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) throw new ArgumentException("Source has syntax error", nameof(source));
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _).GetRunResult();
        return new RunnerResult(config, syntaxTree, outputCompilation, result.Results.First());
    }
}