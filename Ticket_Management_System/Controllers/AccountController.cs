using System;
using System.Data;
using System.Data.Common;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.EnterpriseLibrary.Data;
using TicketDAL.Dal;
using Ticket_Management_System.Helpers.Security;
using Ticket_Management_System.Helpers;
using TicketModel;
using TicketModel.ViewModel;

namespace Ticket_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private UserDAL userDAL = new UserDAL();
        private OtpDataAccess otpDAL = new OtpDataAccess();
        private const int EmployeeRoleId = 3;

        private const string OtpPurposeRegistration = "Registration";
        private const string OtpPurposePasswordReset = "PasswordReset";

        private int OtpExpiryMinutes { get { return ReadIntSetting("OtpExpiryMinutes", 5); } }
        private int OtpMaxAttempts { get { return ReadIntSetting("OtpMaxAttempts", 5); } }
        private int OtpResendCooldownSeconds { get { return ReadIntSetting("OtpResendCooldownSeconds", 60); } }

        private static int ReadIntSetting(string key, int fallback)
        {
            int value;
            string raw = System.Configuration.ConfigurationManager.AppSettings[key];
            return !string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out value) ? value : fallback;
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private int GetCurrentUserId()
        {
            var principal = (ClaimsPrincipal)User;
            var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null) throw new UnauthorizedAccessException();
            return Convert.ToInt32(claim.Value);
        }

        private bool IsLoggedIn()
        {
            var cookie = Request.Cookies[AuthCookieHelper.AccessTokenCookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return false;
            return new JwtTokenService().ValidateAccessToken(cookie.Value) != null;
        }

        // ── Register ────────────────────────────────────────────────────────

        [AllowAnonymous]
        public ActionResult Register()
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                if (userDAL.GetUserByEmail(model.Email) != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                var hashResult = PasswordHasher.HashPassword(model.Password);
                int newUserId = userDAL.InsertUser(model.Email, hashResult.Hash, hashResult.Salt, EmployeeRoleId, isVerified: false);
                userDAL.InsertUserDetail(newUserId, model.UserName, model.Address, model.Age, model.Gender, model.City);

                SendAndStoreOtp(newUserId, model.Email, OtpPurposeRegistration);

                TempData["SuccessMessage"] = "Registration successful! A verification code has been sent to your email. Please verify your account to log in.";
                return RedirectToAction("VerifyOtp", new { email = model.Email, purpose = OtpPurposeRegistration });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "Register");
                ModelState.AddModelError("", "Registration failed: " + ex.Message);
                return View(model);
            }
        }

        // ── Login / Logout ───────────────────────────────────────────────────

        [AllowAnonymous]
        public ActionResult Login()
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                UserModel user = userDAL.GetUserByEmail(model.Email);
                if (user == null || !user.IsActive)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                if (!PasswordHasher.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                if (!user.IsVerified)
                {
                    TempData["ErrorMessage"] = "Your email is not verified yet. Please verify it with the code we sent before logging in.";
                    return RedirectToAction("VerifyOtp", new { email = user.Email, purpose = OtpPurposeRegistration });
                }

                var jwtService = new JwtTokenService();
                var refreshService = new RefreshTokenService();
                string accessToken = jwtService.GenerateAccessToken(user.UserId, user.Email, user.RoleName);
                string refreshToken = refreshService.GenerateAndStore(user.UserId);
                AuthCookieHelper.SetAuthCookies(new HttpContextWrapper(System.Web.HttpContext.Current), accessToken, refreshToken);

                return RedirectToRoleDashboard(user.RoleName);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "Login");
                ModelState.AddModelError("", "Something went wrong: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                var refreshCookie = Request.Cookies[AuthCookieHelper.RefreshTokenCookieName];
                if (refreshCookie != null && !string.IsNullOrEmpty(refreshCookie.Value))
                    new RefreshTokenService().Revoke(refreshCookie.Value);

                AuthCookieHelper.ClearAuthCookies(new HttpContextWrapper(System.Web.HttpContext.Current));
                Session.Clear();
                Session.Abandon();
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "Logout");
                return RedirectToAction("Login", "Account");
            }
        }

        // ── Change Password (all logged-in users) ────────────────────────────

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(currentPassword) ||
                    string.IsNullOrWhiteSpace(newPassword) ||
                    string.IsNullOrWhiteSpace(confirmPassword))
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return View();
                }

                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "New password and confirmation do not match.";
                    return View();
                }

                if (newPassword.Length < 8)
                {
                    TempData["ErrorMessage"] = "New password must be at least 8 characters.";
                    return View();
                }

                int uid = GetCurrentUserId();
                UserModel user = userDAL.GetUserById(uid);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return View();
                }

                // Verify current password
                if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return View();
                }

                // Hash new password and save
                var hashResult = PasswordHasher.HashPassword(newPassword);
                var db = DatabaseFactory.CreateDatabase();
                DbCommand cmd = db.GetStoredProcCommand("User_UpdatePassword");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, uid);
                db.AddInParameter(cmd, "@PasswordHash", DbType.String, hashResult.Hash);
                db.AddInParameter(cmd, "@PasswordSalt", DbType.String, hashResult.Salt);
                db.ExecuteNonQuery(cmd);

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Index", "MyAccount");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "ChangePassword");
                TempData["ErrorMessage"] = "An error occurred changing your password.";
                return View();
            }
        }

        // ── Forgot Password ──────────────────────────────────────────────────

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ErrorMessage"] = "Please enter your email address.";
                    return View();
                }

                // Always show success to prevent email enumeration
                TempData["SuccessMessage"] = "If that email is registered, a verification code has been sent.";

                UserModel user = userDAL.GetUserByEmail(email.Trim().ToLower());
                if (user == null || !user.IsActive)
                    return RedirectToAction("ForgotPassword");

                if (!user.IsVerified)
                {
                    TempData["ErrorMessage"] = "This account has not been verified yet. Please verify your email first.";
                    return RedirectToAction("VerifyOtp", new { email = user.Email, purpose = OtpPurposeRegistration });
                }

                // Generate, store and email an OTP for the password reset flow
                SendAndStoreOtp(user.UserId, user.Email, OtpPurposePasswordReset);

                return RedirectToAction("VerifyOtp", new { email = user.Email, purpose = OtpPurposePasswordReset });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "ForgotPassword");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return View();
            }
        }

        // ── Email OTP Verification ───────────────────────────────────────────

        [AllowAnonymous]
        public ActionResult VerifyOtp(string email, string purpose)
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");

            UserModel user = ResolveOtpUser(email, purpose);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid request. Please try again.";
                return RedirectToAction("Login");
            }

            // If no active code exists (expired / consumed / never sent), issue a fresh one.
            OtpRecord active = otpDAL.GetActive(user.UserId, purpose);
            if (active == null)
            {
                SendAndStoreOtp(user.UserId, user.Email, purpose);
                TempData["SuccessMessage"] = "A new verification code has been sent to your email.";
            }

            ViewBag.Email = user.Email;
            ViewBag.Purpose = purpose;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyOtp(string email, string purpose, string otp)
        {
            try
            {
                UserModel user = ResolveOtpUser(email, purpose);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid request. Please try again.";
                    return RedirectToAction("Login");
                }

                ViewBag.Email = user.Email;
                ViewBag.Purpose = purpose;

                if (string.IsNullOrWhiteSpace(otp))
                {
                    ModelState.AddModelError("", "Please enter the verification code.");
                    return View();
                }

                OtpRecord record = otpDAL.GetActive(user.UserId, purpose);
                if (record == null)
                {
                    ModelState.AddModelError("", "This verification code has expired. Request a new one.");
                    return View();
                }

                if (record.Attempts >= record.MaxAttempts)
                {
                    otpDAL.MarkUsed(record.OtpId);
                    SendAndStoreOtp(user.UserId, user.Email, purpose);
                    ModelState.AddModelError("", "Too many incorrect attempts. A new code has been sent to your email.");
                    return View();
                }

                if (string.Compare(OtpGenerator.Hash(otp.Trim()), record.OtpHash, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    otpDAL.IncrementAttempts(record.OtpId);
                    int remaining = record.MaxAttempts - record.Attempts - 1;
                    ModelState.AddModelError("", remaining > 0
                        ? "Incorrect verification code. " + remaining + " attempt(s) remaining."
                        : "Too many incorrect attempts. Request a new code.");
                    return View();
                }

                otpDAL.MarkUsed(record.OtpId);

                if (string.Equals(purpose, OtpPurposePasswordReset, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["OtpVerifiedUserId"] = user.UserId;
                    TempData["SuccessMessage"] = "Code verified. You can now set a new password.";
                    return RedirectToAction("ResetPassword");
                }

                // Registration verification
                userDAL.MarkVerified(user.UserId);
                TempData["SuccessMessage"] = "Your email has been verified successfully. You can now log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "VerifyOtp POST");
                ModelState.AddModelError("", "Verification failed. Please try again.");
                ViewBag.Email = email;
                ViewBag.Purpose = purpose;
                return View();
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResendOtp(string email, string purpose)
        {
            try
            {
                UserModel user = ResolveOtpUser(email, purpose);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid request. Please try again.";
                    return RedirectToAction("Login");
                }

                OtpRecord active = otpDAL.GetActive(user.UserId, purpose);
                if (active != null && active.ResendAt.HasValue && active.ResendAt.Value > DateTime.UtcNow)
                {
                    int waitSeconds = (int)Math.Ceiling((active.ResendAt.Value - DateTime.UtcNow).TotalSeconds);
                    TempData["ErrorMessage"] = "Please wait " + waitSeconds + " second(s) before requesting a new code.";
                }
                else
                {
                    SendAndStoreOtp(user.UserId, user.Email, purpose);
                    TempData["SuccessMessage"] = "A new verification code has been sent to your email.";
                }

                return RedirectToAction("VerifyOtp", new { email = user.Email, purpose = purpose });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "ResendOtp");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("VerifyOtp", new { email = email, purpose = purpose });
            }
        }

        // ── Reset Password (after OTP verification) ──────────────────────────

        [AllowAnonymous]
        public ActionResult ResetPassword()
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");

            object verifiedUserId = TempData["OtpVerifiedUserId"];
            if (verifiedUserId == null)
            {
                TempData["ErrorMessage"] = "Please verify your email before resetting your password.";
                return RedirectToAction("ForgotPassword");
            }

            // Keep the flag alive so the POST below can consume it.
            TempData["OtpVerifiedUserId"] = verifiedUserId;
            ViewBag.VerifiedUserId = (int)verifiedUserId;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            try
            {
                object verifiedUserId = TempData["OtpVerifiedUserId"];
                if (verifiedUserId == null)
                {
                    TempData["ErrorMessage"] = "Please verify your email before resetting your password.";
                    return RedirectToAction("ForgotPassword");
                }

                int userId = (int)verifiedUserId;

                if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    TempData["ErrorMessage"] = "Both password fields are required.";
                    return RedirectToAction("ResetPassword");
                }

                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match.";
                    return RedirectToAction("ResetPassword");
                }

                if (newPassword.Length < 6)
                {
                    TempData["ErrorMessage"] = "Password must be at least 6 characters.";
                    return RedirectToAction("ResetPassword");
                }

                // Hash and save new password
                var hashResult = PasswordHasher.HashPassword(newPassword);
                var db = DatabaseFactory.CreateDatabase();
                DbCommand updateCmd = db.GetStoredProcCommand("User_UpdatePassword");
                db.AddInParameter(updateCmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(updateCmd, "@PasswordHash", DbType.String, hashResult.Hash);
                db.AddInParameter(updateCmd, "@PasswordSalt", DbType.String, hashResult.Salt);
                db.ExecuteNonQuery(updateCmd);

                TempData["SuccessMessage"] = "Password reset successfully. You can now log in with your new password.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Account", "ResetPassword POST");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("ResetPassword");
            }
        }

        [AllowAnonymous]
        public ActionResult AccessDenied()
        {
            return View();
        }

        // ── private helpers ──────────────────────────────────────────────────

        private ActionResult RedirectToRoleDashboard(string roleName)
        {
            switch (roleName)
            {
                case "Administrator": return RedirectToAction("Dashboard", "Admin");
                case "Support Executive": return RedirectToAction("Index", "Support");
                default: return RedirectToAction("Index", "Ticket");
            }
        }

        private UserModel ResolveOtpUser(string email, string purpose)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(purpose)) return null;
            UserModel user = userDAL.GetUserByEmail(email.Trim().ToLower());
            if (user == null || !user.IsActive) return null;
            return user;
        }

        /// <summary>
        /// Generates a secure OTP, stores only its hash with expiry/cooldown,
        /// invalidates any previous unused codes, then emails the code.
        /// </summary>
        private void SendAndStoreOtp(int userId, string email, string purpose)
        {
            string otp = OtpGenerator.Generate();

            otpDAL.InvalidateOld(userId, purpose);
            otpDAL.Insert(
                userId,
                purpose,
                OtpGenerator.Hash(otp),
                DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                OtpMaxAttempts,
                DateTime.UtcNow.AddSeconds(OtpResendCooldownSeconds));

            AccountEmailService.SendOtp(email, otp, purpose);
        }
    }
}