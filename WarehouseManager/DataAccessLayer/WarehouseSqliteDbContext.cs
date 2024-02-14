using Microsoft.EntityFrameworkCore;

namespace WarehouseManager.DataAccessLayer;

public class WarehouseSqliteDbContext: DbContext
{
    public DbSet<WarehouseItem> WarehouseItems { get; set; } = null!;

    private readonly IConfiguration _configuration;

    public WarehouseSqliteDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(_configuration.GetConnectionString("SqliteDatabase"));
    }
}
