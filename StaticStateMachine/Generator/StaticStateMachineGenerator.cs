using IncrementalSourceGeneratorSupplement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
namespace StaticStateMachine.Generator;


[Generator]
public partial class StaticStateMachineGenerator : IIncrementalGenerator
{
    static string StaticStateMachineNamespace => nameof(StaticStateMachine);
    static string StaticStateMachineAttributeNamespace => $"{StaticStateMachineNamespace}.Attributes";
    static string StateMachineAttributeName => "StaticStateMachineAttribute";
    static string AssociationAttributeName => "AssociationAttribute";
    static string StateMachineAttributeFullName => $"{StaticStateMachineAttributeNamespace}.{StateMachineAttributeName}";
    static string AssociationAttributeFullName => $"{StaticStateMachineAttributeNamespace}.{AssociationAttributeName}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(this.ProductInitialSource);

        var stateMachineAttributeSymbolProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(StateMachineAttributeFullName) ?? throw new NullReferenceException($"{StateMachineAttributeFullName} was not found.");
        });
        var associationAttributeSymbolProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(AssociationAttributeFullName) ?? throw new NullReferenceException($"{AssociationAttributeFullName} was not found");
        });

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
        .Where(symbol => symbol is not null)
        .Combine(stateMachineAttributeSymbolProvider)
        .Combine(associationAttributeSymbolProvider)
        .Select((tuple, token) =>
        {
            var ((syntax, stateMachineAttribute), associationAttribute) = tuple;
            var attributes = syntax!.GetAttributes();
            var stateMachineAttributeData = attributes.SingleOrDefault(a => DefaultEquals(a.AttributeClass, stateMachineAttribute));
            var associationAttributeData = attributes.Where(a => DefaultEquals(a.AttributeClass, associationAttribute));

            return (syntax, stateMachineAttributeData, associationAttributeData);
        })
        .Where((tuple) => tuple.stateMachineAttributeData is not null)
        .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(valuesProvider, this.ProductSource!);
    }
    protected void ProductInitialSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("StaticStateMachine.g.cs", SourceText.From(@"
namespace StaticStateMachine
{
    enum StateMachineCategory
    {
        PlainText, 
    }

    public interface IStateMachine<TArg, TAssociated>
    {
        public MachineState<TAssociated> State { get; }
        public bool Transition(TArg arg);
    }

    public interface IResettableStateMachine<TArg, TAssociated> : IStateMachine<TArg, TAssociated>
    {
        public void Reset();
    }

    public struct MachineState<TAssociated>
    {
        public MachineState(bool isTerminal, TAssociated associated)
        {
            this.IsTerminal = isTerminal;
            this.Accept = true;
            this.Associated = associated;
        }
        public MachineState(bool isTerminal)
        {
            this.IsTerminal = isTerminal;
            this.Accept = false;
            this.Associated = default!;
        }

        public bool IsTerminal { get; }
        public bool Accept { get; }
        public TAssociated Associated { get; }
    }
}
", Encoding.UTF8));

        context.AddSource("StaticStateMachineAttributes.g.cs", SourceText.From(@"
namespace StaticStateMachine.Attributes
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct)]
    class StaticStateMachineAttribute : global::System.Attribute
    {
        public StaticStateMachineAttribute(global::StaticStateMachine.StateMachineCategory category = global::StaticStateMachine.StateMachineCategory.PlainText)
            : this(null, null, category) 
        { }
        public StaticStateMachineAttribute(
            global::System.Type argument, 
            global::System.Type associated, 
            global::StaticStateMachine.StateMachineCategory category = global::StaticStateMachine.StateMachineCategory.PlainText)
        {
            this.ArgumentType = argument;
            this.AssociatedType = associated;
            this.Category = category;
        }

        public global::System.Type ArgumentType { get; }
        public global::System.Type AssociatedType { get; }
        public global::StaticStateMachine.StateMachineCategory Category { get; }
    }

    [AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = true)]
    class AssociationAttribute : global::System.Attribute
    {
        public AssociationAttribute(object pattern, object associated)
        {
            this.Pattern = pattern;
            this.Associated = associated;
        }
        public object Pattern { get; }
        public object Associated { get; }
    }
}", Encoding.UTF8));
    }

    void ProductSource(SourceProductionContext context, ((INamedTypeSymbol, AttributeData, IEnumerable<AttributeData>), Compilation) tuple)
    {
        try
        {
            var ((symbol, stateMachineAttribute, associationAttributes), compilation) = tuple;
            var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
            var associations = associationAttributes.Select(data =>
            {
                var args = data.ConstructorArguments;
                return (Pattern: args[0], Associated: args[1]);
            }).ToImmutableArray();

            var (argType, associatedType, category) = ReadStateMachineAttribute(compilation, stateMachineAttribute, associations);
            var argTypeFullName = argType.ToDisplayString(Format.GlobalFullName);
            var associatedTypeFullName = associatedType.ToDisplayString(Format.GlobalFullName);
            var writer = new IndentedWriter("    ");
            WriteDeclaration(writer, new()
            {
                Symbol = symbol,
                ArgType = argTypeFullName,
                AssociatedType = associatedTypeFullName,
                StateType = $"global::StaticStateMachine.MachineState<{associatedTypeFullName}>",
                ContainingNamespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
                InheritedInterfaces = $"global::StaticStateMachine.IResettableStateMachine<{argTypeFullName}, {associatedTypeFullName}>",
                InternalStateVariable = "_state",
                StateVariable = "State",
                TransitionArgCharaVariable = "chara",
                Automaton = AutomatonFactory.Create(category, associations),
            });

            context.AddSource($"{symbol.ToDisplayString(Format.FileName)}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.GetType()} was thrown in product source. Message : {ex.Message} | StackTrace : {ex.StackTrace}", ex);
        }
    }

    static (ITypeSymbol ArgType, ITypeSymbol AssociatedType, StateMachineCategory Category) ReadStateMachineAttribute(
        Compilation compilation,
        AttributeData stateMachineAttribute,
        ImmutableArray<(TypedConstant Pattern, TypedConstant Associated)> associations)
    {
        var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
        var args = stateMachineAttribute.ConstructorArguments;
        var argType = default(ITypeSymbol);
        var associatedType = default(ITypeSymbol);
        var category = StateMachineCategory.PlainText;

        switch (args.Length)
        {
            case 1:
                category = Enum.ToObject(typeof(StateMachineCategory), args[0].Value) as StateMachineCategory? ?? StateMachineCategory.PlainText;
                break;
            case 2:
                argType = args[0].Value as ITypeSymbol;
                associatedType = args[1].Value as ITypeSymbol;
                break;
            case 3:
                argType = args[0].Value as ITypeSymbol;
                associatedType = args[1].Value as ITypeSymbol;
                category = Enum.ToObject(typeof(StateMachineCategory), args[2].Value) as StateMachineCategory? ?? StateMachineCategory.PlainText;
                break;
            default: break;
        }

        argType ??= LowestCommonAncestorOf(
                        associations.Select(a =>
                        {
                            var pattern = a.Pattern;
                            return pattern.Kind switch
                            {
                                TypedConstantKind.Array => (pattern.Type as IArrayTypeSymbol)?.ElementType,
                                TypedConstantKind.Primitive => compilation.GetSpecialType(SpecialType.System_Char),
                                _ => null,
                            };
                        })
                    ) ?? objectSymbol;

        associatedType ??= LowestCommonAncestorOf(associations.Select(pair => pair.Associated.Type)) ?? objectSymbol;

        validate(compilation, argType, associatedType, category);

        return (argType, associatedType, category);

        static void validate(Compilation compilation, ITypeSymbol argType, ITypeSymbol associatedType, StateMachineCategory category)
        {
            switch (category)
            {
                case StateMachineCategory.PlainText: return;
#if false
                case StateMachineCategory.Regex:
                    var charSymbol = compilation.GetSpecialType(SpecialType.System_Char);
                    if (SymbolEqualityComparer.Default.Equals(argType, charSymbol)) return;
                    throw new ArgumentException("regex state machine supports only string argument");
#endif
            }
        }
    }

    static void WriteDeclaration(IndentedWriter writer, GenerationContext context)
    {
        if (context.ContainingNamespace is not null)
        {
            writer["namespace "][context.ContainingNamespace].Line()
                  ['{'].Line().Indent(1);
        }
        foreach (var containing in Containings(context.Symbol))
        {
            writer["partial "][containing.ToDisplayString(Format.TypeDecl)].Line()
                  ['{'].Line().Indent(1);
        }

        writer["partial "][context.Symbol.ToDisplayString(Format.TypeDecl)][" : "][context.InheritedInterfaces].Line()
              ['{'].Line().Indent(1);

        writer["private int "][context.InternalStateVariable][';'].Line()
              ["public "][context.StateType][' '][context.StateVariable][" { get; private set; }"].Line();

        writer["public void Reset()"].Line()
              ['{'].Line().Indent(1)
              ["this."][context.InternalStateVariable][" = 0;"].Line()
              ["this."][context.StateVariable][" = new(false);"].Line().Indent(-1)
              ['}'].Line();

        WriteTransition(writer, context);

        writer.Indent(-1)['}'].Line();

        foreach (var _ in Containings(context.Symbol))
        {
            writer.Indent(-1)['}'].Line();
        }

        if (context.ContainingNamespace is not null)
        {
            writer.Indent(-1)['}'].Line();
        }
    }
    static void WriteTransition(IndentedWriter writer, GenerationContext context)
    {
        var automaton = context.Automaton;

        writer["public bool Transition("][context.ArgType][' '][context.TransitionArgCharaVariable][')'].Line()
              ['{'].Line().Indent(1)
              ["switch(this."][context.InternalStateVariable][')'].Line()
              ['{'].Line().Indent(1);

        foreach (var (src, transitions) in automaton.EnumerateConnections())
        {
            writer["case "][src][':'].Line().Indent(1)
                  ["switch("][context.TransitionArgCharaVariable][')'].Line()
                  ['{'].Line().Indent(1);

            foreach (var (arg, dst, associated) in transitions)
            {
                if (arg is null)
                {
                    writer["default :"].Line();
                }
                else
                {
                    writer["case "][arg][':'].Line();
                }
                writer.Indent(1)
                      ["this."][context.InternalStateVariable][" = "][dst][';'].Line()
                      ["this."][context.StateVariable][" = new("][dst < 0 ? "true" : "false"].End();
                if (associated is not null)
                {
                    writer[", "][associated].End();
                }
                writer[");"].Line();
                writer["return "][dst < 0 ? "false" : "true"][';'].Line().Indent(-1);
            }
            writer["default:"].Line().Indent(1)
                  ["this."][context.InternalStateVariable][" = -1;"].Line()
                  ["this."][context.StateVariable][" = new (true);"].Line()
                  ["return false;"].Line().Indent(-2)
                  ['}'].Line().Indent(-1);
        }

        writer["default: return false;"].Line().Indent(-1)
              ['}'].Line().Indent(-1)
              ['}'].Line();
    }
}