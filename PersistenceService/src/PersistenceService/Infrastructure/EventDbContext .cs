using Microsoft.EntityFrameworkCore;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Configurations;

namespace PersistenceService.Infrastructure;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<EventEntity> Events => Set<EventEntity>();    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comics");        

        modelBuilder.ApplyConfiguration(new EventEntityConfiguration());        
    }  

}
