using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Request;

public class TypeSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}
