using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using TicketModel.Models;
using TicketDAL.Dal;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Controllers
{
    [Authorize]
    public class ChatBotController : Controller
    {
        private ChatBotDAL _dal = new ChatBotDAL();

        public ActionResult Index()
        {
            try
            {
                var model = new ChatBotViewModel
                {
                    Categories = _dal.GetAllCategories(),
                    ComplaintTypes = _dal.GetComplaintTypes(),
                    FAQs = _dal.GetAllFAQs()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "Index");
                ViewBag.Error = "An error occurred while loading the help center.";
                return View(new ChatBotViewModel());
            }
        }

        public ActionResult ChatHistory(int? days)
        {
            try
            {
                int userId = GetCurrentUserId();
                int daysToShow = days ?? 30;
                var history = _dal.GetChatHistory(userId, daysToShow);
                return View(history);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "ChatHistory");
                ViewBag.Error = "An error occurred while loading chat history.";
                return View(new List<ChatHistory>());
            }
        }

        [HttpGet]
        public JsonResult GetFAQs(string category = "")
        {
            try
            {
                List<FAQ> faqs = string.IsNullOrEmpty(category)
                    ? _dal.GetAllFAQs()
                    : _dal.GetFAQsByCategory(category);
                return Json(new { success = true, data = faqs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "GetFAQs");
                return Json(new { success = false, message = "Failed to load FAQs." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SearchFAQ(string searchText)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return Json(new { success = false, message = "Please enter a search term." });

                var results = _dal.SearchFAQs(searchText, maxResults: 5);
                sw.Stop();

                int userId = GetCurrentUserId();
                try
                {
                    if (results.Count > 0)
                    {
                        _dal.LogChatHistory(userId, searchText, results[0].FAQId, results[0].Answer);
                        _dal.IncrementViewCount(results[0].FAQId);
                    }
                    else
                    {
                        _dal.LogChatHistory(userId, searchText, null, null);
                    }
                }
                catch { }

                return Json(new
                {
                    success = true,
                    data = results,
                    totalResults = results.Count,
                    responseMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "SearchFAQ");
                return Json(new { success = false, message = "Search failed. Please try again." });
            }
        }

        [HttpGet]
        public JsonResult GetComplaintTypes()
        {
            try
            {
                var types = _dal.GetComplaintTypes();
                return Json(new { success = true, data = types }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "GetComplaintTypes");
                return Json(new { success = false, message = "Failed to load complaint types." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RateFeedback(int chatHistoryId, bool isHelpful)
        {
            try
            {
                bool updated = _dal.UpdateChatFeedback(chatHistoryId, isHelpful);
                return Json(updated
                    ? new { success = true, message = "Thank you for your feedback!" }
                    : new { success = false, message = "Failed to save feedback." });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "RateFeedback");
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpGet]
        public JsonResult GetFAQDetail(int faqId)
        {
            try
            {
                var faq = _dal.GetFAQById(faqId);
                if (faq == null)
                    return Json(new { success = false, message = "FAQ not found." }, JsonRequestBehavior.AllowGet);

                _dal.IncrementViewCount(faqId);
                return Json(new { success = true, data = faq }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "ChatBot", "GetFAQDetail");
                return Json(new { success = false, message = "Failed to load FAQ." }, JsonRequestBehavior.AllowGet);
            }
        }

        private int GetCurrentUserId()
        {
            return Session["UserId"] != null ? (int)Session["UserId"] : 0;
        }
    }

    [Authorize(Roles = "Administrator")]
    public class AdminChatBotController : Controller
    {
        private ChatBotDAL _dal = new ChatBotDAL();

        public ActionResult Index(string category = "")
        {
            try
            {
                List<FAQ> faqs = string.IsNullOrEmpty(category)
                    ? _dal.GetAllFAQs()
                    : _dal.GetFAQsByCategory(category);

                ViewBag.Categories = _dal.GetAllCategories();
                ViewBag.SelectedCategory = category;
                return View(faqs);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "AdminChatBot", "Index");
                ViewBag.Error = "An error occurred while loading FAQs.";
                return View(new List<FAQ>());
            }
        }

        public ActionResult Create()
        {
            try
            {
                ViewBag.Categories = _dal.GetAllCategories();
                return View(new FAQ());
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "AdminChatBot", "Create");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FAQ faq)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = _dal.GetAllCategories();
                    ViewBag.Error = "Please fill all required fields.";
                    return View(faq);
                }

                faq.CreatedBy = GetCurrentUserId();
                int newId = _dal.CreateFAQ(faq);
                TempData["SuccessMessage"] = "FAQ created successfully. ID: " + newId;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "AdminChatBot", "Create");
                ViewBag.Categories = _dal.GetAllCategories();
                ViewBag.Error = "An error occurred while creating the FAQ.";
                return View(faq);
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                var faq = _dal.GetFAQById(id);
                if (faq == null)
                {
                    TempData["ErrorMessage"] = "FAQ not found.";
                    return RedirectToAction("Index");
                }
                ViewBag.Categories = _dal.GetAllCategories();
                return View(faq);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "AdminChatBot", "Edit");
                TempData["ErrorMessage"] = "An error occurred while loading the FAQ.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FAQ faq)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = _dal.GetAllCategories();
                    ViewBag.Error = "Please fill all required fields.";
                    return View(faq);
                }

                bool updated = _dal.UpdateFAQ(faq);
                if (updated)
                {
                    TempData["SuccessMessage"] = "FAQ updated successfully.";
                    return RedirectToAction("Index");
                }

                ViewBag.Categories = _dal.GetAllCategories();
                ViewBag.Error = "Failed to update FAQ.";
                return View(faq);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "AdminChatBot", "Edit");
                ViewBag.Categories = _dal.GetAllCategories();
                ViewBag.Error = "An error occurred while updating the FAQ.";
                return View(faq);
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                bool deleted = _dal.DeleteFAQ(id);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted ? "FAQ deleted successfully." : "Failed to delete FAQ.";
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "AdminChatBot", "Delete");
                TempData["ErrorMessage"] = "An error occurred while deleting the FAQ.";
            }
            return RedirectToAction("Index");
        }

        private int GetCurrentUserId()
        {
            try
            {
                var principal = (System.Security.Claims.ClaimsPrincipal)User;
                var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                {
                    int uid;
                    if (int.TryParse(claim.Value, out uid))
                        return uid;
                }
            }
            catch { }
            return 0;
        }
    }
}
