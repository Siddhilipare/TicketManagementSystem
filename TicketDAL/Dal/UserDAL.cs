using System;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using TicketModel;

namespace TicketDAL.Dal
{
    public class UserDAL
    {
        private Database db;
        public UserDAL() { db = DatabaseFactory.CreateDatabase(); }

        #region Getuserbymail
        public UserModel GetUserByEmail(string email)
        {
            UserModel user = null;
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("User_GetByEmail");
                db.AddInParameter(cmd, "@Email", DbType.String, email);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        user = MapUser(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "UserDAL", "GetUserByEmail");
                throw;
            }
            return user;
        }
        #endregion

        #region GetUserById
        public UserModel GetUserById(int userId)
        {
            UserModel user = null;
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("User_GetById");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        user = MapUser(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "UserDAL", "GetUserById");
                throw;
            }
            return user;
        }
        #endregion

        #region InsertUser
        public int InsertUser(string email, string passwordHash, string passwordSalt, int roleId, bool isVerified = false)
        {
            int newUserId = 0;
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("User_Insert");
                db.AddInParameter(cmd, "@Email", DbType.String, email);
                db.AddInParameter(cmd, "@PasswordHash", DbType.String, passwordHash);
                db.AddInParameter(cmd, "@PasswordSalt", DbType.String, passwordSalt);
                db.AddInParameter(cmd, "@RoleId", DbType.Int32, roleId);
                db.AddInParameter(cmd, "@CreatedBy", DbType.Int32, DBNull.Value);
                db.AddInParameter(cmd, "@IsVerified", DbType.Boolean, isVerified);

                object result = db.ExecuteScalar(cmd);
                newUserId = Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "UserDAL", "InsertUser");
                throw;
            }
            return newUserId;
        }
        #endregion

        #region MarkVerified
        public void MarkVerified(int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("User_MarkVerified");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "UserDAL", "MarkVerified");
                throw;
            }
        }
        #endregion

        #region InsertUserDetail
        public void InsertUserDetail(int userId, string userName, string address, int? age, string gender, string city)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("UserDetail_Insert");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@UserName", DbType.String, userName);
                db.AddInParameter(cmd, "@Address", DbType.String, (object)address ?? DBNull.Value);
                db.AddInParameter(cmd, "@Age", DbType.Int32, (object)age ?? DBNull.Value);
                db.AddInParameter(cmd, "@Gender", DbType.String, (object)gender ?? DBNull.Value);
                db.AddInParameter(cmd, "@City", DbType.String, (object)city ?? DBNull.Value);
                db.AddInParameter(cmd, "@CreatedBy", DbType.Int32, userId);

                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "UserDAL", "InsertUserDetail");
                throw;
            }
        }
        #endregion

        private UserModel MapUser(IDataReader dr)
        {
            return new UserModel
            {
                UserId = Convert.ToInt32(dr["UserId"]),
                Email = dr["Email"].ToString(),
                PasswordHash = dr["PasswordHash"].ToString(),
                PasswordSalt = dr["PasswordSalt"] == DBNull.Value ? null : dr["PasswordSalt"].ToString(),
                RoleId = Convert.ToInt32(dr["RoleId"]),
                RoleName = dr["RoleName"].ToString(),
                IsActive = Convert.ToBoolean(dr["IsActive"]),
                IsVerified = Convert.ToBoolean(dr["IsVerified"])
            };
        }
    }
}
