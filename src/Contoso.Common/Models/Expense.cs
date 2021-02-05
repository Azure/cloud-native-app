using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contoso.Expenses.Common.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }

        public string Purpose { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Cost Center")]
        public string CostCenter { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Approver Email")]
        [DataType(DataType.EmailAddress)]
        public string ApproverEmail { get; set; }

        [Display(Name = "Submitter Email")]
        [DataType(DataType.EmailAddress)]
        public string SubmitterEmail { get; set; }

        [Display(Name = "Receipt Provided, if not please explain")]
        public string Receipt { get; set;}
    }
}
