using System;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace TicketDAL.Dal
{
    public class OtpRecord
    {
        public int OtpId { get; set; }
        public int UserId { get; set; }
        public string Purpose { get; set; }
        public string OtpHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime? ResendAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class OtpDataAccess
    {
        private Database db;
        public OtpDataAccess() { db = DatabaseFactory.CreateDatabase(); }

        public int Insert(int userId, string purpose, string otpHash, DateTime expiresAt, int maxAttempts, DateTime? resendAt = null)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("UserOtp_Insert");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@Purpose", DbType.String, purpose);
                db.AddInParameter(cmd, "@OtpHash", DbType.String, otpHash);
                db.AddInParameter(cmd, "@ExpiresAt", DbType.DateTime, expiresAt);
                db.AddInParameter(cmd, "@MaxAttempts", DbType.Int32, maxAttempts);
                db.AddInParameter(cmd, "@ResendAt", DbType.DateTime, (object)resendAt ?? DBNull.Value);
                return Convert.ToInt32(db.ExecuteScalar(cmd));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "OtpDataAccess", "Insert");
                throw;
            }
        }

        public OtpRecord GetActive(int userId, string purpose)
        {
            try
            {
                OtpRecord record = null;
                DbCommand cmd = db.GetStoredProcCommand("UserOtp_GetActive");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@Purpose", DbType.String, purpose);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        record = new OtpRecord
                        {
                            OtpId = Convert.ToInt32(dr["OtpId"]),
                            UserId = Convert.ToInt32(dr["UserId"]),
                            Purpose = dr["Purpose"].ToString(),
                            OtpHash = dr["OtpHash"].ToString(),
                            ExpiresAt = Convert.ToDateTime(dr["ExpiresAt"]),
                            Attempts = Convert.ToInt32(dr["Attempts"]),
                            MaxAttempts = Convert.ToInt32(dr["MaxAttempts"]),
                            ResendAt = dr["ResendAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["ResendAt"]),
                            IsUsed = Convert.ToBoolean(dr["IsUsed"]),
                            UsedAt = dr["UsedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["UsedAt"]),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"])
                        };
                    }
                }
                return record;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "OtpDataAccess", "GetActive");
                throw;
            }
        }

        public void MarkUsed(int otpId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("UserOtp_MarkUsed");
                db.AddInParameter(cmd, "@OtpId", DbType.Int32, otpId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "OtpDataAccess", "MarkUsed");
                throw;
            }
        }

        public void IncrementAttempts(int otpId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("UserOtp_IncrementAttempts");
                db.AddInParameter(cmd, "@OtpId", DbType.Int32, otpId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "OtpDataAccess", "IncrementAttempts");
                throw;
            }
        }

        public void InvalidateOld(int userId, string purpose)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("UserOtp_InvalidateOld");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@Purpose", DbType.String, purpose);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "OtpDataAccess", "InvalidateOld");
                throw;
            }
        }
    }
}
