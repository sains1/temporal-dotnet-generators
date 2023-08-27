using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Temporalio.Workflows;
using Xunit;

namespace Generator.Tests.WorkflowGenerator;

public class WorkflowGeneratorTests
{
    [Fact]
    public void GeneratesCorrectOutputForBasicWorkflow()
    {
        const string input = """
                             using System.Threading.Tasks;
                             using Temporalio.Workflows;
                             using Temporalio.Generators.Workflows;

                             namespace Generator.Tests.WorkflowGenerator;

                             [Workflow]
                             [GenerateWorkflowExtension]
                             public class TestWorkflow
                             {
                                 [WorkflowRun]
                                 public Task RunAsync()
                                 {
                                     return Task.CompletedTask;
                                 }
                             }
                             """;
        const string expectedOutput = """
                                      //------------------------------------------------------------------------------
                                      // <auto-generated>
                                      //     This code was generated.
                                      //
                                      //     Changes to this file may cause incorrect behavior and will be lost if
                                      //     the code is regenerated.
                                      // </auto-generated>
                                      //------------------------------------------------------------------------------

                                      using System.Threading.Tasks;
                                      using Generator.Tests.WorkflowGenerator;
                                      using Temporalio.Client;
                                      using Temporalio.Workflows;

                                      namespace Temporalio.Generators.Workflows
                                      {
                                          public static partial class TemporalClientExtensions
                                          {
                                              public static Task ExecuteTestWorkflowAsync(this ITemporalClient client, WorkflowOptions options)
                                              {
                                                  return client.ExecuteWorkflowAsync((TestWorkflow wf) => wf.RunAsync(), options);
                                              }
                                      
                                              public static Task<WorkflowHandle<TestWorkflow>> StartTestWorkflowAsync(this ITemporalClient client, WorkflowOptions options)
                                              {
                                                  return client.StartWorkflowAsync((TestWorkflow wf) => wf.RunAsync(), options);
                                              }
                                          }
                                      }

                                      """;

        RunTest(input, expectedOutput);
    }
    
    [Fact]
    public void GeneratesCorrectOutputForWorkflowWithReturnType()
    {
        const string input = """
                             using System.Threading.Tasks;
                             using Temporalio.Workflows;
                             using Temporalio.Generators.Workflows;

                             namespace Generator.Tests.WorkflowGenerator;

                             [Workflow]
                             [GenerateWorkflowExtension]
                             public class TestWorkflow
                             {
                                 [WorkflowRun]
                                 public Task<string> RunAsync()
                                 {
                                     return Task.FromResult("hello world");
                                 }
                             }
                             """;
        
        const string expectedOutput = """
                                      //------------------------------------------------------------------------------
                                      // <auto-generated>
                                      //     This code was generated.
                                      //
                                      //     Changes to this file may cause incorrect behavior and will be lost if
                                      //     the code is regenerated.
                                      // </auto-generated>
                                      //------------------------------------------------------------------------------

                                      using System.Threading.Tasks;
                                      using Generator.Tests.WorkflowGenerator;
                                      using Temporalio.Client;
                                      using Temporalio.Workflows;

                                      namespace Temporalio.Generators.Workflows
                                      {
                                          public static partial class TemporalClientExtensions
                                          {
                                              public static Task<string> ExecuteTestWorkflowAsync(this ITemporalClient client, WorkflowOptions options)
                                              {
                                                  return client.ExecuteWorkflowAsync((TestWorkflow wf) => wf.RunAsync(), options);
                                              }
                                      
                                              public static Task<WorkflowHandle<TestWorkflow, string>> StartTestWorkflowAsync(this ITemporalClient client, WorkflowOptions options)
                                              {
                                                  return client.StartWorkflowAsync((TestWorkflow wf) => wf.RunAsync(), options);
                                              }
                                          }
                                      }

                                      """;

        RunTest(input, expectedOutput);
    }
    
        
    [Fact]
    public void GeneratesEmptyClassWhenWorkflowNotDecoratedWithAttribute()
    {
        const string input = """
                             using System.Threading.Tasks;
                             using Temporalio.Workflows;
                             using Temporalio.Generators.Workflows;

                             namespace Generator.Tests.WorkflowGenerator;

                             // NOTE - commented out to prevent code generation
                             // [GenerateWorkflowExtension]
                             [Workflow]
                             public class TestWorkflow
                             {
                                 [WorkflowRun]
                                 public Task<string> RunAsync()
                                 {
                                     return Task.FromResult("hello world");
                                 }
                             }
                             """;
        
        const string expectedOutput = """
                                      //------------------------------------------------------------------------------
                                      // <auto-generated>
                                      //     This code was generated.
                                      //
                                      //     Changes to this file may cause incorrect behavior and will be lost if
                                      //     the code is regenerated.
                                      // </auto-generated>
                                      //------------------------------------------------------------------------------

                                      using Temporalio.Client;
                                      using Temporalio.Workflows;

                                      namespace Temporalio.Generators.Workflows
                                      {
                                          public static partial class TemporalClientExtensions
                                          {
                                          }
                                      }

                                      """;

        RunTest(input, expectedOutput);
    }

    private void RunTest(string input, string expectedOutput)
    {
        // arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(input);
        Assert.Empty(syntaxTree.GetDiagnostics());

        var compilation = CSharpCompilation.Create(nameof(WorkflowGeneratorTests), new[] { syntaxTree })
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Workflow).Assembly.Location)
            );

        // act
        var runResult = CSharpGeneratorDriver.Create(new WorkflowCodeGenerator())
            .RunGenerators(compilation)
            .GetRunResult();

        // assert
        var outputFile = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("TemporalClientExtensions.g.cs"));
        Assert.Equal(expectedOutput, outputFile.GetText().ToString());
    }
}