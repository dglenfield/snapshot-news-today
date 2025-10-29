using Common.Logging;
using NewsAnalyzer.Providers;

namespace NewsAnalyzer.Processors;

public class NewsProcessor(PerplexityApiProvider perplexityApiProvider, Logger logger)
{
    internal async Task Run()
    {
		// News Snapshot Job
		// 1. Scrape Main Page for Headlines and links to articles
		// 2. Scrape each Article Page for the Article
		// 3. Analyze the news and select the top headlines 
		// 4. Summarize the article for each selected headline


		// news_snapshot_job
		// 
        // ap_news_scrape_job
        // ap_news_headline
        // ap_news_article

        var q1 = @"
select j.job_started_on, j.job_finished_on, h.* 
from ap_news_scrape_job j
inner join ap_news_headline h on j.id = h.job_id
where j.is_success = 1 and j.id = 1 and h.section_name in ('A1 Main Story', 'A2 Main Story', 'A3');
";

        var q2 = @"
select j.job_started_on, j.job_finished_on, h.*, a.* 
from ap_news_scrape_job j
inner join ap_news_headline h on j.id = h.job_id
inner join ap_news_article a on h.id = a.headline_id
where j.is_success = 1 and j.id = 1
and h.section_name not in ('A1 Main Story', 'A2 Main Story', 'A3')
and a.is_success = 1
order by a.last_updated_on desc;
";


        try
		{
			// Get the headlines from the database

			// Call Perplexity API to select the top headlines

			// Get the articles from the database

			// Call Perplexity API to summarize the articles
		}
		catch (Exception ex)
		{

			throw;
		}
		finally 
		{
			
		}
    }
}
