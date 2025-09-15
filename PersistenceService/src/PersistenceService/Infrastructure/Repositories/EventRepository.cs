namespace PersistenceService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class EventRepository : IEventRepository
{
    private readonly EventDbContext _dbContext;
    private readonly ILogger<EventRepository> _logger;
    private const int MaxRetries = 3;
    private const int DelayMilliseconds = 500;

    public EventRepository(EventDbContext dbContext, ILogger<EventRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                persistAction();
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Event(s) persisted successfully on attempt {Attempt}", attempt);
                return;
            }
            catch (DbUpdateException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(DelayMilliseconds * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist event(s) after {Attempt} attempts", attempt);
                throw;
            }
        }
    }
}


