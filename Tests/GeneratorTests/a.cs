using System.Threading.Tasks;
using Temporalio.Activities;

namespace Sample
{
    public class TestActivities
    {
        [Temporalio.Activities.ActivityAttribute]
        public async Task<string> RunMethod1(string input)
        {
            await Task.Delay(1000);
            return "";
        }
    }
}