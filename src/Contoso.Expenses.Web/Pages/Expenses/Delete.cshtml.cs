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
    public class DeleteModel : PageModel
    {
        private readonly ContosoExpensesWebContext _context;

        public DeleteModel(ContosoExpensesWebContext context)
        {
            _context = context;
        }

        [BindProperty]
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

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Expense = await _context.Expense.FindAsync(id);

            if (Expense != null)
            {
                _context.Expense.Remove(Expense);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
