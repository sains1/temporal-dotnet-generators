using System.Threading.Tasks;
using Temporalio.Workflows;

namespace WorkflowGenerator.Sample;

[Workflow]
public class TestWorkflow
{
    [WorkflowRun]
    public Task RunAsync()
    {
        return Task.CompletedTask;
    }
}