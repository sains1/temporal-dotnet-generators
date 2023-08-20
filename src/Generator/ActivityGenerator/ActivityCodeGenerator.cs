using System.Collections.Immutable;
using System.Linq;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ActivityGenerator;

[Generator]
public class ActivityCodeGenerator : IIncrementalGenerator
{
    // temporal constants
    private const string TemporalActivityAttributeFullName = "Temporalio.Activities.ActivityAttribute";
    private const string TemporalClientNamespace = "Temporalio.Client";
    private const string TemporalWorkflowsNamespace = "Temporalio.Workflows";
    
    // system constants
    private const string SystemTaskFullName = "System.Threading.Tasks.Task";
    
    // output constants
    private const string OutputNamespace = "Temporalio.Generators.Activities";
    private const string OutputFileName = "TemporalActivityExtensions.g.cs";
    private const string OutputClassName = "Activities";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var workflowClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
                TemporalActivityAttributeFullName,
                predicate: static (s, _) => s is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => ctx.TargetSymbol as IMethodSymbol
            ).Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(workflowClasses, Execute);
    }
    
    private static void Execute(SourceProductionContext context, ImmutableArray<IMethodSymbol?> symbols)
    {
        // common imports are included here, but most types will be fully qualified by using
        //      Symbol.ToDisplayString() to get the full name
        var builder = CodeBuilder.Create(OutputNamespace)
            .AddNamespaceImport(TemporalClientNamespace)
            .AddNamespaceImport(TemporalWorkflowsNamespace);

        // add a static class to host our static methods
        var classBuilder = builder.AddClass(OutputClassName)
            .MakeStaticClass()
            .MakePublicClass();

        foreach (var namedTypeSymbol in symbols.Where(symbol => symbol is not null))
        {
            if (namedTypeSymbol is null)
            {
                continue;
            }

            // each activity adds extensions to the parent class
            GenerateActivityMethodSource(classBuilder, namedTypeSymbol);
        }

        context.AddSource(OutputFileName, builder.ToString());
    }
    
    private static void GenerateActivityMethodSource(ClassBuilder builder, IMethodSymbol activityMethod)
    {
        // Activity methods are named in the format Execute{ActivityName}
        var enclosingClass = activityMethod.ContainingType;
        var parameters = string.Join(", ", activityMethod.Parameters.Select(x => x.Name));

        // static activities can be invoked directly vs non-static which are accepted as part of the expression
        var body = enclosingClass.IsStatic
            ? $"return Workflow.ExecuteActivityAsync(() => {enclosingClass.ToDisplayString()}.{activityMethod.Name}({parameters}), options);"
            : $"return Workflow.ExecuteActivityAsync(({enclosingClass.ToDisplayString()} x) =>  x.{activityMethod.Name}({parameters}), options);";

        var methodBuilder = builder.AddMethod("Execute" + activityMethod.Name)
            .MakeStaticMethod()
            .MakePublicMethod()
            .WithReturnType(GetReturnTypeWrappedInTask(activityMethod.ReturnType))
            .WithBody(x => x.AppendLine(body));
        
        // add the individual activity parameters
        foreach (var parameter in activityMethod.Parameters)
        {
            methodBuilder.AddParameter(parameter);
        }
        
        // add the activity options as a parameter
        methodBuilder.AddParameter("ActivityOptions", "options");
    }

    // Feels hacky but need to ensure our return type is wrapped in a Task
    //      If the type is already a Task or Task<T> then we have no need to wrap it
    //      If the type is void we'll return a plain Task
    //      If the type is anything else we'll return Task<T>
    private static string GetReturnTypeWrappedInTask(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol namedTypeSymbol || returnType.SpecialType == SpecialType.System_Void)
        {
            return SystemTaskFullName;
        }

        if (namedTypeSymbol.ToString() == SystemTaskFullName)
        {
            return namedTypeSymbol.ToDisplayString();
        }
            
        if (namedTypeSymbol.IsGenericType && namedTypeSymbol.BaseType?.ToString() == SystemTaskFullName)
        {
            return namedTypeSymbol.ToDisplayString();
        }

        return SystemTaskFullName +"<" + returnType.ToDisplayString() + ">";
    }
}