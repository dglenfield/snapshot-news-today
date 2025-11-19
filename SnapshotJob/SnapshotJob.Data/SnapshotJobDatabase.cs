using Microsoft.Extensions.Options;
using SnapshotJob.Common.Logging;
using SnapshotJob.Data.Configuration.Options;
using SnapshotJob.Data.Repositories;

namespace SnapshotJob.Data;

public class SnapshotJobDatabase : SqliteDatabase
{
    private readonly DatabaseInfoRepository _databaseInfoRepository;
    private readonly NewsSnapshotRepository _newsSnapshotRepository;
    private readonly ScrapedHeadlineRepository _scrapedHeadlineRepository;
    private readonly ScrapedArticleRepository _scrapedArticleRepository;
    private readonly TopStoryApiCallRepository _topStoryApiCallRepository;
    private readonly TopStoryRepository _topStoryRepository;
    private readonly Logger _logger;
    private readonly DatabaseOptions _options;

    public SnapshotJobDatabase(IOptions<DatabaseOptions> options, Logger logger) : base(options.Value.DatabaseFilePath)
    {
        _logger = logger;
        _options = options.Value;
        _databaseInfoRepository = new(this);
        _newsSnapshotRepository = new(this);
        _scrapedHeadlineRepository = new(this);
        _scrapedArticleRepository = new(this);
        _topStoryApiCallRepository = new(this);
        _topStoryRepository = new(this);
    }

    public async Task InitializeAsync()
    {
        if (_options.DeleteExistingDatabase)
        {
            // Delete the existing database
            await DeleteAsync();
            _logger.Log($"Database deleted at '{DatabaseFilePath}'.", LogLevel.Success);
        }

        if (!File.Exists(DatabaseFilePath))
        {
            // Create the entire database
            await _databaseInfoRepository.CreateTableAsync();
            await _newsSnapshotRepository.CreateTableAsync();
            await _scrapedHeadlineRepository.CreateTableAsync();
            await _scrapedArticleRepository.CreateTableAsync();
            await _topStoryApiCallRepository.CreateTableAsync();
            await _topStoryRepository.CreateTableAsync();

            _logger.Log($"Database created at '{DatabaseFilePath}'.", LogLevel.Success);
        }
        else
        {
            // Update individual tables
            await UpdateDatabaseInfoTable();
            await UpdateTopStoryApiCallTable();
            await UpdateTopStoryTable();
        }            
    }

    private async Task UpdateDatabaseInfoTable()
    {
        var tableInfo = await _databaseInfoRepository.GetAsync("database_info");
        if (tableInfo is null)
        {
            // Create the table
            await _databaseInfoRepository.CreateTableAsync();
            _logger.Log("database_info table created.", LogLevel.Success);
        }
        else if (_databaseInfoRepository.Version > tableInfo.Version)
        {
            // Update the table
            _databaseInfoRepository.UpdateTable(tableInfo.Version);
            _logger.Log("database_info table updated.", LogLevel.Success);
        }
    }

    private async Task UpdateTopStoryTable()
    {
        var tableInfo = await _databaseInfoRepository.GetAsync("top_story");
        if (tableInfo is null)
        {
            // Create the table
            await _topStoryRepository.CreateTableAsync();
            _logger.Log("top_story table created.", LogLevel.Success);
        }
    }

    private async Task UpdateTopStoryApiCallTable()
    {
        var tableInfo = await _databaseInfoRepository.GetAsync("top_story_api_call");
        if (tableInfo is null)
        {
            // Create the table
            await _topStoryApiCallRepository.CreateTableAsync();
            _logger.Log("top_story_api_call table created.", LogLevel.Success);
        }
        else if (_topStoryApiCallRepository.Version > tableInfo.Version)
        {
            // Update the table
            _topStoryApiCallRepository.UpdateTable(tableInfo.Version);
            _logger.Log("top_story_api_call table updated.", LogLevel.Success);
        }
    }
}
