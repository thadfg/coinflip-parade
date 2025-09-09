using Microsoft.EntityFrameworkCore;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Configurations;
using System.Collections.Generic;

namespace PersistenceService.Infrastructure;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<ComicRecordEntity> Comic => Set<ComicRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ComicRecordConfiguration());
    }  

}
