using System.Threading.Tasks;
using Temporalio.Activities;

namespace WorkflowGenerator.Sample;

public class TestActivity
{
    [Activity]
    public async Task Execute(TestActivityInput input)
    {
        await Task.Delay(1000);
    }

    [Activity]
    public async Task Execute2(string input1, string input2)
    {
        await Task.Delay(1000);
    }
}

public record TestActivityInput;