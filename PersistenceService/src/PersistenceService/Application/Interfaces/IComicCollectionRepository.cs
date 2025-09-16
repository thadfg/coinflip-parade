using PersistenceService.Domain.Entities;

namespace PersistenceService.Application.Interfaces;

public interface IComicCollectionRepository
{
    Task UpsertBatchAsync(IEnumerable<(ComicRecordEntity Comic, Guid EventId)> batch, CancellationToken cancellationToken);
}

