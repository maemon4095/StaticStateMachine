using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StaticStateMachine.Generator;

internal static class LiteralFactory
{
    public static string From(TypedConstant constant)
    {
        var str = constant.ToCSharpString();
        switch (constant.Kind)
        {
            case TypedConstantKind.Enum:
                var prefix = constant.Type?.ToDisplayString(Format.GlobalFullName);
                var idx = str.LastIndexOf('.');
                return prefix + str[idx..];
            default:
                var type = constant.Value?.GetType();
                if (type == typeof(string)) return str;
                if (type == typeof(char)) return str;
                if (type == typeof(Type)) return str;
                return $"({constant.Type?.ToDisplayString(Format.GlobalFullName)}){str}";
        }
    }
    public static string From(char chara) => SyntaxFactory.Literal(chara).ToString();
}