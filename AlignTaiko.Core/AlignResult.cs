namespace AlignTaiko.Core
{
    public sealed record AlignResult(
        bool Success,
        int ChangedObjects,
        string? Error
    );
}
