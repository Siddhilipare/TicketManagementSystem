using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace TicketDAL.Dal
{
    public class RefreshTokenRecord
    {
        public int TokenId { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string ReplacedByTokenHash { get; set; }
    }

    public class TokenDAL
    {
        private Database db;
        public TokenDAL() { db = DatabaseFactory.CreateDatabase(); }

        public int InsertRefreshToken(int userId, string tokenHash, DateTime expiresAt)
        {
            int newTokenId = 0;
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("InsertRefreshToken");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@TokenHash", DbType.String, tokenHash);
                db.AddInParameter(cmd, "@ExpiresAt", DbType.DateTime, expiresAt);

                object result = db.ExecuteScalar(cmd);
                newTokenId = Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TokenDAL", "InsertRefreshToken");
                throw;
            }
            return newTokenId;
        }

        public RefreshTokenRecord GetByTokenHash(string tokenHash)
        {
            RefreshTokenRecord record = null;
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("GetRefreshTokenByHash");
                db.AddInParameter(cmd, "@TokenHash", DbType.String, tokenHash);

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                    {
                        record = new RefreshTokenRecord
                        {
                            TokenId = Convert.ToInt32(dr["TokenId"]),
                            UserId = Convert.ToInt32(dr["UserId"]),
                            TokenHash = dr["TokenHash"].ToString(),
                            ExpiresAt = Convert.ToDateTime(dr["ExpiresAt"]),
                            CreatedAt = Convert.ToDateTime(dr["CreatedAt"]),
                            RevokedAt = dr["RevokedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["RevokedAt"]),
                            ReplacedByTokenHash = dr["ReplacedByTokenHash"] == DBNull.Value ? null : dr["ReplacedByTokenHash"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TokenDAL", "GetByTokenHash");
                throw;
            }
            return record;
        }

        public void RevokeToken(string tokenHash, string replacedByTokenHash = null)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("RevokeRefreshToken");
                db.AddInParameter(cmd, "@TokenHash", DbType.String, tokenHash);
                db.AddInParameter(cmd, "@ReplacedByTokenHash", DbType.String, (object)replacedByTokenHash ?? DBNull.Value);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TokenDAL", "RevokeToken");
                throw;
            }
        }

        public void RevokeAllForUser(int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("RevokeTokenFamilyByUser");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "TokenDAL", "RevokeAllForUser");
                throw;
            }
        }
    }
}
