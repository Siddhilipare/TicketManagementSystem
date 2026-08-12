using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Data;
using TicketModel.Models;

namespace TicketDAL.Dal
{
    public class ChatBotDAL
    {
        private Database db;

        public ChatBotDAL()
        {
            db = DatabaseFactory.CreateDatabase();
        }

        public List<FAQ> GetAllFAQs()
        {
            try
            {
                var list = new List<FAQ>();
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT FAQId, Question, Answer, Category, SubCategory, Keywords, " +
                    "ViewCount, HelpfulCount, UnhelpfulCount " +
                    "FROM FAQs WHERE IsActive = 1 ORDER BY Category, Question");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                        list.Add(MapFAQ(dr));
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "GetAllFAQs");
                throw;
            }
        }

        public List<FAQ> SearchFAQs(string searchText, int maxResults = 5)
        {
            var list = new List<FAQ>();
            if (string.IsNullOrWhiteSpace(searchText))
                return list;

            try
            {
                var keywords = ExtractKeywords(searchText);

                if (keywords.Count == 0)
                    return SearchFAQsOriginal(searchText, maxResults);

                var sqlBuilder = new StringBuilder(
                    "SELECT TOP " + maxResults + " FAQId, Question, Answer, Category, " +
                    "SubCategory, Keywords, ViewCount, HelpfulCount, UnhelpfulCount " +
                    "FROM FAQs WHERE IsActive = 1 AND (");

                for (int i = 0; i < keywords.Count; i++)
                {
                    if (i > 0) sqlBuilder.Append(" OR ");
                    sqlBuilder.Append("(Question LIKE @k" + i + " OR Answer LIKE @k" + i + " OR Keywords LIKE @k" + i + ")");
                }

                // FIX: The original ORDER BY ViewCount DESC caused wrong answers because all FAQs
                // start at ViewCount = 0, so SQL returned matches in arbitrary order.
                // New ORDER BY ranks results in three tiers:
                //   Tier 0 — the Question column contains the search term  → best match
                //   Tier 1 — the Keywords column contains the search term  → good match
                //   Tier 2 — only the Answer column matched               → weakest match
                // Within each tier, higher ViewCount (more-viewed FAQs) sorts first.
                // This ensures e.g. "How is my complaint prioritized?" returns the Priority FAQ,
                // not whatever SQL happened to return first when all ViewCounts were zero.
                sqlBuilder.Append(") ORDER BY " +
                    "CASE " +
                    "WHEN Question LIKE @k0 THEN 0 " +
                    "WHEN Keywords LIKE @k0 THEN 1 " +
                    "ELSE 2 END ASC, " +
                    "ViewCount DESC");

                DbCommand cmd = db.GetSqlStringCommand(sqlBuilder.ToString());
                for (int i = 0; i < keywords.Count; i++)
                    db.AddInParameter(cmd, "@k" + i, DbType.String, "%" + keywords[i] + "%");

                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                        list.Add(MapFAQ(dr));
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "SearchFAQs");
                return list;
            }
        }

        private List<FAQ> SearchFAQsOriginal(string searchText, int maxResults)
        {
            try
            {
                var list = new List<FAQ>();
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT TOP " + maxResults + " FAQId, Question, Answer, Category, SubCategory, Keywords, " +
                    "ViewCount, HelpfulCount, UnhelpfulCount " +
                    "FROM FAQs WHERE IsActive = 1 " +
                    "AND (Question LIKE @search OR Answer LIKE @search OR Keywords LIKE @search) " +
                    "ORDER BY CASE WHEN Question LIKE @search THEN 0 " +
                    "WHEN Keywords LIKE @search THEN 1 ELSE 2 END, ViewCount DESC");
                db.AddInParameter(cmd, "@search", DbType.String, "%" + searchText.Trim() + "%");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                        list.Add(MapFAQ(dr));
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "SearchFAQsOriginal");
                return new List<FAQ>();
            }
        }

        private List<string> ExtractKeywords(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return new List<string>();

                var stopWords = new[] { "the", "a", "an", "and", "or", "but", "to", "is", "are", "was", "be", "by", "for", "of", "in", "on", "at", "as", "if", "it", "do", "my", "me", "i", "how", "what", "where", "when", "why", "can", "will", "should", "would", "could", "i'm", "don't", "doesn't", "didn't" };

                return searchText
                    .ToLower()
                    .Split(new[] { ' ', ',', '?', '!', '.', ';', ':', '\'' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(k => k.Length > 2 && !stopWords.Contains(k) && !k.All(c => c == '*' || c == '%'))
                    .Distinct()
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "ExtractKeywords");
                return new List<string>();
            }
        }

        public List<FAQ> GetFAQsByCategory(string category)
        {
            try
            {
                var list = new List<FAQ>();
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT FAQId, Question, Answer, Category, SubCategory, Keywords, " +
                    "ViewCount, HelpfulCount, UnhelpfulCount " +
                    "FROM FAQs WHERE IsActive = 1 AND Category = @category ORDER BY Question");
                db.AddInParameter(cmd, "@category", DbType.String, category);
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                        list.Add(MapFAQ(dr));
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "GetFAQsByCategory");
                throw;
            }
        }

        public List<string> GetAllCategories()
        {
            try
            {
                var list = new List<string>();
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT DISTINCT Category FROM FAQs WHERE IsActive = 1 ORDER BY Category");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                        list.Add(dr["Category"].ToString());
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "GetAllCategories");
                throw;
            }
        }

        public FAQ GetFAQById(int faqId)
        {
            try
            {
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT FAQId, Question, Answer, Category, SubCategory, Keywords, " +
                    "ViewCount, HelpfulCount, UnhelpfulCount " +
                    "FROM FAQs WHERE FAQId = @faqId");
                db.AddInParameter(cmd, "@faqId", DbType.Int32, faqId);
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    if (dr.Read())
                        return MapFAQ(dr);
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "GetFAQById");
                throw;
            }
        }

        public int CreateFAQ(FAQ faq)
        {
            try
            {
                DbCommand cmd = db.GetSqlStringCommand(
                    "INSERT INTO FAQs (Question, Answer, Category, SubCategory, Keywords, CreatedBy, CreatedDate, IsActive) " +
                    "VALUES (@question, @answer, @category, @subCategory, @keywords, @createdBy, GETDATE(), 1); " +
                    "SELECT SCOPE_IDENTITY();");
                db.AddInParameter(cmd, "@question", DbType.String, faq.Question);
                db.AddInParameter(cmd, "@answer", DbType.String, faq.Answer);
                db.AddInParameter(cmd, "@category", DbType.String, faq.Category);
                db.AddInParameter(cmd, "@subCategory", DbType.String, faq.SubCategory ?? "");
                db.AddInParameter(cmd, "@keywords", DbType.String, faq.Keywords ?? "");
                db.AddInParameter(cmd, "@createdBy", DbType.Int32, faq.CreatedBy);
                return Convert.ToInt32(db.ExecuteScalar(cmd));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "CreateFAQ");
                throw;
            }
        }

        public bool UpdateFAQ(FAQ faq)
        {
            try
            {
                DbCommand cmd = db.GetSqlStringCommand(
                    "UPDATE FAQs SET Question = @question, Answer = @answer, " +
                    "Category = @category, SubCategory = @subCategory, " +
                    "Keywords = @keywords, IsActive = @isActive, " +
                    "ModifiedDate = GETDATE() WHERE FAQId = @faqId");
                db.AddInParameter(cmd, "@faqId", DbType.Int32, faq.FAQId);
                db.AddInParameter(cmd, "@question", DbType.String, faq.Question);
                db.AddInParameter(cmd, "@answer", DbType.String, faq.Answer);
                db.AddInParameter(cmd, "@category", DbType.String, faq.Category);
                db.AddInParameter(cmd, "@subCategory", DbType.String, faq.SubCategory ?? "");
                db.AddInParameter(cmd, "@keywords", DbType.String, faq.Keywords ?? "");
                db.AddInParameter(cmd, "@isActive", DbType.Boolean, faq.IsActive);
                return db.ExecuteNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "UpdateFAQ");
                throw;
            }
        }

        public bool DeleteFAQ(int faqId)
        {
            try
            {
                DbCommand cmd = db.GetSqlStringCommand(
                    "UPDATE FAQs SET IsActive = 0, ModifiedDate = GETDATE() WHERE FAQId = @faqId");
                db.AddInParameter(cmd, "@faqId", DbType.Int32, faqId);
                return db.ExecuteNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "DeleteFAQ");
                throw;
            }
        }

        public void IncrementViewCount(int faqId)
        {
            try
            {
                DbCommand cmd = db.GetSqlStringCommand(
                    "UPDATE FAQs SET ViewCount = ViewCount + 1 WHERE FAQId = @faqId");
                db.AddInParameter(cmd, "@faqId", DbType.Int32, faqId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "IncrementViewCount");
            }
        }

        public void LogFeedback(int faqId, bool isHelpful)
        {
            try
            {
                string col = isHelpful ? "HelpfulCount" : "UnhelpfulCount";
                DbCommand cmd = db.GetSqlStringCommand(
                    "UPDATE FAQs SET " + col + " = " + col + " + 1 WHERE FAQId = @faqId");
                db.AddInParameter(cmd, "@faqId", DbType.Int32, faqId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "LogFeedback");
            }
        }

        public int LogChatHistory(int userId, string searchQuery, int? matchedFAQId, string answer)
        {
            try
            {
                if (userId <= 0) return 0;
                DbCommand cmd = db.GetSqlStringCommand(
                    "INSERT INTO ChatHistory (UserId, SearchQuery, MatchedFAQId, AnswerProvided, ChatTimestamp) " +
                    "VALUES (@userId, @query, @faqId, @answer, GETDATE()); " +
                    "SELECT SCOPE_IDENTITY();");
                db.AddInParameter(cmd, "@userId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@query", DbType.String, searchQuery ?? "");
                db.AddInParameter(cmd, "@faqId", DbType.Int32, (object)matchedFAQId ?? DBNull.Value);
                db.AddInParameter(cmd, "@answer", DbType.String, answer ?? "");
                return Convert.ToInt32(db.ExecuteScalar(cmd));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "LogChatHistory");
                return 0;
            }
        }

        public List<ChatHistory> GetChatHistory(int userId, int days = 30)
        {
            try
            {
                var list = new List<ChatHistory>();
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT TOP 100 ChatHistoryId, UserId, SearchQuery, MatchedFAQId, " +
                    "AnswerProvided, UserFeedback, ChatTimestamp " +
                    "FROM ChatHistory WHERE UserId = @userId " +
                    "AND ChatTimestamp >= DATEADD(DAY, -@days, GETDATE()) " +
                    "ORDER BY ChatTimestamp DESC");
                db.AddInParameter(cmd, "@userId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@days", DbType.Int32, days);
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new ChatHistory
                        {
                            ChatHistoryId = Convert.ToInt32(dr["ChatHistoryId"]),
                            UserId = Convert.ToInt32(dr["UserId"]),
                            SearchQuery = dr["SearchQuery"].ToString(),
                            MatchedFAQId = dr["MatchedFAQId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["MatchedFAQId"]),
                            AnswerProvided = dr["AnswerProvided"] == DBNull.Value ? null : dr["AnswerProvided"].ToString(),
                            UserFeedback = dr["UserFeedback"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(dr["UserFeedback"]),
                            ChatTimestamp = Convert.ToDateTime(dr["ChatTimestamp"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "GetChatHistory");
                throw;
            }
        }

        public bool UpdateChatFeedback(int chatHistoryId, bool isHelpful)
        {
            try
            {
                DbCommand cmd = db.GetSqlStringCommand(
                    "UPDATE ChatHistory SET UserFeedback = @feedback WHERE ChatHistoryId = @id");
                db.AddInParameter(cmd, "@id", DbType.Int32, chatHistoryId);
                db.AddInParameter(cmd, "@feedback", DbType.Boolean, isHelpful);
                return db.ExecuteNonQuery(cmd) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "UpdateChatFeedback");
                throw;
            }
        }

        public List<ComplaintType> GetComplaintTypes()
        {
            try
            {
                var list = new List<ComplaintType>();
                DbCommand cmd = db.GetSqlStringCommand(
                    "SELECT ComplaintTypeId, TypeName, Description, Category, IconClass, ResolveTimeHours " +
                    "FROM ComplaintTypes WHERE IsActive = 1 ORDER BY Category, TypeName");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new ComplaintType
                        {
                            ComplaintTypeId = Convert.ToInt32(dr["ComplaintTypeId"]),
                            TypeName = dr["TypeName"].ToString(),
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                            Category = dr["Category"] == DBNull.Value ? "" : dr["Category"].ToString(),
                            IconClass = dr["IconClass"] == DBNull.Value ? "" : dr["IconClass"].ToString(),
                            ResolveTimeHours = Convert.ToInt32(dr["ResolveTimeHours"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "GetComplaintTypes");
                throw;
            }
        }

        private FAQ MapFAQ(IDataReader dr)
        {
            try
            {
                return new FAQ
                {
                    FAQId = Convert.ToInt32(dr["FAQId"]),
                    Question = dr["Question"].ToString(),
                    Answer = dr["Answer"].ToString(),
                    Category = dr["Category"].ToString(),
                    SubCategory = dr["SubCategory"] == DBNull.Value ? "" : dr["SubCategory"].ToString(),
                    Keywords = dr["Keywords"] == DBNull.Value ? "" : dr["Keywords"].ToString(),
                    ViewCount = dr["ViewCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ViewCount"]),
                    HelpfulCount = dr["HelpfulCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HelpfulCount"]),
                    UnhelpfulCount = dr["UnhelpfulCount"] == DBNull.Value ? 0 : Convert.ToInt32(dr["UnhelpfulCount"])
                };
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "ChatBotDAL", "MapFAQ");
                throw;
            }
        }
    }
}