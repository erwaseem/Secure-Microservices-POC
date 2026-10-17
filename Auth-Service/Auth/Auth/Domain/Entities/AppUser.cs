namespace SecureMicroservicesPOC.Domain.Entities
{
    /// <summary>Simple user entity — no ASP.NET Identity dependency.</summary>
    public class AppUser
    {
        public int      Id          { get; set; }
        public string   Username    { get; set; } = string.Empty;
        public string   Password    { get; set; } = string.Empty;   // plain text (demo only)
        public string   FullName    { get; set; } = string.Empty;
        public string   Role        { get; set; } = "User";
        public DateTime CreatedUtc  { get; set; } = DateTime.UtcNow;
    }
}
