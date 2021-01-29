using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using Prototype1.Foundation.Logging;

namespace Prototype1.Services.Logging
{
    public class DebugExceptionLogger: IExceptionLogger
    {
            public struct ExceptionInfo
            {
                public string Info;
                public Exception Exception;
            }
            public DebugExceptionLogger()
            {
                this.Exceptions = new List<ExceptionInfo>();
            }

            public List<ExceptionInfo> Exceptions { get; set; }
        
            public void LogException(Exception ex, string info = "", ExceptionContext currentContext = null)
            {
                this.Exceptions.Add(new ExceptionInfo { Exception = ex, Info = info });

                OutputExceptionsToDebugConsole();
            }

            private void OutputExceptionsToDebugConsole()
            {
                foreach (var info in this.Exceptions)
                {
                    Debug.WriteLine(info.Exception.GetType());
                    if (!string.IsNullOrEmpty(info.Info))
                        Debug.WriteLine(info.Info);
                    Debug.WriteLine(info.Exception.Message);
                    Debug.WriteLine("--------------------------------------------------------------------");
                    if (info.Exception.InnerException != null)
                    {
                        Debug.WriteLine(info.Exception.InnerException.Message);
                    }
                }
            }
        } 
}