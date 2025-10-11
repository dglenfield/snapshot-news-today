using Common.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewsScraper.Data;

internal class RawContentRepository(string databaseFilePath, string databaseVersion, Logger logger)
{
    public string DatabaseFilePath => _databaseFilePath;

    private readonly string _connectionString = $"Data Source={databaseFilePath};Pooling=False";
    private readonly string _databaseFilePath = string.IsNullOrWhiteSpace(databaseFilePath) ?
            throw new ArgumentNullException(nameof(databaseFilePath)) : databaseFilePath;
    private readonly string _databaseVersion = string.IsNullOrWhiteSpace(databaseVersion) ?
            throw new ArgumentNullException(nameof(databaseVersion)) : databaseVersion;
    private readonly string _directoryPath = Path.GetDirectoryName(databaseFilePath) ?? throw new DirectoryNotFoundException("Directory path missing or invalid.");
    private readonly string _fileName = Path.GetFileName(databaseFilePath) ?? throw new DirectoryNotFoundException("File name missing or invalid.");
    private readonly Logger _logger = logger;
}
