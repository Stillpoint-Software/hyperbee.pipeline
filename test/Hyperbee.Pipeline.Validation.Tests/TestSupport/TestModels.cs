namespace Hyperbee.Pipeline.Validation.Tests.TestSupport;

public record TestOutput
{
    public string Name { get; init; } = string.Empty;
    public int ProcessedAge { get; init; }
}

public record RuleSetModel
{
    public string Name { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string VersionTag { get; init; } = string.Empty;
    public decimal Value { get; init; }
}
