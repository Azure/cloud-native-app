using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contoso.Expenses.Web.Models
{
    public class CostCenter
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string SubmitterEmail { get; set; }
        public string ApproverEmail { get; set; }
        public string CostCenterName { get; set; }
    }
}
