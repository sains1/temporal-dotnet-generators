# Temporal Dotnet Source Generators

This repository contains a few experimental source generators for the temporal-dotnet SDK

## Contents

- [Getting Started](#getting-started)
- [Use Cases](#use-cases)
  - [1. Workflow extension methods](#1-workflow-extension-methods)
  - [2. Activity methods](#2-activity-methods)
  - [3. Activity mocks](#3-activity-mocks)
- [Debugging the code generators](#debugging-the-code-generators)
- [What does the generated code look like?](#what-does-the-generated-code-look-like)

## Getting Started

> TODO

## Use Cases:

### 1. Workflow extension methods

Creates a set of extension methods on the ITemporalClient for all classes marked with a [Workflow] attribute.

> TODO clarify whether this avoids issues with expression tree compilation

Before:

```csharp
var client = new TemporalClient(...);
var input = "input";
var result = client.ExecuteWorkflowAsync((MyWorkflow wf) => wf.RunAsync(input), options);
```

After:

```csharp
var client = new TemporalClient(...);
var result = client.ExecuteMyWorkflowAsync("input", options);
```

### 2. Activity methods

Creates a set of static methods on an Activities class for all classes marked with an [Activity] attribute.

> TODO clarify whether this avoids issues with expression tree compilation

Before:

```csharp
[WorkflowRun]
public async Task RunAsync()
{
    var input = "input";
    var result = await Workflow.ExecuteActivityAsync((MyActivity activity) => activity.RunMyActivity(input), options);
}
```

After:

```csharp
[WorkflowRun]
public async Task RunAsync()
{
    var result = await Activities.RunMyActivity("input", options);
}
```

### 3. Activity mocks

Creates test doubles for activity classes. All methods marked with an [Activity] attribute will be mocked using NSubstitue meaning the mock behaviour can be configured from test execution. This is helpful when testing workflows with lots of activity executions or where we want to verify our activity delegates have been invoked.

Before:

```csharp

```

After:

```csharp

```

## Debugging the code generators

> TODO steps for debugging

## What does the generated code look like?

### Workflow Extension Methods:

```chsarp

```

### Activity Methods:

```chsarp

```

### Activity Mocks:

```chsarp

```
