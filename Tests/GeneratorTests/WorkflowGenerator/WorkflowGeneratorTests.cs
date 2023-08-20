using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Generator.Tests.WorkflowGenerator;

public class WorkflowGeneratorTests
{

    private const string Wf = """
                              using System.Threading.Tasks;
                              using Temporalio.Workflows;

                              namespace Generator.Tests.WorkflowGenerator;

                              [Workflow]
                              public class TestWorkflow
                              {
                                  [WorkflowRun]
                                  public Task RunAsync()
                                  {
                                      return Task.CompletedTask;
                                  }
                              }
                              """;
    
    [Fact]
    public void GeneratesExecuteExtensionMethodForWorkflowRun()
    {
        // arrange
        var driver = CSharpGeneratorDriver.Create(new WorkflowCodeGenerator());
        
        // this is the most basic workflow, no return type, no arguments, no implementation
        // var basicWorkflow = File.ReadAllText(Path.Combine("WorkflowGenerator", "BasicWorkflow.cs"));

        var syntaxTree = CSharpSyntaxTree.ParseText(Wf);
        Assert.Empty(syntaxTree.GetDiagnostics());

        var compilation = CSharpCompilation.Create(nameof(WorkflowGeneratorTests), new[] { syntaxTree })
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                // MetadataReference.CreateFromFile(typeof(Workflow).Assembly.Location))
            );
            // .AddReferences(Assembly.GetEntryAssembly()!.GetReferencedAssemblies()
            //     .Select(x => MetadataReference.CreateFromFile(Assembly.Load(x).Location)));

        // act
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // assert
        var outputFile = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("TemporalClientExtensions.g.cs"));
    }
}