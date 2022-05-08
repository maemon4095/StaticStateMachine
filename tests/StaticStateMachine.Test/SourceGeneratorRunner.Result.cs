using Microsoft.CodeAnalysis;
using System.Linq;
using System;

using System.Collections.Immutable;
using System.Collections.Generic;

namespace StaticStateMachine.Test;

readonly partial struct SourceGeneratorRunner
{
    public readonly struct Result
    {
        Result(Config config, SyntaxTree syntaxTree, Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics, object resultOrException)
        {
            this.Config = config;
            this.SourceSyntaxTree = syntaxTree;
            this.Compilation = compilation;
            this.DriverResultOrException = resultOrException;
            this.GeneratorDiagnostics = generatorDiagnostics;
        }

        public Result(Config config, SyntaxTree syntaxTree, Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics, GeneratorDriverRunResult result)
            : this(config, syntaxTree, compilation, generatorDiagnostics, (object)result)
        {

        }
        public Result(Config config, SyntaxTree syntaxTree, Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics, Exception exception)
            : this(config, syntaxTree, compilation, generatorDiagnostics, (object)exception)
        {

        }

        public Config Config { get; }
        public SyntaxTree SourceSyntaxTree { get; }
        public Compilation Compilation { get; }
        public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }
        private readonly object DriverResultOrException;
        public GeneratorDriverRunResult DriverResult => this.DriverResultOrException as GeneratorDriverRunResult ?? throw new InvalidOperationException();
        public Exception? Exception => this.DriverResultOrException as Exception;
        public bool Succeeded
        {
            get
            {
                if (this.Exception is not null) return false;
                return this.GetAllDiagnostics().Validate();
            }
        }
        public IEnumerable<Diagnostic> GetAllDiagnostics()
        {
            var exceptDriver = this.GeneratorDiagnostics.Concat(this.Compilation.GetDiagnostics());
            if (this.Exception is not null) return exceptDriver;
            return exceptDriver.Concat(this.DriverResult.Diagnostics);
        }

        public void Validate(GeneratorValidator validator)
        {
            validator(this);
        }
    }
}