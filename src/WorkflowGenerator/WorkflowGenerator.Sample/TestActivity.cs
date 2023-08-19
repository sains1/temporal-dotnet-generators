using System.Threading.Tasks;
using Temporalio.Activities;

namespace WorkflowGenerator.Sample;

public class TestActivity
{
    [Activity]
    public async Task Execute(string input)
    {
        await Task.Delay(1000);
    }
}