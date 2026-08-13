using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TicketModel.Models
{
    // ── FAQ Model ──────────────────────────────────────────────────────────
    public class FAQ
    {
        [Key]
        public int FAQId { get; set; }

        [Required]
        [StringLength(500)]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        [StringLength(100)]
        public string SubCategory { get; set; }

        [StringLength(500)]
        public string Keywords { get; set; }

        public bool IsActive { get; set; }

        public int ViewCount { get; set; }

        public int HelpfulCount { get; set; }

        public int UnhelpfulCount { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }

    // ── ChatHistory Model ──────────────────────────────────────────────────  
    public class ChatHistory
    {
        [Key]
        public int ChatHistoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string SearchQuery { get; set; }

        public int? MatchedFAQId { get; set; }

        public string AnswerProvided { get; set; }

        public bool? UserFeedback { get; set; }

        public int? ResponseTimeMs { get; set; }

        public DateTime ChatTimestamp { get; set; }
    }

    // ── ComplaintType Model ────────────────────────────────────────────────
    public class ComplaintType
    {
        [Key]
        public int ComplaintTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; }

        public string Description { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(50)]
        public string IconClass { get; set; }

        public int ResolveTimeHours { get; set; }

        public bool IsActive { get; set; }
    }

    // ── ViewModel for the Chatbot page ─────────────────────────────────────
    public class ChatBotViewModel
    {
        public List<FAQ> FAQs { get; set; }
        public List<string> Categories { get; set; }
        public List<ComplaintType> ComplaintTypes { get; set; }
        public string SearchQuery { get; set; }

        public ChatBotViewModel()
        {
            FAQs = new List<FAQ>();
            Categories = new List<string>();
            ComplaintTypes = new List<ComplaintType>();
        }
    }

    // ── API response wrapper (used by AJAX calls) ──────────────────────────
    public class ChatBotResponse
    {
        public bool Success { get; set; }
        public string Answer { get; set; }
        public string MatchedQuestion { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }      // "faq_db", "greeting", "guard", "no_match"
        public List<FAQ> Also { get; set; }      // related FAQs

        public ChatBotResponse()
        {
            Also = new List<FAQ>();
        }
    }
}