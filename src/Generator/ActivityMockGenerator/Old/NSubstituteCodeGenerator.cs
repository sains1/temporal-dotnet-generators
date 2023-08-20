// using System;
// using System.Collections.Generic;
// using System.Collections.Immutable;
// using System.Linq;
// using System.Text;
// using CodeGenHelpers;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Text;
//
// namespace ActivityMockGenerator;

// [Generator]
// public class NSubstituteCodeGenerator : IIncrementalGenerator
// {
//     private const string AttributeSourceCode = $$"""
//                                                  // <auto-generated/>
//
//                                                  namespace {{NSubstituteGeneratorConstants.RootNamespace}}
//                                                  {
//                                                      [System.AttributeUsage(System.AttributeTargets.Class)]
//                                                      public class {{NSubstituteGeneratorConstants.DecoratorAttributeName}} : System.Attribute
//                                                      {
//                                                      }
//                                                  }
//                                                  """;
//     
//     public void Initialize(IncrementalGeneratorInitializationContext context)
//     {
//         // important for first stage in the pipeline to be very fast and not to allocate, as it will be called a lot
//         var classDeclarations = context.SyntaxProvider
//             .CreateSyntaxProvider(
//                 // filter anything that's not a method
//                 predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
//                 // get any classes containing methods with the [Activity] attribute
//                 transform: static (ctx, _) =>
//                     ctx.GetClassDeclarationsForClassesContainingTargetAttribute(NSubstituteGeneratorConstants
//                         .DecoratorAttributeFullName))
//             .Where(static m => m is not null);
//
//         // register our source output
//         context.RegisterSourceOutput(context.CompilationProvider.Combine(classDeclarations.Collect()),
//             static (spc, source) =>
//             {
//                 var testDoubles = CollectTestDoubles(source.Item1, source.Item2);
//                 var results = Build(testDoubles);
//
//                 foreach (var (file, content) in results)
//                 {
//                     spc.AddSource("Generated.g.cs", SourceText.From(content, Encoding.UTF8));
//                 }
//             });
//     }
//
//     private static IEnumerable<ActivityClass> CollectTestDoubles(Compilation compilation,
//         ImmutableArray<ClassDeclarationSyntax?> classDeclarations)
//     {
//         if (classDeclarations.IsDefaultOrEmpty)
//         {
//             // nothing to do yet
//             return Array.Empty<ActivityClass>();
//         }
//
//         var activityClasses = new List<ActivityClass>();
//         foreach (var classDeclaration in classDeclarations.Where(classDeclaration => classDeclaration is not null))
//         {
//             // get generic type argument
//             if (classDeclaration?.BaseList?.Types.FirstOrDefault()?.Type is not GenericNameSyntax baseType ||
//                 baseType.Identifier.ToString() != nameof(ActivityMockBase<object>)) continue;
//             
//             var genericArgumentSyntax = baseType.TypeArgumentList.Arguments.FirstOrDefault();
//             if (genericArgumentSyntax is null)
//             {
//                 continue;
//             }
//
//             var semanticModel = compilation.GetSemanticModel(genericArgumentSyntax.SyntaxTree);
//             var typeSymbol = semanticModel.GetSymbolInfo(genericArgumentSyntax).Symbol as INamedTypeSymbol;
//
//             if (typeSymbol == null) continue;
//             
//             var methods = typeSymbol.GetMembers()
//                 .OfType<IMethodSymbol>()
//                 .Where(method => method.MethodKind == MethodKind.Ordinary &&
//                                  method.GetAttributes().Any(x =>
//                                      x.AttributeClass?.Name ==
//                                      TemporalConstants.Activity.ActivityAttributeName))
//                 .Select(x => new ActivityMethod
//                 {
//                     MethodName = x.Name,
//                     ReturnType = x.ReturnType.ToDisplayString(),
//                     Parameters = x.Parameters.Select(param => new Parameter
//                     {
//                         ParameterName = param.Name,
//                         ParameterType = param.Type.Name,
//                         OptionalParameterTypeNamespace =param.Type.ContainingNamespace.ToDisplayString()
//                     }).ToArray()
//                 });
//                     
//             var classNamespace = classDeclaration.GetNamespaceOrNull();
//             if (classNamespace is null)
//             {
//                 continue;
//             }
//             
//             activityClasses.Add(new ActivityClass
//             {
//                 ActivityTypeNamespace = classNamespace.Name.ToString(),
//                 ActivityClassName = classDeclaration.Identifier.Text,
//                 ActivityMethods = methods.ToArray()
//             });
//         }
//
//         return activityClasses;
//     }
//
//     private static IEnumerable<(string, string)> Build(IEnumerable<ActivityClass> activities)
//     {
//         var results = new List<(string, string)>();
//         foreach (var activity in activities)
//         {
//             var builder = CodeBuilder.Create(activity.ActivityTypeNamespace)
//                 .AddNamespaceImport(TemporalConstants.Activity.ActivitiesNamespace)
//                 .AddNamespaceImport(CommonNamespaceConstants.SystemThreadingTasksNamespace)
//                 .AddNamespaceImport(NSubstituteGeneratorConstants.NSubstituteNamespace)
//                 .AddClass(activity.ActivityClassName)
//                 .MakePublicClass();
//
//
//             foreach (var method in activity.ActivityMethods)
//             {
//                 var funcType =
//                     $"Func<{string.Join(", ", method.Parameters.Select(x => x.ParameterType))}{(method.Parameters.Any() ? ", " : string.Empty)}{method.ReturnType}>";
//                 
//                 builder
//                     .AddProperty(method.MethodName + "Mock", Accessibility.Public)
//                     .SetType(funcType)
//                     .WithValue($"Substitute.For<{funcType}>()");
//
//                 var methodBuilder = builder
//                     .AddMethod(method.MethodName, Accessibility.Public)
//                     .AddAttribute("Activity")
//                     .WithReturnType(method.ReturnType)
//                     .WithBody(x =>
//                         x.AppendLine(
//                             $"return {method.MethodName}Mock({string.Join(", ", method.Parameters.Select(param => param.ParameterName))});"));
//                 
//                 foreach (var parameter in method.Parameters)
//                 {
//                     methodBuilder.AddParameter(parameter.ParameterType, parameter.ParameterName);
//                     if (parameter.OptionalParameterTypeNamespace is not null)
//                     {
//                         builder.AddNamespaceImport(parameter.OptionalParameterTypeNamespace);
//                     }
//                 }
//             }
//             results.Add((activity.ActivityClassName + ".g.cs", builder.Build()));
//         }
//
//         return results;
//     }
// }

// public class ActivityClass
// {
//     public required string ActivityTypeNamespace { get; set; }
//     public required string ActivityClassName { get; set; }
//     public required ActivityMethod[] ActivityMethods { get; set; }
// }
//
// public class ActivityMethod
// {
//     public required string MethodName { get; set; }
//     public required string ReturnType { get; set; }
//     public required Parameter[] Parameters { get; set; }
// }
