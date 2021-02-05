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
    public class DetailsModel : PageModel
    {
        private readonly ContosoExpensesWebContext _context;

        public DetailsModel(ContosoExpensesWebContext context)
        {
            _context = context;
        }

        public Expense Expense { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Expense = await _context.Expense.FirstOrDefaultAsync(m => m.ExpenseId == id);

            if (Expense == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
