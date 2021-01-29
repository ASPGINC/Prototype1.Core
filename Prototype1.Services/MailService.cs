using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using Prototype1.Foundation;

namespace Prototype1.Services
{
    public class MailService : Foundation.Interfaces.IMailService
    {
        public bool SendEmail(string to, string from, string subject, string cc = "", string bcc = "",
            string htmlBody = "", string textOnlyBody = "", string[] attachmentPaths = null)
        {
            if (from.IsNullOrEmpty() || (to.IsNullOrEmpty() && cc.IsNullOrEmpty() && bcc.IsNullOrEmpty()))
                return false;

            return SendEmailTask(to, from, subject, cc, bcc, htmlBody, textOnlyBody, attachmentPaths);
        }
        
        public void SendEmailAsync(string to, string from, string subject, string cc = "", string bcc = "",
            string htmlBody = "", string textOnlyBody = "", string[] attachmentPaths = null)
        {
            if (from.IsNullOrEmpty() || (to.IsNullOrEmpty() && cc.IsNullOrEmpty() && bcc.IsNullOrEmpty()))
                return;

            new TaskFactory().StartNew(() =>
                SendEmailTask(to, from, subject, cc, bcc, htmlBody, textOnlyBody, attachmentPaths));
        }

        private static MailMessage CreateMessage(string to, string from, string subject, string cc, string bcc, string htmlBody, string textOnlyBody, IEnumerable<string> attachmentPaths)
        {
            var msg = new MailMessage { From = new MailAddress(from), Subject = subject };

            msg.ReplyToList.Add(new MailAddress(from));

            foreach (
                var t in
                    to.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(t => !t.ToLower().Contains("noreply")))
                try { msg.To.Add(new MailAddress(t)); }
                catch { }

            if (!string.IsNullOrEmpty(cc))
                foreach (
                    var c in
                        cc.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(c => !c.ToLower().Contains("noreply")))
                    try { msg.CC.Add(new MailAddress(c)); }
                    catch { }

            if (!string.IsNullOrEmpty(bcc))
                foreach (
                    var b in
                        bcc.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(b => !b.ToLower().Contains("noreply")))
                    try { msg.Bcc.Add(new MailAddress(b)); }
                    catch { }

            if (attachmentPaths != null)
                foreach (var s in attachmentPaths)
                    msg.Attachments.Add(new Attachment(s));

            if (!textOnlyBody.IsNullOrEmpty())
                msg.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(textOnlyBody, null, MediaTypeNames.Text.Plain));

            if (!htmlBody.IsNullOrEmpty())
            {
                msg.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html));
                msg.IsBodyHtml = true;
            }
            else
                msg.IsBodyHtml = false;

            return msg;
        }

        private static bool SendEmailTask(string to, string from, string subject, string cc, string bcc, string htmlBody, string textOnlyBody, string[] attachmentPaths)
        {
            using(var mailClient = new SmtpClient())
            using (var msg = CreateMessage(to, from, subject, cc, bcc, htmlBody, textOnlyBody, attachmentPaths))
            {
                if (msg.To.Count == 0 && msg.CC.Count == 0 && msg.Bcc.Count == 0)
                    return false;

                try
                {
                    mailClient.Send(msg);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}
