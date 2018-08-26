namespace BirdTouch.Models
{
    class LoginResponse
    {
        public UserInfoModel User { get; set; }
        public string JwtToken { get; set; }
    }
}