using Contoso.Expenses.Common.Models;
using Microsoft.AspNetCore.Http;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contoso.Expenses.OpenFaaS
{
    public class EmailHandler
    {
        public async Task<(int, string)> Handle(HttpRequest request)
        {
            var reader = new StreamReader(request.Body);
            var input = await reader.ReadToEndAsync();
            Expense expense = JsonSerializer.Deserialize<Expense>(input);
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            var client = new SendGridClient(apiKey);

            string emailFrom = "Expense@ContosoExpenses.com";
            string emailTo = expense.ApproverEmail;
            string emailSubject = $"New Expense for the amount of ${expense.Amount} submitted";
            string emailBody = $"Hello {expense.ApproverEmail}, <br/> New Expense report submitted for the purpose of: {expense.Purpose}. <br/> Please review as soon as possible. <br/> <br/> <br/> This is a auto generated email, please do not reply to this email";

            Console.WriteLine($"Email Subject: {emailSubject}");
            Console.WriteLine($"Email body: {emailBody}");

            var message = new SendGridMessage();
            message.From = new EmailAddress(emailFrom, "Contoso Expenses");
            message.AddTo(emailTo, "Srikant Sarwa");
            message.Subject = emailSubject;
            message.AddContent(MimeType.Html, emailBody);

            var response = await client.SendEmailAsync(message);

            Console.WriteLine($"Email sent successfully to: {emailTo}");

            Console.WriteLine($"Input received is: {input}");
            return (200, $"Input received is: {input}");
        }
    }
}
