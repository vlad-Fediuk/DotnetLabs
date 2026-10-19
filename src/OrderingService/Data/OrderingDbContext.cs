using Microsoft.EntityFrameworkCore;
using OrderingService.Domain;

namespace OrderingService.Data;

public class OrderingDbContext : DbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.IdempotencyKey).IsUnique();
            entity.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(o => o.IdempotencyKey).HasMaxLength(200);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(r => r.Key);
            entity.Property(r => r.Key).HasMaxLength(200);
            entity.Property(r => r.ResponseBody).IsRequired();
        });
    }
}
