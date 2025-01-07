using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class Heir
    {
        public Int64 Id { get; set; }

        [Required(ErrorMessage = "Relationship is required")]
        public int RelationshipId { get; set; }

        public string? RelationshipName { get; set; }

        public Guid MemberID { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        public string? FullName { get; set; }


        [Required(ErrorMessage = "MobileNo is required")]
        public string MobileNo { get; set; }

    }
}
