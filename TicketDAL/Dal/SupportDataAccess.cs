using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using TicketModel.Models;

namespace TicketDAL.Dal
{
    public class SupportDataAccess
    {
        private Database db;
        public SupportDataAccess() { db = DatabaseFactory.CreateDatabase(); }

        public List<TicketModel.Models.TicketModel> GetAssignedTickets(int userId, string search, int? statusId, int? priorityId)
        {
            try
            {
                var list = new List<TicketModel.Models.TicketModel>();
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetAssignedToUser");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@SearchKeyword", DbType.String, (object)search ?? DBNull.Value);
                db.AddInParameter(cmd, "@StatusId", DbType.Int32, (object)statusId ?? DBNull.Value);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, (object)priorityId ?? DBNull.Value);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new TicketModel.Models.TicketModel
                        {
                            TicketId = Convert.ToInt32(dr["TicketId"]),
                            Title = dr["Title"].ToString(),
                            Description = dr["Description"].ToString(),
                            PriorityId = dr["PriorityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["PriorityId"]),
                            PriorityName = dr["PriorityName"].ToString(),
                            StatusId = Convert.ToInt32(dr["StatusId"]),
                            StatusName = dr["StatusName"].ToString(),
                            RaisedbyUserId = Convert.ToInt32(dr["RaisedbyUserId"]),
                            RaisedByName = dr["RaisedByName"].ToString(),
                            AssignedtoUserId = dr["AssignedtoUserId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["AssignedtoUserId"]),
                            AssignedToName = dr["AssignedToName"] == DBNull.Value ? null : dr["AssignedToName"].ToString(),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                            TicketClosedDate = dr["TicketClosedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["TicketClosedDate"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "GetAssignedTickets");
                throw;
            }
        }

        public TicketModel.Models.TicketModel GetTicketByIdForSupport(int ticketId, int userId)
        {
            try
            {
                TicketModel.Models.TicketModel ticket = null;
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetByIdForSupport");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        ticket = new TicketModel.Models.TicketModel
                        {
                            TicketId = Convert.ToInt32(dr["TicketId"]),
                            Title = dr["Title"].ToString(),
                            Description = dr["Description"].ToString(),
                            PriorityId = dr["PriorityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["PriorityId"]),
                            PriorityName = dr["PriorityName"].ToString(),
                            StatusId = Convert.ToInt32(dr["StatusId"]),
                            StatusName = dr["StatusName"].ToString(),
                            RaisedbyUserId = Convert.ToInt32(dr["RaisedbyUserId"]),
                            RaisedByName = dr["RaisedByName"].ToString(),
                            AssignedtoUserId = Convert.ToInt32(dr["AssignedtoUserId"]),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                            TicketClosedDate = dr["TicketClosedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["TicketClosedDate"])
                        };
                    }
                }
                return ticket;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "GetTicketByIdForSupport");
                throw;
            }
        }

        public bool UpdateStatusPriority(int ticketId, int userId, int statusId, int priorityId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_UpdateStatusPriority");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@StatusId", DbType.Int32, statusId);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, priorityId);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "UpdateStatusPriority");
                throw;
            }
        }

        public bool UpdateStatusOnly(int ticketId, int userId, int statusId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_UpdateStatusOnly");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@StatusId", DbType.Int32, statusId);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "UpdateStatusOnly");
                throw;
            }
        }

        public List<TicketModel.Models.TicketModel> GetNeedsPriority(int userId)
        {
            try
            {
                var list = new List<TicketModel.Models.TicketModel>();
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetNeedsPriority");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new TicketModel.Models.TicketModel
                        {
                            TicketId = Convert.ToInt32(dr["TicketId"]),
                            Title = dr["Title"].ToString(),
                            Description = dr["Description"].ToString(),
                            RaisedByName = dr["RaisedByName"].ToString(),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "GetNeedsPriority");
                throw;
            }
        }

        public bool SetPriority(int ticketId, int userId, int priorityId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_SetPriority");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, priorityId);
                return Convert.ToInt32(db.ExecuteScalar(cmd)) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "SetPriority");
                throw;
            }
        }

        public List<TicketModel.Models.TicketModel> GetCompletedArchive(int userId)
        {
            try
            {
                var list = new List<TicketModel.Models.TicketModel>();
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetCompletedArchive");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new TicketModel.Models.TicketModel
                        {
                            TicketId = Convert.ToInt32(dr["TicketId"]),
                            Title = dr["Title"].ToString(),
                            Description = dr["Description"].ToString(),
                            PriorityId = dr["PriorityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["PriorityId"]),
                            PriorityName = dr["PriorityName"].ToString(),
                            StatusId = Convert.ToInt32(dr["StatusId"]),
                            StatusName = dr["StatusName"].ToString(),
                            RaisedByName = dr["RaisedByName"].ToString(),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                            TicketClosedDate = dr["TicketClosedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["TicketClosedDate"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "GetCompletedArchive");
                throw;
            }
        }

        public bool MoveBackToProgress(int ticketId, int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_MoveBackToProgress");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                return Convert.ToInt32(db.ExecuteScalar(cmd)) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "MoveBackToProgress");
                throw;
            }
        }

        public bool UpdatePriority(int ticketId, int userId, int priorityId)
        {
            try
            {
                var cmd = db.GetStoredProcCommand("Ticket_UpdatePriority");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, priorityId);
                var result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "SupportDataAccess", "UpdatePriority");
                throw;
            }
        }
    }
}
