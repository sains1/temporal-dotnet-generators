# Temporal Dotnet Source Generators

This repository contains a few experimental source generators for the temporal-dotnet SDK

## Contents

- [Getting Started](#getting-started)
- [Use Cases](#use-cases)
  - [1. Activity mocks](#1-activity-mocks)
  - [2. Workflow extension methods](#2-workflow-extension-methods)
  - [3. Activity methods](#3-activity-methods)
- [Debugging the code generators](#debugging-the-code-generators)
- [What does the generated code look like?](#what-does-the-generated-code-look-like)

## Getting Started

The source generators are published to the github nuget registry

1. Add nuget config for registry

<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="Nuget" value="https://api.nuget.org/v3/index.json" />
        <add key="sains1" value="https://nuget.pkg.github.com/sains1/index.json" />
    </packageSources>
</configuration>

> Note: Instructions for authenticating can be found at https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry

2. Install the packages

```
dotnet add package ActivityMockGenerator
dotnet add package ActivityGenerator
dotnet add package WorkflowGenerator
```

3. See instructions for each generator use-case below

## Use Cases:

### 1. Activity mocks

Creates test doubles for activity classes. All methods on the target class marked with an [Activity] attribute will be mocked using NSubstitue meaning the mock behaviour can be configured from test execution. This is helpful when testing workflows with lots of activity executions or where we want to verify our activity delegates have been invoked.

Usage:

Create a partial class inheriting from `ActivityMockBase` and mark it with the `[GenerateNSubstituteMocks]` attribute.

```csharp
[GenerateNSubstituteMocks]
public partial class TestMocks : ActivityMockBase<TestActivities>
{
}

public class TestActivities
{
    [Activity]
    public Task<string> RunAsync()
    {
        return Task.FromResult("hello");
    }
}
```

From a test instantiate the mock class and configure the activity behaviour using NSubstitute.

e.g. We can configure the activity RunAsync to return the string "goodbye" when invoked:

```csharp
[Fact]
public async Task Test1()
{
    // arrange
    var activities = new TestMocks();
    activities.MockRunAsync().Returns(Task.FromResult("goodbye"));

    // act
    var result = await activities.RunAsync();

    // assert
    Assert.Equal("goodbye", result);
}
```

The test above isn't very useful as we're just asserting the NSubstitute behaviour. However, it becomes more useful when we want to test Workflow executions and verify that our activities are being called correctly.

A more realistic test might look something like the below:

```csharp
// TestActivities.cs
public class TestActivities
{
    [Activity]
    public Task<string> RunAsync(string name)
    {
        return Task.FromResult("hello");
    }
}

// TestWorkflow.cs
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

// UnitTest1.cs
public class UnitTest1
{
    [Fact]
    public async Task TestWorkflow()
    {
        var taskQueueId = Guid.NewGuid().ToString();
        var inputName = "Bob";

        // setup our mock activity to return "Hello {inputName}"
        //      Note - we can be more specific with the input parameter matching if needed
        var mockActivities = new TestMocks();
        mockActivities.MockRunAsync(Arg.Any<string>()).Returns($"Hello {inputName}");

        // create test environment / worker
        //    Note - need to ensure we pass the mockActivities to the worker
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions(taskQueueId).AddWorkflow<TestWorkflow>()
                .AddAllActivities(mockActivities));

        await worker.ExecuteAsync(async () =>
        {
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

// Mark a mock class with the [GenerateNSubstituteMocks] attribute to ensure the source generator runs
[GenerateNSubstituteMocks]
public partial class TestMocks : ActivityMockBase<TestActivities>
{
}
```

### 2. Workflow extension methods

> NOTE: Not very useful in its current format

Creates a set of extension methods on the ITemporalClient for all classes marked with a [GenerateWorkflowExtension] attribute.

> TODO clarify whether this avoids issues with expression tree compilation

Usage:

Add GenerateWorkflowExtension attribute to Workflow:

```csharp
[Workflow]
[GenerateWorkflowExtension]
public class MyWorkflow
{
    [WorkflowRun]
    public Task RunAsync(string input)
    {
        return Task.CompletedTask;
    }
}
```

Invoke workflow from client:

```csharp
var client = new TemporalClient(...);
var result = client.ExecuteMyWorkflowAsync("input", options);
```

### 3. Activity methods

> NOTE: Not very useful in its current format

Creates a set of static methods on an Activities class for all classes marked with a [GenerateActivityExtension] attribute.

> TODO clarify whether this avoids issues with expression tree compilation

Usage:

Add GenerateActivityExtension attribute to Activity:

```csharp
[Activity]
[GenerateActivityExtension]
public async string MyActivity(string input)
{
    return "hello";
}
```

Invoke from Workflow:

```csharp
[WorkflowRun]
public async Task RunAsync()
{
    var result = await Activities.ExecuteMyActivity("input", options);
}
```

## Debugging the code generators

Each source generator has a launch profile with a sample project to debug the generators. The sample projects are located in the src/Samples folder.

## What does the generated code look like?

### Workflow Extension Methods:

```csharp
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
```

### Activity Methods:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Generator.Tests.ActivityGenerator;
using Temporalio.Client;
using Temporalio.Workflows;

namespace Temporalio.Generators.Activities
{
    public static partial class Activities
    {
        public static System.Threading.Tasks.Task ExecuteRunMethod(Generator.Tests.ActivityGenerator.TestRecord input, string input2, ActivityOptions options)
        {
            return Workflow.ExecuteActivityAsync(() => Generator.Tests.ActivityGenerator.TestActivities.RunMethod(input, input2), options);
        }
    }
}
```

### Activity Mocks:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using NSubstitute;
using Temporalio.Activities;

namespace Generator.Tests.ActivityMockGenerator
{
    public partial class TestMocks
    {
        public Func<Task<string>> MockRunAsync = Substitute.For<Func<Task<string>>>();

        [Activity]
        public Task<string> RunAsync()
        {
            return MockRunAsync();
        }
    }
}
```
