using System.ComponentModel.DataAnnotations;

namespace SaccosApi.DTO
{
    public class HeirInformation
    {
        public Nullable<Guid> MemberID { get; set; }
        public int RelationshipID { get; set; }
        public string? FullName { get; set; }
        public string? MobileNo { get; set; }

    }
}
