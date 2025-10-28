using Common.Logging;
using NewsAnalyzer.Providers;

namespace NewsAnalyzer.Processors;

public class NewsAnalyzerProcessor(Logger logger, PerplexityApiProvider perplexityApiProvider)
{
    private readonly Logger _logger = logger;
    private readonly PerplexityApiProvider _perplexityApiProvider = perplexityApiProvider;

    public async Task Run()
    {
        // Use _perplexityApiProvider as needed

        return;
    }
}
