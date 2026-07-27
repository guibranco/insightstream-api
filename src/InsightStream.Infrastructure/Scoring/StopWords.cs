namespace InsightStream.Infrastructure.Scoring;

internal static class StopWords
{
    public static readonly HashSet<string> English = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "are", "but", "not", "you", "your", "with", "this",
        "that", "these", "those", "from", "have", "has", "had", "was", "were",
        "will", "would", "could", "should", "what", "when", "where", "which",
        "while", "about", "into", "over", "under", "than", "then", "them",
        "their", "there", "here", "how", "why", "who", "whom", "all", "any",
        "can", "did", "does", "doing", "each", "more", "most", "other", "some",
        "such", "only", "same", "very", "just", "being", "because",
    };
}
