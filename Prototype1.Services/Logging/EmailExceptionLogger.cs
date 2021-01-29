using System;
using System.Configuration;
using System.IO;
using System.Web;
using Prototype1.Foundation;
using Prototype1.Foundation.Interfaces;
using Prototype1.Foundation.Logging;
using Environment = System.Environment;

namespace Prototype1.Services.Logging
{
    public class EmailExceptionLogger : IExceptionLogger
    {
        private readonly IMailService _mailService;

        private static readonly string ExceptionEmailTo = ConfigurationManager.AppSettings["ExceptionEmail"] ??
                                                        "noreply@prototype1.io";

        private static readonly string ExceptionEmailFrom = ConfigurationManager.AppSettings["ExceptionEmailFrom"] ??
                                                        "noreply@prototype1.io";

        public struct ExceptionInfo
        {
            public string Info;
            public Exception Exception;
        }

        public EmailExceptionLogger(IMailService mailService)
        {
            _mailService = mailService;
        }

        public void LogException(Exception ex, string info = "", ExceptionContext currentContext = null)
        {
            try
            {
                currentContext = currentContext ?? new ExceptionContext(HttpContext.Current);
                SendExceptionEmail(ex, info, currentContext);
            }
            catch(Exception ex2)
            {
                try
                {
                    _mailService.SendEmailAsync(ExceptionEmailTo, ExceptionEmailFrom, "Website Exception",
                        textOnlyBody: "Exception sending exception: " + ex2.GetFullMessage());
                }
                catch (Exception)
                {
                }
            }
        }

        private void SendExceptionEmail(Exception exception, string info, ExceptionContext currentContext)
        {
            using (var sw = new StringWriter())
            {
                sw.WriteLine("<pre>");
                sw.WriteLine("--------------------------------------------------------------------");
                sw.WriteLine("Server: " + Environment.MachineName);
                sw.WriteLine("--------------------------------------------------------------------");
                sw.WriteLine("Type: " + exception.GetType());
                sw.WriteLine("--------------------------------------------------------------------");
                if (!string.IsNullOrEmpty(info))
                {
                    sw.WriteLine("Info: " + info);
                    sw.WriteLine("--------------------------------------------------------------------");
                }
                sw.WriteLine("Message: ");
                sw.WriteLine(exception.GetFullMessage());
                sw.WriteLine("--------------------------------------------------------------------");
                sw.WriteLine("Stack:");
                sw.WriteLine(exception.StackTrace);
                sw.WriteLine("--------------------------------------------------------------------");
                try
                {
                    sw.WriteLine("URL: " + currentContext.HttpMethod + " " +
                                 currentContext.Url);
                    foreach (var k in currentContext.QueryString.Keys)
                        sw.WriteLine("   " + k + ": " + currentContext.QueryString[k]);
                    sw.WriteLine("IP: " + currentContext.UserHostAddress);
                    if (!currentContext.UserID.IsNullOrEmpty())
                    {
                        sw.WriteLine("User: " + currentContext.UserID);
                    }
                    if (!currentContext.RequestBody.IsNullOrEmpty())
                    {
                        sw.WriteLine("Request Body:");
                        sw.WriteLine(currentContext.RequestBody);
                    }
                }
                catch (Exception ex)
                {
                    sw.WriteLine(ex.GetFullMessage());
                }
                sw.WriteLine("--------------------------------------------------------------------");
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine("</pre>");

                _mailService.SendEmailAsync(ExceptionEmailTo, ExceptionEmailFrom, "Website Exception",
                    htmlBody: sw.ToString());
            }
        }

    }
}