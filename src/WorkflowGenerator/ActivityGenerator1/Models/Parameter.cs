namespace ActivityGenerator.Models;

public class Parameter
{
    public required string ParameterName { get; set; }
    public required string ParameterType { get; set; }
    public string? OptionalParameterTypeNamespace { get; set; }
}