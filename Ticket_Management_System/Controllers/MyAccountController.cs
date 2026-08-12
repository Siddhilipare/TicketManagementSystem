using System;
using System.Data;
using System.Data.Common;
using System.Security.Claims;
using System.Web.Mvc;
using Microsoft.Practices.EnterpriseLibrary.Data;
using TicketModel;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Controllers
{
    [Authorize]
    public class MyAccountController : Controller
    {
        private int GetCurrentUserId()
        {
            var principal = (ClaimsPrincipal)User;
            var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null) throw new UnauthorizedAccessException("Unable to identify current user.");
            return Convert.ToInt32(claim.Value);
        }

        // GET: /MyAccount/Index
        public ActionResult Index()
        {
            try
            {
                int uid = GetCurrentUserId();
                UserDetailModel profile;
                string email, roleName;
                GetProfile(uid, out profile, out email, out roleName);

                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Email = email;
                ViewBag.RoleName = roleName;
                return View(profile);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "MyAccount", "Index");
                TempData["ErrorMessage"] = "An error occurred loading your profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /MyAccount/Edit
        public ActionResult Edit()
        {
            try
            {
                int uid = GetCurrentUserId();
                UserDetailModel profile;
                string email, roleName;
                GetProfile(uid, out profile, out email, out roleName);

                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Profile not found.";
                    return RedirectToAction("Index");
                }

                ViewBag.Email = email;
                ViewBag.RoleName = roleName;
                return View(profile);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "MyAccount", "Edit");
                TempData["ErrorMessage"] = "An error occurred loading your profile.";
                return RedirectToAction("Index");
            }
        }

        // POST: /MyAccount/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserDetailModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.UserName) || model.UserName.Trim().Length < 2)
                    ModelState.AddModelError("UserName", "Name must be at least 2 characters.");

                if (model.Age.HasValue && (model.Age.Value < 16 || model.Age.Value > 100))
                    ModelState.AddModelError("Age", "Age must be between 16 and 100.");

                if (!ModelState.IsValid)
                {
                    // reload display-only fields for the form
                    int uid2 = GetCurrentUserId();
                    UserDetailModel p2; string e2, r2;
                    GetProfile(uid2, out p2, out e2, out r2);
                    ViewBag.Email = e2; ViewBag.RoleName = r2;
                    return View(model);
                }

                int uid = GetCurrentUserId();

                var db = DatabaseFactory.CreateDatabase();
                DbCommand cmd = db.GetStoredProcCommand("UserDetail_UpdateByUserId");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, uid);
                db.AddInParameter(cmd, "@UserName", DbType.String, model.UserName.Trim());
                db.AddInParameter(cmd, "@Address", DbType.String, string.IsNullOrWhiteSpace(model.Address) ? (object)DBNull.Value : model.Address.Trim());
                db.AddInParameter(cmd, "@Age", DbType.Int32, model.Age.HasValue ? (object)model.Age.Value : DBNull.Value);
                db.AddInParameter(cmd, "@Gender", DbType.String, string.IsNullOrWhiteSpace(model.Gender) ? (object)DBNull.Value : model.Gender);
                db.AddInParameter(cmd, "@City", DbType.String, string.IsNullOrWhiteSpace(model.City) ? (object)DBNull.Value : model.City.Trim());

                db.ExecuteNonQuery(cmd);
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "MyAccount", "Edit POST");
                ModelState.AddModelError("", "An error occurred saving your profile.");
                return View(model);
            }
        }

        // ── helper ──────────────────────────────────────────────────────────
        private void GetProfile(int userId, out UserDetailModel model, out string email, out string roleName)
        {
            model = null;
            email = "";
            roleName = "";

            var db = DatabaseFactory.CreateDatabase();
            DbCommand cmd = db.GetStoredProcCommand("UserDetail_GetByUserId");
            db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);

            using (IDataReader dr = db.ExecuteReader(cmd))
            {
                if (dr.Read())
                {
                    model = new UserDetailModel
                    {
                        UserDetailId = Convert.ToInt32(dr["UserDetailId"]),
                        UserId = Convert.ToInt32(dr["UserId"]),
                        UserName = dr["UserName"] == DBNull.Value ? "" : dr["UserName"].ToString(),
                        Address = dr["Address"] == DBNull.Value ? "" : dr["Address"].ToString(),
                        Age = dr["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["Age"]),
                        Gender = dr["Gender"] == DBNull.Value ? "" : dr["Gender"].ToString(),
                        City = dr["City"] == DBNull.Value ? "" : dr["City"].ToString()
                    };
                    email = dr["Email"].ToString();
                    roleName = dr["RoleName"].ToString();
                }
            }
        }
    }
}