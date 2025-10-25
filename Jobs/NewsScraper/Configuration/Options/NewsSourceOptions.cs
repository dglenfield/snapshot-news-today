using NewsScraper.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NewsScraper.Configuration.Options;

public class NewsSourceOptions
{
    public const string SectionName = "NewsSources";

    [Required]
    public AssociatedPressOptions AssociatedPress { get; set; } = new();
    public class AssociatedPressOptions
    {
        public const string SectionName = "AssociatedPress";

        [Required]
        public Uri BaseUri { get; set; } = default!;

        [Required]
        public ScraperOptions Scrapers { get; set; } = new();
        public class ScraperOptions
        {
            public ArticlePageOptions ArticlePage { get; set; } = new();
            public class ArticlePageOptions
            {
                [Required]
                public bool Skip { get; set; }

                [Required]
                public string TestFile { get; set; } = default!;

                [Required]
                public bool UseTestFile { get; set; }
            }

            public MainPageOptions MainPage { get; set; } = new();
            public class MainPageOptions
            {
                [Required]
                public bool Skip { get; set; }

                [Required]
                public string TestFile { get; set; } = default!;

                [Required]
                public bool UseTestFile { get; set; }
            }
        }
    }

    [Required]
    public CnnOptions CNN { get; set; } = new();
    public class CnnOptions
    {
        [Required]
        public Uri BaseUri { get; set; } = default!;

        [Required]
        public ScraperOptions Scrapers { get; set; } = new();
        public class ScraperOptions
        {
            [Required]
            public ArticlePageOptions ArticlePage { get; set; } = new();
            public class ArticlePageOptions
            {
                [Required]
                public string TestFile { get; set; } = default!;

                [Required]
                public bool UseTestFile { get; set; }
            }

            [Required]
            public MainPageOptions MainPage { get; set; } = new();
            public class MainPageOptions
            {
                [Required]
                public string TestFile { get; set; } = default!;

                [Required]
                public bool UseTestFile { get; set; }
            }
        }
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
