namespace Prototype1.Foundation.Interfaces
{
    public interface IMailService
    {
        bool SendEmail(string to, string from, string subject, string cc = "", string bcc = "", string htmlBody = "",
            string textOnlyBody = "", string[] attachmentPaths = null);

        void SendEmailAsync(string to, string from, string subject, string cc = "", string bcc = "",
            string htmlBody = "", string textOnlyBody = "", string[] attachmentPaths = null);
    }
}
