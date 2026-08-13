using System;
using System.IO;
using System.Web;

namespace Ticket_Management_System.Helpers
{
    public static class Logger
    {
        private static string GetLogFilePath()
        {
            string fileName = "error_log_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string baseDir = HttpContext.Current != null
                ? HttpContext.Current.Server.MapPath("~/App_Data/")
                : AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, fileName);
        }

        public static void LogToFile(Exception ex,
            string controllerName = null,
            string actionName = null,
            string extraInfo = null)
        {
            try
            {
                string separator = new string('-', 80);
                string controller = string.IsNullOrEmpty(controllerName) ? "" : " | Controller: " + controllerName;
                string action = string.IsNullOrEmpty(actionName) ? "" : " | Action: " + actionName;
                string extra = string.IsNullOrEmpty(extraInfo) ? "" : "\r\nInfo: " + extraInfo;

                string entry = string.Format(
                    "[{0:yyyy-MM-dd HH:mm:ss}]{1}{2}\r\nType   : {3}\r\nMessage: {4}{5}\r\nStack  :\r\n{6}\r\n{7}\r\n",
                    DateTime.Now,
                    controller,
                    action,
                    ex.GetType().Name,
                    ex.Message,
                    extra,
                    ex.StackTrace,
                    separator);

                string path = GetLogFilePath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(path, entry);
            }
            catch
            {
                
            }
        }
    }
}