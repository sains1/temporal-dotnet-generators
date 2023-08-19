using System.Collections.Generic;

namespace ActivityGenerator.Models;

public class Activity
{
    public required string ActivityTypeName { get; set; }
    public required string ActivityMethodName { get; set; }
    public required string ActivityTypeNamespace { get; set; }
    public string? OptionalReturnType { get; set; }
    public required ICollection<Parameter> Parameters { get; set; }
}