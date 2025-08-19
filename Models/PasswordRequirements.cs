namespace NewVivaApi.Models
{
    public class PasswordRequirements
    {
        public bool RequireNumber { get; set; }
        public bool RequireSymbol { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireUppercase { get; set; }
        public int MinimumLength { get; set; }
        public int MaximumLength { get; set; }
    }
}
