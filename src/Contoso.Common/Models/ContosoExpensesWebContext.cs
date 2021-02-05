using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Expenses.Common.Models
{
    // see https://docs.microsoft.com/en-us/ef/core/get-started/aspnetcore/new-db?tabs=visual-studio
    public class ContosoExpensesWebContext : DbContext
    {
        public ContosoExpensesWebContext (DbContextOptions<ContosoExpensesWebContext> options)
            : base(options)
        {
        }

        public DbSet<Expense> Expense { get; set; }
    }
}
