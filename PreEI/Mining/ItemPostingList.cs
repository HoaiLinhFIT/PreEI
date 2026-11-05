namespace PreEIApplication.Mining;

/// <summary>
/// Posting-list representation of a candidate itemset. Gain is the sum of
/// transaction profits covered by that candidate; TransactionIds is kept in
/// ascending order by the input reader and the difference-list operations.
/// </summary>
internal sealed class ItemPostingList
{
    internal int ItemId { get; set; } = -1;
    internal int Gain { get; set; }
    internal List<int> TransactionIds { get; set; } = new();
}
