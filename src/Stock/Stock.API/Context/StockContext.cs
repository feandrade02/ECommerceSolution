using Microsoft.EntityFrameworkCore;
using Stock.API.Domain.Entities;

namespace Stock.API.Context;

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
