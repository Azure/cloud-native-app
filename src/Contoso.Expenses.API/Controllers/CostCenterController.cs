using System;
using System.Collections.Generic;
using System.Linq;
using Contoso.Expenses.API.Database;
using Contoso.Expenses.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Contoso.Expenses.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostCenterController : ControllerBase
    {
        private readonly DatabaseContext _dbctx;

        public CostCenterController(DatabaseContext databaseContext)
        {
            _dbctx = databaseContext;
        }

        // GET: api/CostCenter
        [HttpGet]
        public IEnumerable<CostCenter> Get()
        {
            return _dbctx.CostCenters.ToList();
        }

        // GET: api/CostCenter/umar@hotmail.com
        [HttpGet("{email}", Name = "Get")]
        public CostCenter Get(string email)
        {
            var costCenter = _dbctx.CostCenters.FirstOrDefault(x => x.SubmitterEmail == email);

            if (costCenter != null)
            {
                Console.WriteLine("Cost Center with email {0} found." + email);
                return costCenter;
            }
            else
            {
                Console.WriteLine("Cost Center with email {0} not found." + email);
                return null;
            }
        }

        // POST: api/CostCenter
        [HttpPost]
        public IActionResult Post([FromBody] CostCenter model)
        {
            try
            {
                _dbctx.CostCenters.Add(model);
                _dbctx.SaveChanges();
                return StatusCode(StatusCodes.Status201Created, model);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        // DELETE: api/CostCenter/umar@hotmail.com
        [HttpDelete("{email}")]
        public IActionResult Delete(string email)
        {
            var costCenter = Get(email);

            if (costCenter != null)
            {
                _dbctx.CostCenters.Remove(Get(email));
                _dbctx.SaveChanges();
                Console.WriteLine("Cost Center with email {0} deleted successfully." + email);
                return StatusCode(StatusCodes.Status200OK, costCenter);
            }
            else
            {
                Console.WriteLine("Cost Center with email {0} not found. No action taken." + email);
                return StatusCode(StatusCodes.Status500InternalServerError, string.Format("Cost Center with email {0} not found. No action taken.", email));
            }

        }
    }
}
