using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Responses;

public class PerplexityResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; }

    [JsonPropertyName("citations")]
    public List<string> Citations { get; set; }

    [JsonPropertyName("search_results")]
    public List<SearchResult> SearchResults { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; }
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("search_context_size")]
    public string SearchContextSize { get; set; }

    [JsonPropertyName("cost")]
    public Cost Cost { get; set; }
}

public class Cost
{
    [JsonPropertyName("input_tokens_cost")]
    public double InputTokensCost { get; set; }

    [JsonPropertyName("output_tokens_cost")]
    public double OutputTokensCost { get; set; }

    [JsonPropertyName("request_cost")]
    public double RequestCost { get; set; }

    [JsonPropertyName("total_cost")]
    public double TotalCost { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; }

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }
}

public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("delta")]
    public Delta Delta { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class Delta
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class CuratedArticlesResponse
{
    [JsonPropertyName("top_stories")]
    public List<Story> TopStories { get; set; }

    [JsonPropertyName("selection_criteria")]
    public JsonElement SelectionCriteriaRaw { get; set; }  // Use JsonElement to handle both string and object

    [JsonPropertyName("excluded_categories")]
    public JsonElement ExcludedCategoriesRaw { get; set; }  // Same for excluded_categories

    // Helper properties to get the actual values
    [JsonIgnore]
    public string SelectionCriteriaText =>
        SelectionCriteriaRaw.ValueKind == JsonValueKind.String
            ? SelectionCriteriaRaw.GetString()
            : SelectionCriteriaRaw.ToString();

    [JsonIgnore]
    public SelectionCriteria SelectionCriteriaObject =>
        SelectionCriteriaRaw.ValueKind == JsonValueKind.Object
            ? SelectionCriteriaRaw.Deserialize<SelectionCriteria>()
            : null;

    [JsonIgnore]
    public List<string> ExcludedCategoriesList
    {
        get
        {
            if (ExcludedCategoriesRaw.ValueKind == JsonValueKind.Array)
            {
                return ExcludedCategoriesRaw.Deserialize<List<string>>();
            }
            else if (ExcludedCategoriesRaw.ValueKind == JsonValueKind.Object)
            {
                // Handle object case from previous response
                var obj = ExcludedCategoriesRaw.Deserialize<Dictionary<string, string>>();
                return obj?.Values.ToList() ?? new List<string>();
            }
            return new List<string>();
        }
    }
}

public class SelectionCriteria
{
    [JsonPropertyName("national_international_impact")]
    public string NationalInternationalImpact { get; set; }

    [JsonPropertyName("immediacy")]
    public string Immediacy { get; set; }

    [JsonPropertyName("societal_relevance")]
    public string SocietalRelevance { get; set; }

    [JsonPropertyName("shaping_upcoming_events")]
    public string ShapingUpcomingEvents { get; set; }
}

public class Story
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("headline")]
    public string Headline { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("highlights")]
    public string Highlights { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; }
}