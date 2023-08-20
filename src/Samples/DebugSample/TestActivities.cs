using System.Threading.Tasks;
using Temporalio.Activities;

namespace Sample;

public class TestActivities
{
    [Activity]
    public async Task<string> RunMethod1(TestActivityInput input)
    {
        await Task.Delay(1000);
        return "";
    }

    [Activity]
    public async Task RunMethod2(string input1, string input2)
    {
        await Task.Delay(1000);
    }
    
    [Activity]
    public string RunMethod3()
    {
        return "";
    }

    [Activity]
    public void RunMethod4()
    {
        
    }
}

public record TestActivityInput;