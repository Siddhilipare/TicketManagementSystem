using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel.ViewModels
{
    public class BulkImportRowResult
    {
        public int RowNumber { get; set; }
        public string Title { get; set; }
        public bool Success { get; set; }
        public int? TicketId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class BulkImportResultViewModel
    {
        public List<BulkImportRowResult> Results { get; set; } = new List<BulkImportRowResult>();
        public int SuccessCount => Results.Count(r => r.Success);
        public int FailureCount => Results.Count(r => !r.Success);

    }
}
