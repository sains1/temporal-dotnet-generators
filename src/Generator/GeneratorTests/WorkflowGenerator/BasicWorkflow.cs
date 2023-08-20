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