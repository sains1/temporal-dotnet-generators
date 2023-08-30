using NSubstitute;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Generators.Workflows;
using Temporalio.Testing;
using Temporalio.Worker;
using Temporalio.Workflows;

namespace DebugTestSample;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var activities = new TestMocks();
        
        activities.MockRunAsync("name").Returns(Task.FromResult("goodbye"));

        var result = await activities.RunAsync("name");

        Assert.Equal("goodbye", result);
    }

    [Fact]
    public async Task TestWorkflow()
    {
        var taskQueueId = Guid.NewGuid().ToString();
        var inputName = "Bob";
        
        // instantiate our mock activities
        var mockActivities = new TestMocks();

        // create test environment / worker
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions(taskQueueId).AddWorkflow<TestWorkflow>()
                .AddAllActivities(mockActivities));
        
        // start worker
        await worker.ExecuteAsync(async () =>
        {
            // setup our mock activity to return "Hello {inputName}"
            //      Note - we can be more specific with the string input if needed
            mockActivities.MockRunAsync(Arg.Any<string>()).Returns($"Hello {inputName}");
            
            // execute our workflow
            var result = await env.Client.ExecuteWorkflowAsync(
                (TestWorkflow wf) => wf.RunAsync(inputName),
                new(id: $"wf-{Guid.NewGuid()}", taskQueue: taskQueueId));

            // assert the result
            Assert.Equal("Hello Bob", result);

            // verify our mock activity was invoked exactly once with the correct argument
            await mockActivities.MockRunAsync.Received(1)(inputName);
        });
    }
}

[GenerateNSubstituteMocks]
public partial class TestMocks : ActivityMockBase<TestActivities>
{
}

public class TestActivities
{
    [Activity]
    public Task<string> RunAsync(string name)
    {
        return Task.FromResult("hello");
    }
}

[Workflow]
public class TestWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(string name)
    {
        return await Workflow.ExecuteActivityAsync((TestActivities x) => x.RunAsync(name),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
    }
}