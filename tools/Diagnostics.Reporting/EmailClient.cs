using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.Reporting
{
    public static class EmailClient
    {
        public static async Task<Response> SendEmail(IConfiguration config, List<string> tos, string subject, string plainTextContent, string htmlContent)
        {
            SendGridClient client = new SendGridClient(config["EmailSettings:SendGrid_API_Key"].ToString());
            List<EmailAddress> toemails = new List<EmailAddress>();
            foreach(string to in tos)
            {
                toemails.Add(new EmailAddress(to));
            }

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(new EmailAddress(config["EmailSettings:FromAddress"].ToString()), toemails, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            return response;
        }
    }
}
