using StaticStateMachine.Generator;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneratorRunner;
using SourceGeneratorRunner.Testing;

namespace StaticStateMachine.Test;

public class StateMachineTest
{
    public StateMachineTest(ITestOutputHelper helper)
    {
        this.helper = helper;
        this.runner = GeneratorRunner.Create(() => new StaticStateMachineGenerator());
    }

    readonly ITestOutputHelper helper;
    readonly GeneratorRunner runner;

    [Fact]
    public void EmptyAssociation()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine]
partial struct A
{
}";
        this.runner.Run(source).Verify(result =>
        {
            Assert.True(result.Succeeded);
        });
    }

    [Fact]
    public void EmptyPattern()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine]
[Association("""", 'a')]
partial struct A
{
}";
        this.runner.Run(source).Verify(result =>
        {
            Assert.True(result.Succeeded);
        });
    }

    [Fact]
    public void StringPattern()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine]
[Association(""abc"", 0)]
[Association(""abd"", 1)]
partial struct A
{
}";
        this.runner.Run(source).Verify(result =>
        {
            var type = result.Compilation.GetTypeByMetadataName("A")!;
            var typeArgs = type.Interfaces.Single().TypeArguments;

            Assert.Equal(result.Compilation.GetSpecialType(SpecialType.System_Char), typeArgs[0], SymbolEqualityComparer.Default);
            Assert.Equal(result.Compilation.GetSpecialType(SpecialType.System_Int32), typeArgs[1], SymbolEqualityComparer.Default);

            Assert.True(result.Succeeded);
        });
    }

    [Fact]
    public void Typewise()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine(StateMachineCategory.TypeWise)]
[Association(new[] { 0, 1, 2 }, 0)]
[Association(new[] { 0.0, 0, 1 } , 1)]
partial struct A
{
}";
        this.runner.Run(source).Verify(result =>
        {
            var type = result.Compilation.GetTypeByMetadataName("A")!;
            Assert.Empty(type.Interfaces);
            Assert.True(result.Succeeded);
        });
    }

    [Fact]
    public void MultiType()
    {
        var source = @"
using StaticStateMachine;
[StaticStateMachine]
[Association(new[] { 0, 1, 2 }, 0)]
[Association(new[] { 0.0, 0, 1 } , 1L)]
partial struct A
{
}";
        this.runner.Run(source).Verify(result =>
        {
            var type = result.Compilation.GetTypeByMetadataName("A")!;
            var typeArgs = type.Interfaces.Single().TypeArguments;

            Assert.Equal(result.Compilation.GetSpecialType(SpecialType.System_ValueType), typeArgs[0], SymbolEqualityComparer.Default);
            Assert.Equal(result.Compilation.GetSpecialType(SpecialType.System_ValueType), typeArgs[1], SymbolEqualityComparer.Default);

            Assert.True(result.Succeeded);
        });
    }
}