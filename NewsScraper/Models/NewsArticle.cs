using System;
using System.Collections.Generic;
using System.Text;

namespace NewsScraper.Models;

internal class NewsArticle
{
    internal Uri? SourceUri { get; set; }
    internal string? SourceHeadline { get; set; }
    internal DateTime? SourcePublishDate { get; set; }
    internal string? SourceName { get; set; }
}
