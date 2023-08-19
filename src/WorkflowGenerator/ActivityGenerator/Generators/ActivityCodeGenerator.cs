using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ActivityGenerator.Constants;
using ActivityGenerator.Extensions;
using ActivityGenerator.Models;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ActivityGenerator.Generators;


[Generator]
public class ActivityCodeGenerator: IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // important for first stage in the pipeline to be very fast and not to allocate, as it will be called a lot
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                // filter anything that's not a method
                predicate: static (s, _) => s is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                // get any classes containing methods with the [Activity] attribute
                transform: static (ctx, _) =>
                    ctx.GetMethodDeclarationsForMethodsContainingTargetAttribute(TemporalConstants
                        .Activity.ActivityAttributeFullName))
            .Where(static m => m is not null);

        // register our source output
        context.RegisterSourceOutput(context.CompilationProvider.Combine(classDeclarations.Collect()),
            static (spc, source) =>
            {
                var activities = CollectActivities(source.Item1, source.Item2);
                var result = Build(activities);
                spc.AddSource(ActivityGeneratorConstants.ActivitiesExtensionsGeneratedFileName,
                    SourceText.From(result, Encoding.UTF8));
            });
    }

    /// <summary>
    /// Collect any methods with the [Activity] attribute into a structured collection containing all the
    /// type information we need to construct a set of static methods.
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="activityMethodDeclarations"></param>
    /// <returns></returns>
    private static IEnumerable<Activity> CollectActivities(Compilation compilation,
        ImmutableArray<MethodDeclarationSyntax?> activityMethodDeclarations)
    {
        if (activityMethodDeclarations.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return Array.Empty<Activity>();
        }

        var activities = new List<Activity>();
        foreach (var method in activityMethodDeclarations)
        {
            // get containing class
            if (method?.Parent is not ClassDeclarationSyntax classDeclaration)
            {
                continue;
            }

            // get containing namespace
            if (classDeclaration.Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault() is not { } namespaceDeclaration)
            {
                continue;
            }

            // get generic return type (if any)
            var returnType = (method.ReturnType as GenericNameSyntax)?.TypeArgumentList.Arguments
                .FirstOrDefault()?.ToString();
            
            // get parameters
            var parameters = new List<Parameter>(method.ParameterList.Parameters.Count);
            foreach (var parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type is not { } typeSyntax)
                {
                    continue;
                }

                parameters.Add(new Parameter
                {
                    ParameterType = typeSyntax.ToString(),
                    ParameterName = parameter.Identifier.ToString(),
                    OptionalParameterTypeNamespace = typeSyntax.GetNamespaceString(compilation)
                });
            }

            activities.Add(new Activity
            {
                ActivityMethodName = method.Identifier.ValueText,
                ActivityTypeNamespace = namespaceDeclaration.Name.ToString(),
                ActivityTypeName = classDeclaration.Identifier.Text,
                OptionalReturnType = returnType,
                Parameters = parameters
            });
        }
        
        return activities;
    }
   
    /// <summary>
    /// Build the resulting Activities class using the CodeGenHelpers library.
    /// </summary>
    /// <param name="activities"></param>
    /// <returns></returns>
    private static string Build(IEnumerable<Activity> activities)
    {
        var builder = CodeBuilder.Create(ActivityGeneratorConstants.RootNamespace)
            .AddNamespaceImport(TemporalConstants.Workflow.WorkflowsNamespace)
            .AddNamespaceImport(CommonNamespaceConstants.SystemThreadingTasksNamespace)
            .AddClass(ActivityGeneratorConstants.ActivityExtensionsClassName)
            .MakeStaticClass();

        foreach(var activity in activities)
        {
            // ensure activity is imported
            builder.AddNamespaceImport(activity.ActivityTypeNamespace);
         
            // methods prefixed with the encapsulating class name to avoid collisions
            //      e.g. MyActivityClass.RunAsync -> MyActivityClassRunAsync
            var methodBuilder = builder.AddMethod(activity.ActivityTypeName + activity.ActivityMethodName, Accessibility.Public)
                .MakeStaticMethod()
                .WithReturnType(activity.OptionalReturnType is null ? "Task" : $"Task<{activity.OptionalReturnType}>");

            foreach (var parameter in activity.Parameters)
            {
                methodBuilder.AddParameter(parameter.ParameterType, parameter.ParameterName);
                
                // parameter namespace is optional as it may be a primitive
                if (parameter.OptionalParameterTypeNamespace is not null)
                {
                    // ensure the namespace is imported
                    builder.AddNamespaceImport(parameter.OptionalParameterTypeNamespace);
                }
            }
         
            // every activity method has an options parameter
            methodBuilder.AddParameter(TemporalConstants.Activity.ActivityOptionsName, "options");

            // add the body of the method which will call Workflow.ExecuteActivityAsync
            var paramsList = activity.Parameters.Select(x => x.ParameterName);
            var execute = $"""
                           return {TemporalConstants.Workflow.WorkflowName}.{TemporalConstants.Workflow.ExecuteActivityAsyncMethodName}(({activity.ActivityTypeName} x) =>
                              x.{activity.ActivityMethodName}({string.Join(", ", paramsList)}), options);
                           """;
          
            methodBuilder.WithBody(x => x.AppendLine(execute));
        }
      
        var result = builder.Builder.Build();
        return result;
    }
}