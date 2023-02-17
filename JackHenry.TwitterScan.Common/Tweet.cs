using System.Text.Json.Serialization;

namespace JackHenry.TwitterScan;

public class TweetDataWrapper
{
    public TweetDataWrapper() { }
    public TweetDataWrapper(params string[] hashtags) =>
        Data = new Tweet(hashtags);

    [JsonPropertyName("data")]
    public Tweet Data { get; init; } = null!;
}

public class Tweet
{
    public Tweet() { }
    public Tweet(params string[] hashtags) => 
        Entities = new TweetEntities
        {
            Hashtags = hashtags
                .Select(_ => new TweetTagEntity { Tag = _ })
                .ToArray()
        };
    public IEnumerable<string>? Hashtags => Entities?.Hashtags?.Select(_ => _.Tag);

    [JsonPropertyName("id")] public string Id { get; set; } = null!;
    [JsonPropertyName("text")] public string Text { get; set; } = null!;
    [JsonPropertyName("edit_history_tweet_ids")] public string[]? EditHistoryTweetIds { get; set; }
    [JsonPropertyName("attachments")] public Dictionary<string, string[]>? Attachments { get; set; }
    [JsonPropertyName("author_id")] public string AuthorId { get; set; } = null!;
    [JsonPropertyName("context_annotations")] public TweetContextAnnotation[]? ContextAnnotations { get; set; }
    [JsonPropertyName("conversation_id")] public string ConversationId { get; set; } = null!;
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("edit_controls")] public TweetEditControls? EditControls { get; set; }
    [JsonPropertyName("entities")] public TweetEntities? Entities { get; set; }
    [JsonPropertyName("in_reply_to_user_id")] public string InReplyToUserId { get; set; } = null!;
    [JsonPropertyName("lang")] public string Language { get; set; } = null!;
    [JsonPropertyName("non_public_metrics")] public TweetNonPublicMetrics? NonPublicMetrics { get; set; }
    [JsonPropertyName("organic_metrics")] public TweetBasicMetrics? OrganicMetrics { get; set; }
    [JsonPropertyName("possibly_sensitive")] public bool IsPossiblySensitive { get; set; }
    [JsonPropertyName("promoted_metrics")] public TweetBasicMetrics? PromotedMetrics { get; set; }
    [JsonPropertyName("public_metrics")] public TweetPublicMetrics? PublicMetrics { get; set; }
    [JsonPropertyName("referenced_tweets")] public TweetReferencedTweet[]? ReferencedTweet { get; set; }
    [JsonPropertyName("reply_settings")] public string ReplySettings { get; set; } = null!;
    [JsonPropertyName("source")] public string Source { get; set; } = null!;
    [JsonPropertyName("withheld")] public TweetWithheld? Withheld { get; set; }
}

public class TweetContextAnnotation
{
    [JsonPropertyName("domain")] public TweetContextAnnotationItem Domain { get; set; } = null!; 
    [JsonPropertyName("entity")] public TweetContextAnnotationItem Entity { get; set; } = null!;
}

public class TweetContextAnnotationItem
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;
    [JsonPropertyName("name")] public string Name { get; set; } = null!;
    [JsonPropertyName("description")] public string Description { get; set; } = null!;
}

public class TweetEditControls
{
    [JsonPropertyName("edits_remaining")] public int EditsRemaining { get; set; }
    [JsonPropertyName("is_edit_eligible")] public bool IsEditEligible { get; set; }
    [JsonPropertyName("editable_until")] public DateTime EditableUntil { get; set; }
}

public class TweetEntities
{
    [JsonPropertyName("annotations")] public TweetAnnotationEntity[]? Annotations { get; set; }
    [JsonPropertyName("cashtags")] public TweetTagEntity[]? Cashtags { get; set; }
    [JsonPropertyName("hashtags")] public TweetTagEntity[]? Hashtags { get; set; }
    [JsonPropertyName("mentions")] public TweetTagEntity[]? Mentions { get; set; }
    [JsonPropertyName("urls")] public TweetUrlEntity[]? Urls { get; set; }
}

public class TweetAnnotationEntity
{
    [JsonPropertyName("start")] public int Start { get; set; }
    [JsonPropertyName("end")] public int End { get; set; }
    [JsonPropertyName("probability")] public float Probability { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = null!;
    [JsonPropertyName("normalized_text")] public string NormalizedText { get; set; } = null!;
}

public class TweetTagEntity
{
    [JsonPropertyName("start")] public int start { get; set; }
    [JsonPropertyName("end")] public int end { get; set; }
    [JsonPropertyName("tag")] public string Tag { get; set; } = null!;
}

public class TweetUrlEntity
{
    [JsonPropertyName("start")] public int start { get; set; }
    [JsonPropertyName("end")] public int end { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; } = null!;
    [JsonPropertyName("expanded_url")] public string ExpandedUrl { get; set; } = null!;
    [JsonPropertyName("display_url")] public string DisplayUrl { get; set; } = null!;
    [JsonPropertyName("status")] public string Status { get; set; } = null!;
    [JsonPropertyName("title")] public string Title { get; set; } = null!;
    [JsonPropertyName("description")] public string Description { get; set; } = null!;
    [JsonPropertyName("unwound_url")] public string UnwoundUrl { get; set; } = null!;
}

public class TweetNonPublicMetrics
{
    [JsonPropertyName("impression_count")] public int ImpressionCount { get; set; }
    [JsonPropertyName("url_link_clicks")] public int UrlLinkClicks { get; set; }
    [JsonPropertyName("user_profile_clicks")] public int UserProfileClicks { get; set; }
}

public class TweetBasicMetrics
{
    [JsonPropertyName("impression_count")] public int ImpressionCount { get; set; }
    [JsonPropertyName("like_count")] public int LikeCount { get; set; }
    [JsonPropertyName("reply_count")] public int ReplyCount { get; set; }
    [JsonPropertyName("retweet_count")] public int RetweetClicks { get; set; }
    [JsonPropertyName("url_link_clicks")] public int UrlLinkClicks { get; set; }
    [JsonPropertyName("user_profile_clicks")] public int UserProfileClicks { get; set; }
}

public class TweetPublicMetrics
{
    [JsonPropertyName("retweet_count")] public int RetweetClicks { get; set; }
    [JsonPropertyName("reply_count")] public int ReplyCount { get; set; }
    [JsonPropertyName("like_count")] public int LikeCount { get; set; }
    [JsonPropertyName("quote_count")] public int QuoteCount { get; set; }
}

public class TweetReferencedTweet
{
    [JsonPropertyName("type")] public string Type { get; set; } = null!;
    [JsonPropertyName("id")] public string Id { get; set; } = null!;
}

public class TweetWithheld
{
    [JsonPropertyName("copyright")] public bool Copyright { get; set; }
    [JsonPropertyName("country_code")] public string CountryCode { get; set; } = null!;
}