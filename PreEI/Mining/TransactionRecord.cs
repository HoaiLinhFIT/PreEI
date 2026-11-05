namespace PreEIApplication.Mining;

/// <summary>
/// A profit-augmented transaction. The transaction ID is one-based and preserves
/// the original input order, as required by the difference-list implementation.
/// </summary>
internal sealed class TransactionRecord
{
    internal int TransactionId { get; init; }
    internal int Profit { get; init; }
    internal List<int> ItemIds { get; } = new();
}
