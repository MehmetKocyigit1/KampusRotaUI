using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KampusRota.Api.Data;

public class KampusRotaDbContextFactory : IDesignTimeDbContextFactory<KampusRotaDbContext>
{
    public KampusRotaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KampusRotaDbContext>();
        optionsBuilder.UseSqlite("Data Source=kampusrota.db");

        return new KampusRotaDbContext(optionsBuilder.Options);
    }
}
