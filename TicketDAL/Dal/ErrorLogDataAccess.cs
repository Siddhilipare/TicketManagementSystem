using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data;

namespace TicketDAL.Dal
{
   public class ErrorLogDataAccess
    {
        private Database db;
        public ErrorLogDataAccess() { db = DatabaseFactory.CreateDatabase(); }

        public void LogError(string controllerName, string actionName, string exceptionMessage,
                              string stackTrace, string userEmail, string requestUrl)
        {
            try
            {
                var cmd = db.GetStoredProcCommand("ErrorLog_Insert");
                db.AddInParameter(cmd, "@ControllerName", DbType.String, (object)controllerName ?? System.DBNull.Value);
                db.AddInParameter(cmd, "@ActionName", DbType.String, (object)actionName ?? System.DBNull.Value);
                db.AddInParameter(cmd, "@ExceptionMessage", DbType.String, exceptionMessage);
                db.AddInParameter(cmd, "@StackTrace", DbType.String, (object)stackTrace ?? System.DBNull.Value);
                db.AddInParameter(cmd, "@UserEmail", DbType.String, (object)userEmail ?? System.DBNull.Value);
                db.AddInParameter(cmd, "@RequestUrl", DbType.String, (object)requestUrl ?? System.DBNull.Value);
                db.ExecuteNonQuery(cmd);
            }
            catch
            {
                // Never let logging itself crash the app — swallow silently as last resort.
                // Optionally write to a local file here as a fallback.
            }
        }
    }
}
