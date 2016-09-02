using System;

namespace BirdTouch_Server.Models
{
    public class BusinessEncoded
    {
        public int IdBusinessOwner { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Website { get; set; }
        public string Adress { get; set; }
        public String ProfilePictureDataEncoded { get; set; }
    }
}