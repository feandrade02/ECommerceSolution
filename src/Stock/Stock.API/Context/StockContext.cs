using Microsoft.EntityFrameworkCore;
using Stock.Domain.Entities;

namespace Stock.Context;

public class StockContext : DbContext
{
    public StockContext(DbContextOptions<StockContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>().Property(p => p.Preco).HasPrecision(18, 2);
    }
}
