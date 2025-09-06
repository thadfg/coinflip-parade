using Microsoft.EntityFrameworkCore;
using PersistenceService.Domain.Entities;
using System.Collections.Generic;

namespace PersistenceService.Infrastructure;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<EventEntity> Events => Set<EventEntity>();
}
