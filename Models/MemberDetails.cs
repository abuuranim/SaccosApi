namespace SaccosApi.Models
{
    public class MemberDetails
    {
        public MemberDetails() {

            this.Heirs = new HashSet<Heir>();
        }
        public Guid MemberID { get; set; }
        public string? PFCheckNo { get; set; }
        public string? MembershipNo { get; set; }
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string? NationalID { get; set; }
        public string? MobileNo { get; set; }
        public string? EmailAddress { get; set; }
        public string? JobTitle { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? Status { get; set; }
        public int StageID { get; set; }
        public string? StageName { get; set; }
        public ICollection<Heir>? Heirs { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModified { get; set; }

    }
}


