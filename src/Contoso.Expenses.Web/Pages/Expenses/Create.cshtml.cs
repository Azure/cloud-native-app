using App.Metrics;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Http;
using CloudNative.CloudEvents.NewtonsoftJson;
using Contoso.Expenses.Common.Models;
using Contoso.Expenses.Web.AppMetrics;
using Contoso.Expenses.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Contoso.Expenses.Web.Pages.Expenses
{
    public class CreateModel : PageModel
    {
        private readonly ContosoExpensesWebContext _context;
        private string costCenterAPIUrl;
        private readonly QueueInfo _queueInfo;
        private readonly IWebHostEnvironment _env;
        private readonly IMetrics _metrics;

        private string Source { get; } = "urn:contoso.web";
        private string Type { get; } = "contoso.web.dispatchemail";

        public CreateModel(ContosoExpensesWebContext context, IOptions<ConfigValues> config, QueueInfo queueInfo,
                            IWebHostEnvironment env, IMetrics metrics)
        {
            _metrics = metrics;
            _context = context;
            costCenterAPIUrl = config.Value.CostCenterAPIUrl;
            _queueInfo = queueInfo;
            _env = env;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Expense Expense { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Look up cost center
            CostCenter costCenter = await GetCostCenterAsync(costCenterAPIUrl, Expense.SubmitterEmail);
            if (costCenter != null)
            {
                Expense.CostCenter = costCenter.CostCenterName;
                Expense.ApproverEmail = costCenter.ApproverEmail;
            }
            else
            {
                Expense.CostCenter = "Unkown";
                Expense.ApproverEmail = "Unknown";
            }

            // Write to DB, but don't wait right now
            _context.Expense.Add(Expense);
            Task t = _context.SaveChangesAsync();

            if (!_env.IsEnvironment("Development"))
            {
                var cloudEvent = new CloudEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = Type,
                    Source = new Uri(Source),
                    DataContentType = MediaTypeNames.Application.Json,
                    Data = JsonConvert.SerializeObject(Expense)
                };

                var content = cloudEvent.ToHttpContent(ContentMode.Structured, new JsonEventFormatter());

                var httpClient = new HttpClient();
                var result = await httpClient.PostAsync(_queueInfo.ConnectionString, content);
            }
            // Ensure the DB write is complete
            t.Wait();

            // This is to track customer App Metrics counter to track number of expenses submitted 
            _metrics.Measure.Counter.Increment(MetricsRegistry.CreatedExpenseCounter);

            return RedirectToPage("./Index");
        }

        private static async Task<CostCenter> GetCostCenterAsync(string apiBaseURL, string email)
        {
            string requestUri = "api/costcenter" + "/" + email;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                if (httpResponse.IsSuccessStatusCode)
                {
                    //CostCenter costCenter = await httpResponse.Content.ReadAsAsync<CostCenter>();
                    var response = await httpResponse.Content.ReadAsStringAsync();
                    var costCenter = JsonConvert.DeserializeObject<CostCenter>(response);

                    if (costCenter != null)
                        Console.WriteLine("SubmitterEmail: {0} \r\n ApproverEmail: {1} \r\n CostCenterName: {2}",
                            costCenter.SubmitterEmail, costCenter.ApproverEmail, costCenter.CostCenterName);
                    return costCenter;
                }
                else
                {
                    Console.WriteLine("Internal server error: " + httpResponse.StatusCode);
                    return null;
                }
            }
        }
    }
}
