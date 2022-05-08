using Microsoft.CodeAnalysis;
using System.Linq;
using System;
using System.Collections.Generic;
namespace StaticStateMachine.Test;

readonly partial struct SourceGeneratorRunner
{
    public readonly struct RunnerResult
    {
        public RunnerResult(Config config, SyntaxTree sourceSyntaxTree, Compilation compilation, GeneratorRunResult result)
        {
            this.Config = config;
            this.SourceSyntaxTree = sourceSyntaxTree;
            this.Compilation = compilation;
            this.GeneratorResult = result; 
        }

        public Config Config { get; }
        public SyntaxTree SourceSyntaxTree { get; }
        public Compilation Compilation { get; }
        public GeneratorRunResult GeneratorResult { get; }
        public bool Succeeded => this.GetAllDiagnostics().Verify();
        public IEnumerable<Diagnostic> GetAllDiagnostics() => this.GeneratorResult.Diagnostics.Concat(this.Compilation.GetDiagnostics());

        public void Verify(Action<RunnerResult> validator)
        {
            validator(this);
        }
    }
}