using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace TicketDAL.Dal
{
    /// <summary>
    /// Low-level SMTP email transport used by the whole application.
    /// Settings are read from web.config appSettings with sensible fallbacks.
    /// Errors are logged and swallowed so email failure never breaks business logic.
    /// </summary>
    public static class EmailHelper
    {
        public static void Send(string toEmail, string subject, string htmlBody)
        {
            try
            {
                string smtpHost = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
                int smtpPort = ToInt(ConfigurationManager.AppSettings["SmtpPort"], 587);
                bool enableSsl = ToBool(ConfigurationManager.AppSettings["SmtpEnableSsl"], true);
                string fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"] ?? "siddhilipare2@gmail.com";
                string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "Simplify IT Support";
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "tmgp muab lwmo teja";

                var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.IsBodyHtml = true;
                message.Body = htmlBody;

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.EnableSsl = enableSsl;
                    smtp.Credentials = new NetworkCredential(fromEmail, smtpPassword);
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "EmailHelper", "Send");
            }
        }

        private static int ToInt(string value, int fallback)
        {
            int parsed;
            return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static bool ToBool(string value, bool fallback)
        {
            bool parsed;
            return !string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out parsed) ? parsed : fallback;
        }
    }
}
