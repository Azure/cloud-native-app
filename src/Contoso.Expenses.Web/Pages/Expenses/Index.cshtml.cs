using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Contoso.Expenses.Common.Models;

namespace Contoso.Expenses.Web.Pages.Expenses
{
    public class IndexModel : PageModel
    {
        private readonly ContosoExpensesWebContext _context;

        public IndexModel(ContosoExpensesWebContext context)
        {
            _context = context;
        }

        public IList<Expense> Expense { get;set; }

        public async Task OnGetAsync()
        {
            Expense = await _context.Expense.ToListAsync();
        }
    }
}
