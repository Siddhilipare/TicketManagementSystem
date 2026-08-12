using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using TicketModel.Models;

namespace TicketDAL.Dal
{
    public class TicketDataAccess
    {
        private Database db;

        public TicketDataAccess()
        {
            db = DatabaseFactory.CreateDatabase();
        }

        public int CreateTicket(string title, string description, int raisedByUserId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_Create");
                db.AddInParameter(cmd, "@Title", DbType.String, title);
                db.AddInParameter(cmd, "@Description", DbType.String, description);
                db.AddInParameter(cmd, "@RaisedbyUserId", DbType.Int32, raisedByUserId);
                return Convert.ToInt32(db.ExecuteScalar(cmd));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "CreateTicket");
                throw;
            }
        }

        public List<TicketModel.Models.TicketModel> GetTicketsByUserId(
            int userId, string search, int? statusId, int? priorityId)
        {
            try
            {
                var list = new List<TicketModel.Models.TicketModel>();
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetByUserId");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@SearchKeyword", DbType.String, (object)search ?? DBNull.Value);
                db.AddInParameter(cmd, "@StatusId", DbType.Int32, (object)statusId ?? DBNull.Value);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, (object)priorityId ?? DBNull.Value);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(MapTicket(dr));
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "GetTicketsByUserId");
                throw;
            }
        }

        public TicketModel.Models.TicketModel GetTicketById(int ticketId, int userId)
        {
            try
            {
                TicketModel.Models.TicketModel ticket = null;
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetById");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read()) ticket = MapTicket(dr);
                }
                return ticket;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "GetTicketById");
                throw;
            }
        }

        public bool UpdateTicket(int ticketId, int userId, string title, string description)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_Update");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@Title", DbType.String, title);
                db.AddInParameter(cmd, "@Description", DbType.String, description);

                object result = db.ExecuteScalar(cmd);
                int rowsAffected = result != null ? Convert.ToInt32(result) : 0;
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "UpdateTicket");
                throw;
            }
        }

        public bool DeleteTicket(int ticketId, int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_Delete");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);

                object result = db.ExecuteScalar(cmd);
                int rowsAffected = result != null ? Convert.ToInt32(result) : 0;
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "DeleteTicket");
                throw;
            }
        }

        public void AddAttachment(int ticketId, string filePath, int uploadedByUserId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("TicketAttachment_Insert");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@FilePath", DbType.String, filePath);
                db.AddInParameter(cmd, "@UploadedByUserId", DbType.Int32, uploadedByUserId);
                db.ExecuteScalar(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "AddAttachment");
                throw;
            }
        }

        public List<TicketAttachmentModel> GetAttachmentsByTicketId(int ticketId)
        {
            try
            {
                var list = new List<TicketAttachmentModel>();
                DbCommand cmd = db.GetStoredProcCommand("TicketAttachment_GetByTicketId");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new TicketAttachmentModel
                        {
                            TicketAttachmentId = Convert.ToInt32(dr["TicketAttachmentId"]),
                            TicketId = Convert.ToInt32(dr["TicketId"]),
                            FilePath = dr["FilePath"].ToString(),
                            UploadedByUserId = Convert.ToInt32(dr["UploadedByUserId"]),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "GetAttachmentsByTicketId");
                throw;
            }
        }

        public void AddComment(int ticketId, int commentedByUserId, string commentText)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("TicketComment_Insert");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@CommentedbyUserId", DbType.Int32, commentedByUserId);
                db.AddInParameter(cmd, "@CommentText", DbType.String, commentText);
                db.ExecuteScalar(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "AddComment");
                throw;
            }
        }

        public List<TicketCommentModel> GetCommentsByTicketId(int ticketId)
        {
            try
            {
                var list = new List<TicketCommentModel>();
                DbCommand cmd = db.GetStoredProcCommand("TicketComment_GetByTicketId");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new TicketCommentModel
                        {
                            TicketCommentId = Convert.ToInt32(dr["TicketCommentId"]),
                            TicketId = Convert.ToInt32(dr["TicketId"]),
                            CommentedbyUserId = Convert.ToInt32(dr["CommentedbyUserId"]),
                            CommentedByName = dr["CommentedByName"].ToString(),
                            CommentText = dr["CommentText"].ToString(),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "GetCommentsByTicketId");
                throw;
            }
        }

        private TicketModel.Models.TicketModel MapTicket(IDataReader dr)
        {
            return new TicketModel.Models.TicketModel
            {
                TicketId = Convert.ToInt32(dr["TicketId"]),
                Title = dr["Title"].ToString(),
                Description = dr["Description"].ToString(),
                PriorityId = dr["PriorityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["PriorityId"]),
                PriorityName = dr["PriorityName"].ToString(),
                StatusId = Convert.ToInt32(dr["StatusId"]),
                StatusName = dr["StatusName"].ToString(),
                RaisedbyUserId = Convert.ToInt32(dr["RaisedbyUserId"]),
                AssignedtoUserId = dr["AssignedtoUserId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["AssignedtoUserId"]),
                CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                TicketClosedDate = dr["TicketClosedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["TicketClosedDate"])
            };
        }

        public bool DeleteAttachment(int attachmentId, int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("TicketAttachment_Delete");
                db.AddInParameter(cmd, "@TicketAttachmentId", DbType.Int32, attachmentId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "DeleteAttachment");
                throw;
            }
        }

        public void GetStakeholders(int ticketId, out int raisedByUserId, out int? assignedToUserId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetStakeholders");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                raisedByUserId = 0;
                assignedToUserId = null;
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        raisedByUserId = Convert.ToInt32(dr["RaisedbyUserId"]);
                        assignedToUserId = dr["AssignedtoUserId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["AssignedtoUserId"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TicketDataAccess", "GetStakeholders");
                throw;
            }
        }
    }
}
