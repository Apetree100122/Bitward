﻿using Bit.Core;
using Bit.Core.Utilities;
using Bit.Infrastructure.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.SqliteMigrations;

public class SqliteDbMigrator : IDbMigrator
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SqliteDbMigrator> _logger;

    public SqliteDbMigrator(IServiceScopeFactory serviceScopeFactory, ILogger<SqliteDbMigrator> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public bool MigrateDatabase(bool enableLogging = true,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        if (enableLogging && _logger != null)
        {
            _logger.LogInformation(Constants.BypassFiltersEventId, "Migrating database.");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        databaseContext.Database.Migrate();

        if (enableLogging && _logger != null)
        {
            _logger.LogInformation(Constants.BypassFiltersEventId, "Migration successful.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }
}
