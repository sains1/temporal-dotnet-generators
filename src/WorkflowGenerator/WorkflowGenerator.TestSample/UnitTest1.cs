using NSubstitute;
using WorkflowGenerator.Sample;

namespace WorkflowGenerator.TestSample;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        // arrange
        var input = new TestActivityInput();
        var activityMocks = new TestActivitiesDouble();
        activityMocks.ExecuteMock(Arg.Any<TestActivityInput>()).Returns(Task.FromResult("hello"));
        activityMocks.Execute2Mock(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        activityMocks.Execute3Mock(Arg.Any<string>(), Arg.Any<string>()).Returns("");

        // act
        var result = await activityMocks.Execute(input);

        // assert
        await activityMocks.ExecuteMock.Received(1)(Arg.Is<TestActivityInput>(x => x == input));
    }
}