namespace ActivityGenerator.Constants;

public static class TemporalConstants
{
    public const string RootNamespace = "Temporalio";

    public static class Workflow
    {
        public const string WorkflowName = "Workflow";
        public const string WorkflowsNamespace = RootNamespace + ".Workflows";
        public const string WorkflowAttributeName = "WorkflowAttribute";
        public const string WorkflowAttributeFullName = WorkflowsNamespace + "." + WorkflowAttributeName;
        public const string WorkflowRunAttributeName = "WorkflowRunAttribute";
        public const string WorkflowRunAttributeFullName = WorkflowsNamespace + "." + WorkflowRunAttributeName;
        public const string ExecuteActivityAsyncMethodName = "ExecuteActivityAsync";
        
    }
    
    public static class Activity
    {
        public const string ActivitiesNamespace = RootNamespace + ".Activities";
        public const string ActivityAttributeName = "ActivityAttribute";
        public const string ActivityAttributeFullName = ActivitiesNamespace + "." + ActivityAttributeName;
        public const string ActivityOptionsName = "ActivityOptions";
    }
}