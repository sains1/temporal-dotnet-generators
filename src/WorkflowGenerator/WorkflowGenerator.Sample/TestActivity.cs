using System.Threading.Tasks;
using Temporalio.Activities;

namespace WorkflowGenerator.Sample;

public class TestActivity
{
    [Activity]
    public async Task<string> Execute(TestActivityInput input)
    {
        await Task.Delay(1000);
        return "";
    }

    [Activity]
    public async Task Execute2(string input1, string input2)
    {
        await Task.Delay(1000);
    }
    
    [Activity]
    public string Execute3(string input1, string input2)
    {
        return "";
    }
    
    [Activity]
    public string Execute4()
    {
        return "";
    }
}

public record TestActivityInput;