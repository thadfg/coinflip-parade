namespace PersistenceService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using PersistenceService.Config;

public class EventRepository : IEventRepository
{
    private readonly EventDbContext _dbContext;
    private readonly ILogger<EventRepository> _logger;
    private readonly RepositoryOptions _options;

    public EventRepository(EventDbContext dbContext, ILogger<EventRepository> logger, IOptions<RepositoryOptions> options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
    }

    public async Task SaveAsync(EventEntity entity, CancellationToken cancellationToken)
    {
        await SaveInternalAsync(() => _dbContext.Events.Add(entity), cancellationToken);
    }

    public async Task SaveBatchAsync(IEnumerable<EventEntity> entities, CancellationToken cancellationToken)
    {
        await SaveInternalAsync(() => _dbContext.Events.AddRange(entities), cancellationToken);
    }

    private async Task SaveInternalAsync(Action persistAction, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                persistAction();
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Event(s) persisted successfully on attempt {Attempt}", attempt);
                return;
            }
            catch (DbUpdateException ex) when (attempt < _options.MaxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(_options.DelayMilliseconds * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist event(s) after {Attempt} attempts", attempt);
                throw;
            }
        }
    }
}


