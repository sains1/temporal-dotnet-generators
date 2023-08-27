using System.Threading.Tasks;
using Temporalio.Activities;
using Temporalio.Generators.Activities;

namespace DebugSample;

public class TestActivities
{
    [Activity]
    [GenerateActivityExtension]
    public async Task<string> RunMethod1(TestActivityInput input)
    {
        await Task.Delay(1000);
        return "";
    }

    [Activity]
    [GenerateActivityExtension]
    public async Task RunMethod2(string input1, string input2)
    {
        await Task.Delay(1000);
    }
    
    [Activity]
    [GenerateActivityExtension]
    public string RunMethod3()
    {
        return "";
    }

    [Activity]
    [GenerateActivityExtension]
    public void RunMethod4()
    {
        
    }
}

public record TestActivityInput;