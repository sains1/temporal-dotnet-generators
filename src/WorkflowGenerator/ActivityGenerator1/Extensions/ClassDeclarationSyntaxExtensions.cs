using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ActivityGenerator.Extensions;

public static class ClassDeclarationSyntaxExtensions
{
    public static BaseNamespaceDeclarationSyntax? GetNamespaceOrNull(this ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
    }
}