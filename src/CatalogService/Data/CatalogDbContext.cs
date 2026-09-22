using Microsoft.EntityFrameworkCore;
using CatalogService.Models;

namespace CatalogService.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Slug).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.Sku).IsUnique();
            entity.HasIndex(i => i.Title);
            entity.Property(i => i.Title).HasMaxLength(255).IsRequired();
            entity.Property(i => i.Sku).HasMaxLength(100).IsRequired();
            entity.Property(i => i.Price).HasPrecision(18, 2);
            entity.Property(i => i.IsActive).HasDefaultValue(true);
            entity.Property(i => i.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        var electronicsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var booksId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = electronicsId,
                Name = "Electronics",
                Slug = "electronics",
                Description = "Electronic devices and gadgets"
            },
            new Category
            {
                Id = booksId,
                Name = "Books",
                Slug = "books",
                Description = "Books and literature"
            }
        );

        modelBuilder.Entity<Item>().HasData(
            new Item
            {
                Id = Guid.Parse("11111111-2222-3333-4444-555555555551"),
                Title = "Smartphone X",
                Sku = "ELEC-SMX-001",
                Description = "Flagship smartphone with high-end display",
                Price = 799.99m,
                StockQuantity = 50,
                CategoryId = electronicsId,
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Item
            {
                Id = Guid.Parse("11111111-2222-3333-4444-555555555552"),
                Title = "Wireless Headphones Pro",
                Sku = "ELEC-WHP-002",
                Description = "Noise-cancelling over-ear headphones",
                Price = 199.99m,
                StockQuantity = 120,
                CategoryId = electronicsId,
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Item
            {
                Id = Guid.Parse("11111111-2222-3333-4444-555555555553"),
                Title = "Domain-Driven Design Book",
                Sku = "BOOK-DDD-003",
                Description = "Tackling Complexity in the Heart of Software",
                Price = 49.99m,
                StockQuantity = 200,
                CategoryId = booksId,
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
