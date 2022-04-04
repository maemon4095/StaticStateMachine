using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using IncrementalSourceGeneratorSupplement;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.InteropServices;
using System.Linq;
namespace StaticStateMachine.Generator;

[Generator]
public partial class StaticStateMachineGenerator : IncrementalSourceGeneratorBase<TypeDeclarationSyntax>
{
    static string Namespace => nameof(StaticStateMachine);
    static string StateMachineAttributeName => "StaticStateMachineAttribute";
    static string AssociationAttributeName => "AssociationAttribute";
    static string StateMachineAttributeFullName => $"{Namespace}.Generator.{StateMachineAttributeName}";
    static string AssociationAttributeFullName => $"{Namespace}.Generator.{AssociationAttributeName}";

    static SymbolDisplayFormat FormatTypeDecl { get; } = new SymbolDisplayFormat(
        kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);
    static SymbolDisplayFormat FormatGlobalFullName { get; } = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);
    static SymbolDisplayFormat FormatFileName { get; } = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    static bool DefaultEquals(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);
    static ITypeSymbol? LowestCommonAncestorOf(IEnumerable<ITypeSymbol?> types)
    {
        var common = default(ITypeSymbol);
        foreach (var type in types)
        {
            if (type is null) continue;
            if (common is null)
            {
                common = type;
                continue;
            }
            var attributeBases = BaseTypes(type);
            if (attributeBases.Contains(common, SymbolEqualityComparer.Default)) continue;
            common = BaseTypes(common).Intersect(attributeBases, SymbolEqualityComparer.Default).First() as ITypeSymbol;
        }
        return common;
        static IEnumerable<ITypeSymbol> BaseTypes(ITypeSymbol? symbol)
        {
            while (symbol is not null)
            {
                yield return symbol;
                symbol = symbol.BaseType;
            }
        }
    }


    protected override ImmutableArray<AttributeData> FilterAttribute(Compilation compilation, ImmutableArray<AttributeData> attributes)
    {
        var stateMachineAttributeSymbol = compilation.GetTypeByMetadataName(StateMachineAttributeFullName) ?? throw new NullReferenceException($"{StateMachineAttributeFullName} was not found.");
        var associationAttributeSymbol = compilation.GetTypeByMetadataName(AssociationAttributeFullName) ?? throw new NullReferenceException($"{AssociationAttributeFullName} was not found");
        var filtered = attributes.Where(attribute =>
        {
            return DefaultEquals(attribute.AttributeClass, stateMachineAttributeSymbol) || DefaultEquals(attribute.AttributeClass, associationAttributeSymbol);
        }).ToImmutableArray();
        return filtered.IsEmpty ? default : filtered;
    }
    protected override (string HintName, SourceText Source) ProductInitialSource()
    {
        return ($"{StateMachineAttributeFullName}.g.cs", SourceText.From(@"
namespace StaticStateMachine.Generator
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct)]
    class StaticStateMachineAttribute : global::System.Attribute
    {
        public StaticStateMachineAttribute() : this(null, null)
        { }
        public StaticStateMachineAttribute(Type argument, Type associated)
        {
            this.ArgumentType = argument;
            this.AssociatedType = associated;
        }

        public Type ArgumentType { get; }
        public Type AssociatedType { get; }
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
}

namespace StaticStateMachine
{
    public interface IStateMachine<TArg, TAssociated>
    {
        public MachineState<TAssociated> State { get; }
        public bool Transition(TArg arg);
    }

    public interface IResettableStateMachine<TArg, TAssociated>
    {
        public MachineState<TAssociated> State { get; }
        public void Reset();
        public bool Transition(TArg arg);
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
    }
    protected override (string HintName, SourceText Source) ProductSource(Compilation compilation, ISymbol symbol, ImmutableArray<AttributeData> attributes)
    {
        var stateMachineAttributeSymbol = compilation.GetTypeByMetadataName(StateMachineAttributeFullName)!;
        var associationAttributeSymbol = compilation.GetTypeByMetadataName(AssociationAttributeFullName)!;
        var stateMachineAttributeData = attributes.First(a => DefaultEquals(a.AttributeClass, stateMachineAttributeSymbol));
        var associationAttributes = attributes.Where(a => DefaultEquals(a.AttributeClass, associationAttributeSymbol)).Where(a => a.ConstructorArguments.Length == 2);
        var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);

        var argType = stateMachineAttributeData.ConstructorArguments.ElementAtOrDefault(0).Value as ITypeSymbol;
        var associatedType = stateMachineAttributeData.ConstructorArguments.ElementAtOrDefault(1).Value as ITypeSymbol;
        argType ??= LowestCommonAncestorOf(associationAttributes.Select(a =>
        {
            var arg = a.ConstructorArguments[0];
            return arg.Kind switch
            {
                TypedConstantKind.Array => (arg.Type as IArrayTypeSymbol)?.ElementType,
                TypedConstantKind.Primitive => compilation.GetSpecialType(SpecialType.System_Char),
                _ => null,
            };
        })) ?? objectSymbol;
        associatedType ??= LowestCommonAncestorOf(associationAttributes.Select(a => a.ConstructorArguments[1].Type)) ?? objectSymbol;

        var argTypeFullName = argType.ToDisplayString(FormatGlobalFullName);
        var associatedTypeFullName = associatedType.ToDisplayString(FormatGlobalFullName);

        var writer = new IndentedWriter("    ");
        GenerateSource(writer, new()
        {
            Symbol = (symbol as ITypeSymbol)!,
            ArgType = argTypeFullName,
            AssociatedType = associatedTypeFullName,
            TypeDecl = symbol.ToDisplayString(FormatTypeDecl),
            ContainingNamespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            InheritedInterface = $"global::StaticStateMachine.IResettableStateMachine<{argTypeFullName}, {associatedTypeFullName}>",
            StateFullName = $"global::StaticStateMachine.MachineState<{associatedTypeFullName}>",
            StateMachineAttributeData = stateMachineAttributeData,
            AssociationAttributes = associationAttributes,
        });

        return ($"{symbol.ToDisplayString(FormatFileName)}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));

    }

    static void GenerateSource(IndentedWriter writer, GenerationContext context)
    {
        if (context.ContainingNamespace is not null)
        {
            writer["namespace "][context.ContainingNamespace].Line()
                  ['{'].Line().Indent(1);
        }
        foreach(var containing in Containings(context.Symbol))
        {
            writer["partial "][containing.ToDisplayString(FormatTypeDecl)].Line()
                  ['{'].Line().Indent(1);
        }


        writer["partial "][context.TypeDecl][" : "][context.InheritedInterface].Line()
              ['{'].Line().Indent(1);

        writer["private int _state;"].Line()
              ["public "][context.StateFullName][" State { get; private set; }"].Line();

        writer["public void Reset()"].Line()
              ['{'].Line().Indent(1)
              ["this._state = 0;"].Line().Indent(-1)
              ['}'].Line();

        GenerateTransition(writer, context);

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
    static void GenerateTransition(IndentedWriter writer, GenerationContext context)
    {
        writer["public bool Transition("][context.ArgType][" chara)"].Line()
              ['{'].Line().Indent(1);

        GenerateSwitch(writer, context);

        writer.Indent(-1)['}'].Line();

        static void GenerateSwitch(IndentedWriter writer, GenerationContext context)
        {
            var comparer = Comparer<object?>.Create((left, right) =>
            {
                if (left is TypedConstant cl) left = cl.Value;
                if (right is TypedConstant cr) right = cr.Value;
                return (left, right) switch
                {
                    (null, null) => 0,
                    (null, { }) => -1,
                    ({ }, null) => 1,
                    (Type l, Type r) => CompareType(l, r),
                    (string l, string r) => string.CompareOrdinal(l, r),
                    (bool l, bool r) => l.CompareTo(r),
                    (byte l, byte r) => l.CompareTo(r),
                    (char l, char r) => l.CompareTo(r),
                    (double l, double r) => l.CompareTo(r),
                    (float l, float r) => l.CompareTo(r),
                    (int l, int r) => l.CompareTo(r),
                    (long l, long r) => l.CompareTo(r),
                    (sbyte l, sbyte r) => l.CompareTo(r),
                    (short l, short r) => l.CompareTo(r),
                    (uint l, uint r) => l.CompareTo(r),
                    (ulong l, ulong r) => l.CompareTo(r),
                    (ushort l, ushort r) => l.CompareTo(r),
                    ({ } l, { } r) => CompareType(l.GetType(), r.GetType()),
                };

                static int CompareType(Type left, Type right) => string.CompareOrdinal(left.FullName, right.FullName);
            });
            var patterns = context.AssociationAttributes.Select(a =>
            {
                var args = a.ConstructorArguments;
                var arg0 = args[0];
                var arg1 = args[1];
                switch (arg0.Kind)
                {
                    case TypedConstantKind.Array:
                        return (Pattern: arg0.Values.Select(v => (object)v).ToImmutableArray(), arg1.Value);
                    case TypedConstantKind.Primitive:
                        if (arg0.Value is string str) return (Pattern: str.Select(c => (object)c).ToImmutableArray(), arg1.Value);
                        return default;
                    default: return default;
                }
            }).OrderBy(p => p.Pattern, comparer).ToImmutableArray();

            writer["switch(this._state)"].Line()
                  ['{'].Line().Indent(1);
            Body(writer, comparer, 0, 0, 0, patterns.AsSpan());
            writer["default: return false;"].Line().Indent(-1)
                  ['}'].Line();

            static int Body(IndentedWriter writer, IComparer<object?> comparer, int state, int freeState, int depth, ReadOnlySpan<(ImmutableArray<object> Pattern, object? Associated)> span)
            {
                var free = GenerateCase(writer, comparer, state, freeState, depth, span);
                var branch = 0;
                var offset = 0;
                while (offset < span.Length)
                {
                    var (pattern, associated) = span[offset];
                    if (pattern.Length <= depth)
                    {
                        offset++;
                        continue;
                    }
                    var chara = pattern[depth];
                    var count = 0;
                    var matched = 0;
                    while (offset + matched + count < span.Length)
                    {
                        var (p, _) = span[offset + matched + count];
                        if (p.Length - 1 <= depth)
                        {
                            matched++;
                            continue;
                        }
                        if (comparer.Compare(chara, p[depth]) != 0) break;
                        count++;
                    }
                    if (count > 0)
                    {
                        branch++;
                        free = Body(writer, comparer, branch + freeState, free, depth + 1, span.Slice(offset, count + matched));
                    }
                    offset += count + matched;
                }
                return free;


                static int GenerateCase(IndentedWriter writer, IComparer<object?> comparer, int state, int freeState, int depth, ReadOnlySpan<(ImmutableArray<object> Pattern, object? Associated)> span)
                {
                    writer["case "][state][':'].Line().Indent(1)
                          ["switch(chara)"].Line()
                          ['{'].Line().Indent(1);

                    var branch = 0;
                    for (var offset = 0; offset < span.Length;)
                    {
                        var (pattern, associated) = span[offset];
                        if (pattern.Length <= depth)
                        {
                            offset++;
                            continue;
                        }
                        var chara = pattern[depth];
                        var count = 0;
                        var ignored = 0;
                        while (offset + ignored + count < span.Length)
                        {
                            var (p, _) = span[offset + ignored + count];
                            if (p.Length <= depth)
                            {
                                ignored++;
                                continue;
                            }
                            if (comparer.Compare(chara, p[depth]) != 0) break;
                            count++;
                        }
                        var match = pattern.Length - 1 == depth;
                        var terminal = count == 1 && match;
                        if (!terminal) branch++;
                        var nextState = branch + freeState;
                        writer["case "][GetLiteral(chara)][":"].Line().Indent(1)
                              ["this._state = "][terminal ? -1 : nextState][';'].Line()
                              ["this.State = new ("][terminal ? "true" : "false"].End();
                        if (match)
                        {
                            writer[", "][associated].End();
                        }
                        writer[");"].Line();
                        writer["return "][terminal ? "false" : "true"][';'].Indent(-1).Line();
                        offset += count + ignored;
                    }

                    writer["default:"].Line().Indent(1)
                          ["this._state = -1;"].Line()
                          ["this.State = new (true);"].Line()
                          ["return false;"].Line().Indent(-2)
                          ['}'].Line().Indent(-1);

                    return branch + freeState;

                    static string GetLiteral(object obj)
                    {
                        if (obj is TypedConstant constant) return constant.ToCSharpString();
                        if (obj is char chara) return Escaped(chara);
                        return string.Empty;

                        static string Escaped(char chara) => chara switch
                        {
                            '\'' => @"'\''",
                            '\"' => @"'\""'",
                            '\\' => @"'\\'",
                            '\0' => @"'\0'",
                            '\a' => @"'\a'",
                            '\b' => @"'\b'",
                            '\f' => @"'\f'",
                            '\n' => @"'\n'",
                            '\r' => @"'\r'",
                            '\t' => @"'\t'",
                            '\v' => @"'\v'",
                            _ => $"'{chara}'",
                        };
                    }
                }
            }
        }
    }

    static IEnumerable<INamedTypeSymbol> Containings(ITypeSymbol symbol)
    {
        if (symbol.ContainingType is null) yield break;
        foreach(var containing in Containings(symbol.ContainingType)) yield return containing;
        yield return symbol.ContainingType;
    }
}