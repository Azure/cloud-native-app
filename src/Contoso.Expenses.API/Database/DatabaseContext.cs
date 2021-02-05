using Contoso.Expenses.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Expenses.API.Database
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<CostCenter> CostCenters { get; set; }
    }
}
