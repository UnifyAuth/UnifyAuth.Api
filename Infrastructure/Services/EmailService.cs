using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EmailService : IEMailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IResult> SendAsync(string to, string subject, string htmlContent)
        {
            var email = new MimeMessage();
            var emailSettings = _configuration.GetSection("EmailSettings");
            var from = emailSettings["From"];

            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Plain) { Text = htmlContent };

            SmtpClient smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(emailSettings["Host"], int.Parse(emailSettings["Port"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(emailSettings["From"], emailSettings["Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return new SuccessResult("Email sent successfully");

            }
            catch (Exception ex)
            {
                return new ErrorResult($"Failed to send email: {ex.Message}", "EmailError");
            }
            finally
            {
                smtp.Dispose();
            }


        }
    }
}
