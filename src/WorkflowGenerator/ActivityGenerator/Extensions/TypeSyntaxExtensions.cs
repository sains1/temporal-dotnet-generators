using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ActivityGenerator.Extensions;

public static class TypeSyntaxExtensions
{
    public static string? GetNamespaceString(this TypeSyntax typeSyntax, Compilation compilation)
    {
        switch (typeSyntax)
        {
            case QualifiedNameSyntax qualifiedName:
                return qualifiedName.Left.ToString();
            case SimpleNameSyntax simpleName:
            {
                var semanticModel = compilation.GetSemanticModel(typeSyntax.SyntaxTree);

                if (semanticModel.GetSymbolInfo(simpleName).Symbol is INamedTypeSymbol typeSymbol)
                {
                    return typeSymbol.ContainingNamespace.ToString();
                }

                break;
            }
        }

        return null;
    }
}