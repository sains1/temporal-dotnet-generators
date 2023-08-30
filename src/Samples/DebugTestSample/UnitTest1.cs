using NSubstitute;
using Temporalio.Activities;
using Temporalio.Generators.Workflows;

namespace DebugTestSample;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var activities = new TestMocks3();
        
        activities.MockRunAsync().Returns(Task.FromResult("goodbye"));

        var result = await activities.RunAsync();

        Assert.Equal("goodbye", result);
    }
}

[GenerateNSubstituteMocks]
public partial class TestMocks3 : ActivityMockBase<TestActivities>
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