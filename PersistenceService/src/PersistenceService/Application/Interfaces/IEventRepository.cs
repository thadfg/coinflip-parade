using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PersistenceService.Domain.Entities;

namespace PersistenceService.Application.Interfaces;

public interface IEventRepository
{
    Task SaveAsync(EventEntity entity, CancellationToken cancellationToken);
    Task SaveBatchAsync(IEnumerable<EventEntity> entities, CancellationToken cancellationToken);
}


