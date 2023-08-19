using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WorkflowGenerator;

[Generator]
public class WorkflowSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // important for first stage in the pipeline to be very fast and not to allocate, as it will be called a lot
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), 
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax?> Right)> compilationAndClasses 
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));

    }
    
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

    private const string WorkflowAttribute = "Temporalio.Workflows.WorkflowAttribute";
    private const string WorkflowRunAttribute = "Temporalio.Workflows.WorkflowRunAttribute";

    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a MethodDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes on the method
        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue; // if we can't get the symbol, ignore it
                
                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Is the attribute the [Activity] attribute?
                if (fullName == WorkflowAttribute)
                {
                    // return the parent class of the method
                    return classDeclarationSyntax;
                }
            }
        }
            
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes,
        SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        var methods = new List<string>();
        var usings = new HashSet<string>
        {
            "System.Threading.Tasks",
            "Temporalio.Client",
            "Temporalio.Workflows"
        };
        
        foreach (var classDeclaration in classes)
        {
            if (classDeclaration is null)
            {
                continue;
            }
            
            var identifier = classDeclaration.Identifier.Text;
            foreach (var member in classDeclaration.Members)
            {
                if (!member.IsKind(SyntaxKind.MethodDeclaration))
                {
                    continue;
                }

                var methodDeclarationSyntax = (MethodDeclarationSyntax)member;

                foreach (var attributeListSyntax in member.AttributeLists)
                {
                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        if (ModelExtensions.GetSymbolInfo(compilation.GetSemanticModel(attributeSyntax.SyntaxTree),
                                attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                            continue; // if we can't get the symbol, ignore it

                        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        var fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (fullName == WorkflowRunAttribute)
                        {
                            string? returnType = null;
                            if (methodDeclarationSyntax.ReturnType is GenericNameSyntax genericNameSyntax)
                            {
                                returnType = genericNameSyntax.TypeArgumentList.Arguments[0].ToString();
                            }

                            var paramsArray = new string[methodDeclarationSyntax.ParameterList.Parameters.Count];
                            var paramsIdentifiersArray = new string[methodDeclarationSyntax.ParameterList.Parameters.Count];
                            for (var i = 0; i < methodDeclarationSyntax.ParameterList.Parameters.Count; i++)
                            {
                                var parameter = methodDeclarationSyntax.ParameterList.Parameters[i];
                                paramsArray[i] = $"{parameter.Type} {parameter.Identifier}";
                                paramsIdentifiersArray[i] = parameter.Identifier.ToString();

                                if (GetNamespaceFromTypeSyntax(compilation, parameter.Type) is { } paramNamespace)
                                {
                                    usings.Add(paramNamespace);
                                }
                            }

                            methods.Add(CodeGenerators.GetWorkflowRunMethod(
                                identifier,
                                methodDeclarationSyntax.Identifier.Text,
                                paramsArray,
                                paramsIdentifiersArray,
                                returnType
                            ));

                            var classNamespace = classDeclaration
                                .Ancestors()
                                .OfType<BaseNamespaceDeclarationSyntax>()
                                .FirstOrDefault();

                            if (classNamespace is not null)
                            {
                                usings.Add(classNamespace.Name.ToString());
                            }
                        }
                    }
                }
            }
        }
        
        // Build up the source code
        const string extensionsNamespaceName = "Temporalio.Extensions.Generators.Workflow";

        // Add the source code to the compilation.
        context.AddSource($"TemporalClientExtensions.g.cs",
            SourceText.From(
                CodeGenerators.GetExtensionsClass(string.Join("\n", methods), extensionsNamespaceName,
                    usings.OrderBy(x => x).ToArray()),
                Encoding.UTF8));
    }
    
    static string? GetNamespaceFromTypeSyntax(Compilation compilation, TypeSyntax? typeSyntax)
    {
        if (typeSyntax is null)
        {
            return null;
        }
        
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