// See https://aka.ms/new-console-template for more information

using Temporalio.Client;
using Temporalio.Extensions.Generators.Workflow;
using WorkflowGenerator.Sample.Records;

var client = await TemporalClient.ConnectAsync(new()
{
    TargetHost = "localhost:7233",
    // In production, pass options to configure TLS and other settings
});

await client.ExecuteTestWorkflowAsync(new WorkflowOptions
{
    TaskQueue = "test-queue",
    Id = "test-workflow",
});

var result2 = await client.ExecuteTestWorkflow2Async("", "", new WorkflowOptions
{
    TaskQueue = "test-queue",
    Id = "test-workflow",
});

var result3 = await client.ExecuteTestWorkflow3Async(new Test() ,new WorkflowOptions
{
    TaskQueue = "test-queue",
    Id = "test-workflow",
});