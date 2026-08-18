using Microsoft.CodeAnalysis;

namespace DotPudica.SourceGenerator;

internal static class TypeDisplay
{
    private static readonly SymbolDisplayFormat Code = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static string ForCode(this ITypeSymbol type) => type.ToDisplayString(Code);
}
