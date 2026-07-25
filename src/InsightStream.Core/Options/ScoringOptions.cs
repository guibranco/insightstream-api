namespace InsightStream.Core.Options;

public class ScoringOptions
{
    public const string SectionName = "Scoring";

    /// <summary>Weight applied to the author interaction factor.</summary>
    public double AuthorWeight { get; set; } = 0.35;

    /// <summary>Weight applied to the keyword preference factor.</summary>
    public double KeywordWeight { get; set; } = 0.25;

    /// <summary>Weight applied to the recency decay factor.</summary>
    public double RecencyWeight { get; set; } = 0.20;

    /// <summary>Weight applied to the newsletter popularity factor.</summary>
    public double PopularityWeight { get; set; } = 0.20;

    /// <summary>Number of days over which the recency factor linearly decays to zero.</summary>
    public int RecencyDecayDays { get; set; } = 30;

    /// <summary>Maximum newsletter appearance count considered for the popularity factor.</summary>
    public int PopularityCap { get; set; } = 3;

    /// <summary>Minimum token length considered for keyword matching.</summary>
    public int MinKeywordLength { get; set; } = 4;

    /// <summary>How long a computed score stays cached in Redis.</summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Preference adjustment applied to the author on "Liked".</summary>
    public decimal AuthorLikeAdjustment { get; set; } = 0.2m;

    /// <summary>Preference adjustment applied to the author on "DiscardedAfterReview".</summary>
    public decimal AuthorReviewDiscardAdjustment { get; set; } = -0.1m;

    /// <summary>Preference adjustment applied to the author on "Discarded".</summary>
    public decimal AuthorDiscardAdjustment { get; set; } = -0.2m;
}
