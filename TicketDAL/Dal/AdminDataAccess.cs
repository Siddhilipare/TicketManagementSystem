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
    public class AdminDataAccess
    {
        private Database db;
        public AdminDataAccess() { db = DatabaseFactory.CreateDatabase(); }

        public List<UserListModel> GetAllUsers()
        {
            try
            {
                var list = new List<UserListModel>();
                DbCommand cmd = db.GetStoredProcCommand("User_GetAll");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new UserListModel
                        {
                            UserId = Convert.ToInt32(dr["UserId"]),
                            Email = dr["Email"].ToString(),
                            RoleId = Convert.ToInt32(dr["RoleId"]),
                            RoleName = dr["RoleName"].ToString(),
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            UserName = dr["UserName"] == DBNull.Value ? null : dr["UserName"].ToString(),
                            City = dr["City"] == DBNull.Value ? null : dr["City"].ToString()
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "GetAllUsers");
                throw;
            }
        }

        public bool ToggleUserActive(int userId, bool isActive, int modifiedBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("User_ToggleActive");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@IsActive", DbType.Boolean, isActive);
                db.AddInParameter(cmd, "@ModifiedBy", DbType.Int32, modifiedBy);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "ToggleUserActive");
                throw;
            }
        }

        public bool UpdateUser(int userId, string userName, int roleId, int modifiedBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("User_UpdateDetails");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@UserName", DbType.String, userName);
                db.AddInParameter(cmd, "@RoleId", DbType.Int32, roleId);
                db.AddInParameter(cmd, "@ModifiedBy", DbType.Int32, modifiedBy);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "UpdateUser");
                throw;
            }
        }

        public List<TicketModel.Models.TicketModel> GetAllTickets(string search, int? statusId, int? priorityId)
        {
            try
            {
                var list = new List<TicketModel.Models.TicketModel>();
                DbCommand cmd = db.GetStoredProcCommand("Ticket_GetAll");
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
                            PriorityName = dr["PriorityName"] == DBNull.Value ? "Unassigned" : dr["PriorityName"].ToString(),
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
                Logger.LogToFile(ex, "AdminDataAccess", "GetAllTickets");
                throw;
            }
        }

        public List<UserListModel> GetSupportExecutives()
        {
            try
            {
                var list = new List<UserListModel>();
                DbCommand cmd = db.GetStoredProcCommand("User_GetSupportExecutives");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new UserListModel
                        {
                            UserId = Convert.ToInt32(dr["UserId"]),
                            UserName = dr["UserName"].ToString()
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "GetSupportExecutives");
                throw;
            }
        }

        public bool AssignTicket(int ticketId, int assignedToUserId, int modifiedBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_AssignToUser");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@AssignedToUserId", DbType.Int32, assignedToUserId);
                db.AddInParameter(cmd, "@ModifiedBy", DbType.Int32, modifiedBy);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "AssignTicket");
                throw;
            }
        }

        public TicketModel.Models.TicketModel GetTicketByIdForAdmin(int ticketId)
        {
            try
            {
                return GetAllTickets(null, null, null).Find(t => t.TicketId == ticketId);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "GetTicketByIdForAdmin");
                throw;
            }
        }

        public bool UpdateTicketAsAdmin(int ticketId, string title, string description, int priorityId, int statusId, int modifiedBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_AdminUpdate");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@Title", DbType.String, title);
                db.AddInParameter(cmd, "@Description", DbType.String, description);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, priorityId);
                db.AddInParameter(cmd, "@StatusId", DbType.Int32, statusId);
                db.AddInParameter(cmd, "@ModifiedBy", DbType.Int32, modifiedBy);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "UpdateTicketAsAdmin");
                throw;
            }
        }

        public bool DeleteTicketAsAdmin(int ticketId, int modifiedBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_AdminDelete");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@ModifiedBy", DbType.Int32, modifiedBy);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "DeleteTicketAsAdmin");
                throw;
            }
        }

        public List<UserListModel> GetStaffUsers()
        {
            try
            {
                var list = new List<UserListModel>();
                DbCommand cmd = db.GetStoredProcCommand("User_GetStaff");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new UserListModel
                        {
                            UserId = Convert.ToInt32(dr["UserId"]),
                            Email = dr["Email"].ToString(),
                            RoleId = Convert.ToInt32(dr["RoleId"]),
                            RoleName = dr["RoleName"].ToString(),
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            UserName = dr["UserName"] == DBNull.Value ? null : dr["UserName"].ToString()
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "GetStaffUsers");
                throw;
            }
        }

        public List<UserListModel> GetEmployees()
        {
            try
            {
                var list = new List<UserListModel>();
                DbCommand cmd = db.GetStoredProcCommand("User_GetEmployees");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new UserListModel
                        {
                            UserId = Convert.ToInt32(dr["UserId"]),
                            Email = dr["Email"].ToString(),
                            RoleId = Convert.ToInt32(dr["RoleId"]),
                            RoleName = dr["RoleName"].ToString(),
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            UserName = dr["UserName"] == DBNull.Value ? null : dr["UserName"].ToString()
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "GetEmployees");
                throw;
            }
        }

        public bool AssignAndPrioritize(int ticketId, int priorityId, int assignedToUserId, int modifiedBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_AdminAssignAndPrioritize");
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, ticketId);
                db.AddInParameter(cmd, "@PriorityId", DbType.Int32, priorityId);
                db.AddInParameter(cmd, "@AssignedToUserId", DbType.Int32, assignedToUserId);
                db.AddInParameter(cmd, "@ModifiedBy", DbType.Int32, modifiedBy);
                object result = db.ExecuteScalar(cmd);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "AssignAndPrioritize");
                throw;
            }
        }

        public int CreateTicket(string title, string description, int raisedByUserId, int createdBy)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Ticket_AdminCreate");
                db.AddInParameter(cmd, "@Title", DbType.String, title);
                db.AddInParameter(cmd, "@Description", DbType.String, description);
                db.AddInParameter(cmd, "@RaisedbyUserId", DbType.Int32, raisedByUserId);
                db.AddInParameter(cmd, "@CreatedBy", DbType.Int32, createdBy);
                return Convert.ToInt32(db.ExecuteScalar(cmd));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AdminDataAccess", "CreateTicket");
                throw;
            }
        }
    }
}
