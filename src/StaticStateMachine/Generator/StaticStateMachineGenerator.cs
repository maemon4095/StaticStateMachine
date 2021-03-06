using SourceGeneratorSupplement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
namespace StaticStateMachine.Generator;

[Generator]
public partial class StaticStateMachineGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(this.ProductInitialSource);

        var stateMachineAttributeSymbolProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(Name.StateMachineAttributeFull) ?? throw new NullReferenceException($"{Name.StateMachineAttributeFull} was not found.");
        }).WithComparer(SymbolEqualityComparer.Default);
        var associationAttributeSymbolProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(Name.AssociationAttributeFull) ?? throw new NullReferenceException($"{Name.AssociationAttributeFull} was not found");
        }).WithComparer(SymbolEqualityComparer.Default);

        var valuesProvider = context.SyntaxProvider.CreateSyntaxProvider((syntax, token) =>
        {
            token.ThrowIfCancellationRequested();
            return syntax is TypeDeclarationSyntax { AttributeLists.Count: > 0 };
        },
        (context, token) =>
        {
            var syntax = (context.Node as TypeDeclarationSyntax)!;
            return context.SemanticModel.GetDeclaredSymbol(syntax);
        })
        .WithComparer(SymbolEqualityComparer.Default)
        .Where(symbol => symbol is not null)
        .Combine(stateMachineAttributeSymbolProvider)
        .Combine(associationAttributeSymbolProvider)
        .Select((tuple, token) =>
        {
            var ((symbol, stateMachineAttribute), associationAttribute) = tuple;
            var attributes = symbol!.GetAttributes();
            var stateMachineAttributeData = attributes.SingleOrDefault(a => DefaultEquals(a.AttributeClass, stateMachineAttribute));
            var associationAttributeData = attributes.Where(a => DefaultEquals(a.AttributeClass, associationAttribute));

            return (symbol, stateMachineAttributeData, associationAttributeData);
        })
        .Where((tuple) => tuple.stateMachineAttributeData is not null)
        .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(valuesProvider, this.ProductSource!);
    }
    protected void ProductInitialSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("StaticStateMachine.g.cs", @$"
namespace {Name.Namespace}
{{
    enum {Name.StateMachineCategory}
    {{
        {string.Join(", ", Enum.GetNames(typeof(StateMachineCategory)))}
    }}

    public interface IStateMachine<TArg, TAssociated>
    {{
        public {Name.MachineState}<TAssociated> State {{ get; }}
        public bool Transition(TArg arg);
    }}

    public interface {Name.ResettableStateMachine}<TArg, TAssociated> : IStateMachine<TArg, TAssociated>
    {{
        public void Reset();
    }}

    public struct {Name.MachineState}<TAssociated>
    {{
        public {Name.MachineState}(bool isTerminal, TAssociated associated)
        {{
            this.IsTerminal = isTerminal;
            this.Accept = true;
            this.Associated = associated;
        }}
        public {Name.MachineState}(bool isTerminal)
        {{
            this.IsTerminal = isTerminal;
            this.Accept = false;
            this.Associated = default!;
        }}

        public bool IsTerminal {{ get; }}
        public bool Accept {{ get; }}
        public TAssociated Associated {{ get; }}
    }}
}}");

        context.AddSource("StaticStateMachineAttributes.g.cs", SourceText.From(@$"
namespace {Name.Namespace}
{{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct)]
    class {Name.StateMachineAttribute} : global::System.Attribute
    {{
        public {Name.StateMachineAttribute}(global::{Name.StateMachineCategoryFull} category = global::{Name.StateMachineCategoryFull}.{StateMachineCategory.Plain})
            : this(null, null, category) 
        {{ }}
        public {Name.StateMachineAttribute}(
            global::System.Type argument, 
            global::System.Type associated, 
            global::{Name.StateMachineCategoryFull} category = global::{Name.StateMachineCategoryFull}.{StateMachineCategory.Plain})
        {{
            this.ArgumentType = argument;
            this.AssociatedType = associated;
            this.Category = category;
        }}

        public global::System.Type ArgumentType {{ get; }}
        public global::System.Type AssociatedType {{ get; }}
        public global::{Name.StateMachineCategoryFull} Category {{ get; }}
    }}

    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = true)]
    class {Name.AssociationAttribute} : global::System.Attribute
    {{
        public {Name.AssociationAttribute}(object pattern, object associated)
        {{
            this.Pattern = pattern;
            this.Associated = associated;
        }}
        public object Pattern {{ get; }}
        public object Associated {{ get; }}
    }}
}}", Encoding.UTF8));
    }
    void ProductSource(SourceProductionContext context, ((INamedTypeSymbol, AttributeData, IEnumerable<AttributeData>), Compilation) tuple)
    {
        try
        {
            var ((symbol, stateMachineAttribute, associationAttributes), compilation) = tuple;
            var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
            var associations = associationAttributes.Where(data => data.ConstructorArguments.Length == 2).Select(data =>
            {
                var args = data.ConstructorArguments;
                return (Pattern: args[0], Associated: args[1]);
            }).ToImmutableArray();

            var (argType, associatedType, category) = ReadStateMachineAttribute(compilation, stateMachineAttribute, associations);
            var argTypeFullName = argType?.ToDisplayString(Format.GlobalFullName);
            var associatedTypeFullName = associatedType.ToDisplayString(Format.GlobalFullName);
            var writer = new IndentedWriter("    ");
            WriteDeclaration(writer, new()
            {
                Symbol = symbol,
                ArgType = argTypeFullName,
                AssociatedType = associatedTypeFullName,
                StateType = $"global::{Name.MachineStateFull}<{associatedTypeFullName}>",
                ContainingNamespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
                InheritedInterfaces = category == StateMachineCategory.TypeWise ? null : $"global::{Name.ResettableStateMachineFull}<{argTypeFullName}, {associatedTypeFullName}>",
                InternalStateVariable = "_state",
                StateVariable = "State",
                TransitionArgCharaVariable = "chara",
                Automaton = AutomatonFactory.Create(compilation, category, associations),
                Compilation = compilation,
            });

            context.AddSource($"{symbol.ToDisplayString(Format.FileName)}.g.cs", writer.ToString());
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostics.From(ex));
        }
    }

    static (ITypeSymbol? ArgType, ITypeSymbol AssociatedType, StateMachineCategory Category) ReadStateMachineAttribute(
        Compilation compilation,
        AttributeData stateMachineAttribute,
        ImmutableArray<(TypedConstant Pattern, TypedConstant Associated)> associations)
    {
        var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
        var args = stateMachineAttribute.ConstructorArguments;
        var argType = default(ITypeSymbol);
        var associatedType = default(ITypeSymbol);
        var category = StateMachineCategory.Plain;

        switch (args.Length)
        {
            case 1:
                category = getCategory(args[0]);
                break;
            case 2:
                argType = args[0].Value as ITypeSymbol;
                associatedType = args[1].Value as ITypeSymbol;
                break;
            case 3:
                argType = args[0].Value as ITypeSymbol;
                associatedType = args[1].Value as ITypeSymbol;
                category = getCategory(args[2]);
                break;
            default: break;
        }

        if (category != StateMachineCategory.TypeWise && argType is null)
        {
            var charSymbol = compilation.GetSpecialType(SpecialType.System_Char);
            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            argType = LowestCommonAncestorOf(
                        associations.Select(a =>
                        {
                            var pattern = a.Pattern;

                            switch (pattern.Kind)
                            {
                                case TypedConstantKind.Array:
                                    return (pattern.Type as IArrayTypeSymbol)?.ElementType ?? throw InvalidAssociationException.NotSupportedPatternType(Location.None, pattern.Type);
                                case TypedConstantKind.Primitive:
                                    if (DefaultEquals(pattern.Type, stringSymbol)) return charSymbol;
                                    throw InvalidAssociationException.NotSupportedPatternType(Location.None, pattern.Type);
                                default:
                                    throw InvalidAssociationException.NotSupportedPatternType(Location.None, pattern.Type);
                            }
                        })
                      ) ?? objectSymbol;
        }

        associatedType ??= LowestCommonAncestorOf(associations.Select(pair => pair.Associated.Type)) ?? objectSymbol;

        return (argType, associatedType, category);

        static StateMachineCategory getCategory(TypedConstant constant)
        {
            var obj = constant.Value is null ? null : Enum.ToObject(typeof(StateMachineCategory), constant.Value);
            if (obj is null) return StateMachineCategory.Plain;
            return obj as StateMachineCategory? ?? StateMachineCategory.Plain;
        }
    }
}