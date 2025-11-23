using Microsoft.EntityFrameworkCore;
using SnapshotNewsToday.Data.Models;

namespace SnapshotNewsToday.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Article> Articles => Set<Article>();

    public ApplicationDbContext() : base()
    {

    }

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>().ToContainer("Articles");
    }
}
