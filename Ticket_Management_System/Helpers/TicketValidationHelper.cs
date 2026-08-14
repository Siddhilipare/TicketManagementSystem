using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace Ticket_Management_System.Helpers
{
    public class TicketValidationHelper
    {
        public static readonly string[] AllowedAttachmentExtensions =
          { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;
        public const int MaxAttachmentsPerTicket = 5;

        /// <returns>An error message if invalid, or null if the title is valid.</returns>
        public static string ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3 || title.Trim().Length > 100)
                return "Title must be between 3 and 100 characters.";
            return null;
        }

        /// <returns>An error message if invalid, or null if the description is valid.</returns>
        public static string ValidateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10 || description.Trim().Length > 2000)
                return "Description must be between 10 and 2000 characters.";
            return null;
        }

        /// <returns>An error message if any attachment is invalid, or null if the whole set is valid.</returns>
        public static string ValidateAttachments(List<HttpPostedFileBase> files)
        {
            if (files == null) return null;

            var actualFiles = files.Where(f => f != null && f.ContentLength > 0).ToList();
            if (actualFiles.Count > MaxAttachmentsPerTicket)
                return "You can upload a maximum of " + MaxAttachmentsPerTicket + " files at a time.";

            foreach (var file in actualFiles)
            {
                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (Array.IndexOf(AllowedAttachmentExtensions, ext) < 0)
                    return "'" + file.FileName + "' has an invalid file type. Allowed: jpg, jpeg, png, gif, bmp, pdf, txt, doc, docx.";
                if (file.ContentLength > MaxFileSizeBytes)
                    return "'" + file.FileName + "' exceeds the 5 MB size limit.";
            }
            return null;
        }
    }
}