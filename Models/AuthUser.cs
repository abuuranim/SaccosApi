namespace ASPNetCoreAuth.Models
{
    public class AuthUser
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int StageID { get; set; }
        public string? StageName { get; set; }
        public bool IsActive { get; set; }


    }
}
