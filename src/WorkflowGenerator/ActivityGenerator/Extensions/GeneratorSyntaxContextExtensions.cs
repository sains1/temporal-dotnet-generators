using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ActivityGenerator.Extensions;

public static class GeneratorSyntaxContextExtensions
{
    public static MethodDeclarationSyntax? GetMethodDeclarationsForMethodsContainingTargetAttribute(this GeneratorSyntaxContext context,
        string attributeName)
    {
        var methodDeclarationSyntax = context.Node as MethodDeclarationSyntax;
        if (methodDeclarationSyntax is null)
        {
            return null;
        }
        
        // loop through all the attributes on the method
        foreach (var attributeListSyntax in methodDeclarationSyntax.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue; // if we can't get the symbol, ignore it
            
            var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
            var fullName = attributeContainingTypeSymbol.ToDisplayString();

            // Is the attribute the target attribute?
            if (fullName == attributeName)
            {
                // return the method declaration
                return methodDeclarationSyntax;
            }
        }
            
        return null;
    }

    public static ClassDeclarationSyntax? GetClassDeclarationsForClassesContainingTargetAttribute(
        this GeneratorSyntaxContext context,
        string attributeName)
    {
        var classDeclarationSyntax = context.Node as ClassDeclarationSyntax;
        if (classDeclarationSyntax is null)
        {
            return null;
        }

        // loop through all the attributes on the class
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue; // if we can't get the symbol, ignore it

            var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
            var fullName = attributeContainingTypeSymbol.ToDisplayString();

            // Is the attribute the target attribute?
            if (fullName == attributeName)
            {
                // return the class declaration
                return classDeclarationSyntax;
            }
        }

        return null;
    }
}