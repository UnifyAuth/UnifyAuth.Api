using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Interfaces.Services;
using Azure;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using Org.BouncyCastle.Crypto;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Infrastructure.Services
{
    public class EmailService : IEMailService
    {
        private readonly ISendGridClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, ISendGridClient client)
        {
            _configuration = configuration;
            _logger = logger;
            _client = client;
        }


        public async Task<IResult> SendAsync(string to, string subject, string emailContent)
        {
            var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from,toAddress,subject,emailContent,emailContent);
            var result = await _client.SendEmailAsync(msg);
            if (!result.IsSuccessStatusCode)
            {
                var body = await result.Body.ReadAsStringAsync();
                result.Headers.TryGetValues("X-Message-Id", out var ids);
                _logger.LogError("SendGrid failed. To={To} Status={StatusCode} MsgId={MsgId} Body={Body}",to, (int)result.StatusCode, ids?.FirstOrDefault(), body);
                throw new Exception($"Email send failed ({(int)result.StatusCode}).");
            }
            _logger.LogInformation("Email sent successfully to {To} with subject {Subject}", to, subject);
            return new SuccessResult("Email sent successfully.");
        }
    }
}
