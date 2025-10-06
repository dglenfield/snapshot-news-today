using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Request;

public class ArraySchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("minItems")]
    public int? MinItems { get; set; }

    [JsonPropertyName("maxItems")]
    public int? MaxItems { get; set; }

    [JsonPropertyName("items")]
    public object Items { get; set; } // Can be TypeSchema or ObjectSchema
}
