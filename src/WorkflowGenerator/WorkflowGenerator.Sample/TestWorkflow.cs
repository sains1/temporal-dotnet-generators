using System.Diagnostics;
using System.Threading.Tasks;
using Temporalio.Extensions.Generators.Activities;
using Temporalio.Workflows;
using WorkflowGenerator.Sample.Records;

namespace WorkflowGenerator.Sample;

[Workflow]
public class TestWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        await Activities.TestActivityExecute(new TestActivityInput(), new ActivityOptions());
        await Activities.TestActivityExecute2("", "", new ActivityOptions());
        // await Workflow.ExecuteActivityAsync((TestActivity a) => a.Execute(""), new ActivityOptions());
    }
}

[Workflow]
public class TestWorkflow2
{
    [WorkflowRun]
    public async Task<string> ExecuteRunAsync(string input1, string input2)
    {
        await Workflow.ExecuteActivityAsync((TestActivity a) => a.Execute(new TestActivityInput()), new ActivityOptions());
        return "";
    }
}

[Workflow]
public class TestWorkflow3
{
    [WorkflowRun]
    public async Task<string> ExecuteRunAsync(Test test)
    {
        await Workflow.ExecuteActivityAsync((TestActivity a) => a.Execute(new TestActivityInput()), new ActivityOptions());
        return "";
    }
}