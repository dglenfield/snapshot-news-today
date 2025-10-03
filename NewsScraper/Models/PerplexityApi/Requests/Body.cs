using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Requests;

/// <summary>
/// The request body for the Sonar Chat Completions API.
/// </summary>
internal class Body : BodyBase
{
    /// <summary>
    /// The formatting to use for the response.
    /// </summary>
    //[JsonPropertyOrderAttribute(5)]
    public ResponseFormat Response_Format { get; set; } = new();
}
