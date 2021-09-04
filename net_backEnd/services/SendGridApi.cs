using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace net_backEnd.services
{
    public static class SendGridAPI
    {
       public static async Task<bool> Execute(string userEmail, string userName, string plainTextContent, string htmlContent, string subject) // 
        {
            var apiKey = "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";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("ahmed11dev@gmail.com", "ahmed ali");
            //var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress(userEmail, userName); // 
            //var subject = "hi";
            //var plainTextContent = "good day";
            //var htmlContent = "<h2>hi man </h2>";
            // var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            return await Task.FromResult(true);
        }
    }
}
