﻿using BloodCenter.Data.Dtos;
using BloodCenter.Service.Cores.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace BloodCenter.Service.Cores
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private ModelResult _result;
        public EmailService(IConfiguration config)
        {
            _config = config;
            _result = new ModelResult();
        }
        public async Task<ModelResult> SendMailActiveAccount(string email, string hashEmail)
        {
            try
            {
                var mail = new MimeMessage();
                mail.From.Add(MailboxAddress.Parse(_config["EmailConfig:Email"]));
                mail.To.Add(MailboxAddress.Parse(email));
                mail.Subject = "Click link to activate account";
                var activationLink = $"https://localhost:8081/api/Auth/verify/{hashEmail}";
                mail.Body = new TextPart(TextFormat.Html)
                {
                    Text = $"Please click the link to activate your account: <a href='{activationLink}'>Activate Account</a>"
                };
                using var smtp = new SmtpClient();
                smtp.Connect(_config["EmailConfig:smtp"], int.Parse(_config["EmailConfig:SmtpPort"]), SecureSocketOptions.StartTls);
                smtp.Authenticate(_config["EmailConfig:Email"], _config["EmailConfig:EmailPassword"]);
                smtp.Send(mail);
                smtp.Disconnect(true);
                _result.Success = true;
                _result.Message = "Email sent successfully";
                return _result;
            }
            catch (Exception ex)
            {
                _result.Success = false;
                _result.Message = ex.Message;
                return _result;
            }
        }
        public static async Task<ModelResult> SendMailResetPassword(string email, string password, IConfiguration _config)
        {
            var _result = new ModelResult();
            try
            {
                var mail = new MimeMessage();
                mail.From.Add(MailboxAddress.Parse(_config["EmailConfig:Email"]));
                mail.To.Add(MailboxAddress.Parse(email));
                mail.Subject = "Reset password";
                var pass = $"{password}";
                mail.Body = new TextPart(TextFormat.Html)
                {
                    Text = $"Your password is: {pass}"
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config["EmailConfig:smtp"], int.Parse(_config["EmailConfig:SmtpPort"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailConfig:Email"], _config["EmailConfig:EmailPassword"]);

                await smtp.SendAsync(mail);
                await smtp.DisconnectAsync(true);

                _result.Success = true;
                _result.Message = "Email sent successfully";
            }
            catch (Exception ex)
            {
                _result.Success = false;
                _result.Message = ex.ToString();
            }
            return _result;
        }
    }
}
