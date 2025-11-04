using Common.Data.Repositories;
using Common.Logging;
using Common.Models;
using Common.Models.AssociatedPress;

namespace NewsSnapshot.Processors;

internal class NewsSnapshotProcessor(APNewsProcessor apNewsProcessor, 
    NewsSnapshotJobRepository newsSnapshotJobRepository,
    Logger logger)
{
    internal async Task Run(NewsSnapshotJob job)
    {
		try
		{
            // Insert initial job record to track this session
            job.Id = await newsSnapshotJobRepository.CreateAsync(job);

            //await apNewsProcessor.Run(job.APNewsScrape);

            job.IsSuccess = true;
        }
		catch (Exception ex)
		{
            job.IsSuccess = false;
            job.JobException = new JobException() { Source = $"{nameof(NewsSnapshotProcessor)}.{nameof(Run)}", Exception = ex };
            throw;
        }
        finally
        {
            // Update job record with results
            job.FinishedOn = DateTime.UtcNow;
            await newsSnapshotJobRepository.UpdateAsync(job);

            // Log the results
            job.WriteToLog(logger);
            logger.Log($"\nNews snapshot job finished {(job.IsSuccess!.Value ? "successfully" : "unsuccessfully")}.",
                messageLogLevel: (job.IsSuccess!.Value ? LogLevel.Success : LogLevel.Error));
        }
    }
}
