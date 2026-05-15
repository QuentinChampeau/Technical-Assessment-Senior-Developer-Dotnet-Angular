using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    AuditInterceptor auditInterceptor)
    : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEntryEntityTypeConfiguration());
    }
}