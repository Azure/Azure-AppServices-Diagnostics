using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.Reporting
{
    public static class EmailClient
    {
        public static SendGridMessage InitializeMessage(IConfiguration config, string subject, List<string> toList, List<string> ccList = null)
        {
            SendGridMessage msg = new SendGridMessage()
            {
                From = new EmailAddress(config["EmailSettings:From:Email"].ToString(),
                config["EmailSettings:From:Name"].ToString())
            };

            msg.SetSubject(subject);

            if (toList != null && toList.Any())
            {
                msg.AddTos(toList.Select(p => new EmailAddress(p)).ToList());
            }

            if (ccList != null && ccList.Any())
            {
                msg.AddCcs(ccList.Select(p => new EmailAddress(p)).ToList());
            }

            return msg;
        }

        public static async Task<Response> SendEmail(IConfiguration config, SendGridMessage msg, string htmlContent, string plainContent = "")
        {
            msg.AddContent(MimeType.Html, htmlContent);
            if (!string.IsNullOrWhiteSpace(plainContent))
            {
                msg.AddContent(MimeType.Text, plainContent);
            }

            var sendGridClient = new SendGridClient(config["EmailSettings:SendGrid_API_Key"].ToString());
            var response = await sendGridClient.SendEmailAsync(msg);

            return response;
        }
    }
}
