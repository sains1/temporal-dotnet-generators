using System.Collections.Immutable;
using System.Linq;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

[Generator]
public class WorkflowCodeGenerator : IIncrementalGenerator
{
    // temporal namespaces
    private const string TemporalClientNamespace = "Temporalio.Client";
    private const string TemporalWorkflowsNamespace = "Temporalio.Workflows";
    private const string TemporalWorkflowAttributeFullName = "Temporalio.Workflows.WorkflowAttribute";
    private const string TemporalWorkflowRunAttributeFullName = "Temporalio.Workflows.WorkflowRunAttribute";
    
    // output options
    private const string OutputNamespace = "Temporalio.Generators.Workflows";
    private const string OutputFileName = "TemporalClientExtensions.g.cs";
    private const string OutputClassName = "TemporalClientExtensions";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var workflowClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
                TemporalWorkflowAttributeFullName,
                predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol
            ).Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(workflowClasses, Execute);
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> symbols)
    {
        // common imports are included here, but most types will be fully qualified by using
        //      Symbol.ToDisplayString() to get the full name
        var builder = CodeBuilder.Create(OutputNamespace)
            .AddNamespaceImport(TemporalClientNamespace)
            .AddNamespaceImport(TemporalWorkflowsNamespace);

        // add a static class to host our extension methods
        var classBuilder = builder.AddClass(OutputClassName)
            .MakeStaticClass()
            .MakePublicClass();

        foreach (var namedTypeSymbol in symbols.Where(symbol => symbol is not null))
        {
            if (namedTypeSymbol is null)
            {
                continue;
            }

            // each workflow class adds extensions to the parent class
            GenerateClassSource(classBuilder, namedTypeSymbol);
        }

        context.AddSource(OutputFileName, builder.ToString());
    }

    private static void GenerateClassSource(ClassBuilder builder, INamedTypeSymbol workflowClass)
    {
        var methods = workflowClass.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method.MethodKind == MethodKind.Ordinary)
            .ToImmutableArray();

        var runMethod = methods.FirstOrDefault(method => method.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == TemporalWorkflowRunAttributeFullName));
        
        // add the [WorkflowRun] method extensions. Methods will be named: Start{WorkflowClassName}Async
        //      and Execute{WorkflowClassName}Async
        GenerateRunMethodSource(builder, runMethod, workflowClass);
    }

    private static void GenerateRunMethodSource(ClassBuilder classBuilder, IMethodSymbol? runMethod,
        INamedTypeSymbol workflowClass)
    {
        if (runMethod is null) return;

        // when the return type is a Task the handle is in the format WorkflowHandle<{WorkflowClassName}>
        // when the return type is a Task<T> the handle is in the format WorkflowHandle<{WorkflowClassName}, {T}>
        var unpackedReturnType = UnpackGenericOrNull(runMethod.ReturnType as INamedTypeSymbol);
        var handleType =
            $"WorkflowHandle<{workflowClass.Name}{(unpackedReturnType != null ? ", " + unpackedReturnType : "")}>";

        // ensure class is imported
        classBuilder.AddNamespaceImport(workflowClass);
        classBuilder.AddNamespaceImport(runMethod.ReturnType);

        // Add the StartWorkflowAsync extension method
        AddWorkflowMethod("Start", $"Task<{handleType}>");
        
        // Add the ExecuteWorkflowAsync extension method
        AddWorkflowMethod("Execute", runMethod.ReturnType.Name);

        return;

        void AddWorkflowMethod(string methodName, string returnType)
        {
            var namedParams = string.Join(", ", runMethod.Parameters.Select(parameter => parameter.Name));

            // build a static extension method on the TemporalClient
            var methodBuilder = classBuilder.AddMethod($"{methodName}{workflowClass.Name}Async")
                .MakeStaticMethod()
                .MakePublicMethod()
                .WithReturnType(returnType)
                .WithBody(x =>
                    x.AppendLine(
                        $"return client.{methodName}WorkflowAsync(({workflowClass.Name} wf) => wf.{runMethod.Name}({namedParams}), options);"));

            // add the client to start of extension method
            methodBuilder.AddParameter("this ITemporalClient", "client");
            
            // list the individual workflow parameters
            foreach (var parameter in runMethod.Parameters)
            {
                methodBuilder.AddParameter(parameter);
            }

            // add the workflow options as the last parameter
            methodBuilder.AddParameter("WorkflowOptions", "options");
        }
    }
    
    private static string? UnpackGenericOrNull(INamedTypeSymbol? symbol)
    {
        var generic= symbol?.TypeArguments.FirstOrDefault();

        if (generic is null)
        {
            return null;
        }

        // ensure primitives return like 'string' instead of 'String' class
        return generic.SpecialType == SpecialType.None ? generic.Name : generic.ToDisplayString();
    }
}