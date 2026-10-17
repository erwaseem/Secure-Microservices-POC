using System.Text.Json.Serialization;

namespace UI.Models
{
    public class LoginViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        public string  Username { get; set; } = string.Empty;
        public string  Password { get; set; } = string.Empty;
        public string  FullName { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username    { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName    { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role        { get; set; } = string.Empty;
    }

    public class CommentViewModel
    {
        [JsonPropertyName("id")]
        public int      Id          { get; set; }

        [JsonPropertyName("username")]
        public string   Username    { get; set; } = string.Empty;

        [JsonPropertyName("comment")]
        public string   Comment     { get; set; } = string.Empty;

        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc  { get; set; }
    }
}
