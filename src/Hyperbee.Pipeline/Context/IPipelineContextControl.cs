namespace Hyperbee.Pipeline.Context;

public interface IPipelineContextControl
{
    CancellationToken CancellationToken { get; }
    public bool HasCancellationValue { get; }
    object CancellationValue { get; set; }
    int Id { get; set; }
    string Name { get; set; }

    int GetNextId();
}
