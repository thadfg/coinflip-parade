using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PersistenceService.Domain.Entities;

namespace PersistenceService.Application.Interfaces;

public interface IComicCollectionRepository
{
    Task UpsertBatchAsync(IEnumerable<(ComicRecordEntity Comic, System.Guid EventId)> batch, CancellationToken cancellationToken);
}

