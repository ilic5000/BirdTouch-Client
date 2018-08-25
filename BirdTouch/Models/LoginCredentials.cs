using Newtonsoft.Json;

namespace BirdTouch.Models
{
    class LoginCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}