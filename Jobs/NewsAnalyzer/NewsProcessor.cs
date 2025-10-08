using Common.Logging;
using NewsAnalyzer.Providers;

namespace NewsAnalyzer;

public class NewsProcessor(Logger logger, PerplexityApiProvider perplexityApiProvider)
{
    private readonly Logger _logger = logger;
    private readonly PerplexityApiProvider _perplexityApiProvider = perplexityApiProvider;

    public void Run(string[] args)
    {
        _logger.Log("********** Application started **********");
        // Use _perplexityApiProvider as needed
    }
}
