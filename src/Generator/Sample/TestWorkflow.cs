using System;
using System.Threading.Tasks;
using Temporalio.Generators.Activities;
using Temporalio.Workflows;

namespace Sample;

[Workflow]
public class TestWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync()
    {
        var result = await Activities.ExecuteRunMethod1(new TestActivityInput(),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromDays(1) });

        return result;
    }
}