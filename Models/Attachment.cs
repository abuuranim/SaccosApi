using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class Attachment
    {
        public Guid AttachmentID { get; set; }
        public string? FileContent { get; set; }

        [Required(ErrorMessage = "Loan application id is required")]
        public Guid LoanApplicationID { get; set; }

        [Required(ErrorMessage = "File name is required")]
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string? FilePath { get; set; }
        public string? AttachmentType { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
