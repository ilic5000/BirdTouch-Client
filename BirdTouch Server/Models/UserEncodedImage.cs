using System;

namespace BirdTouch_Server.Models
{
    public class UserEncodedImage
    {

        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DateOfBirth { get; set; }
        public string Adress { get; set; }

        public String ProfilePictureDataEncoded { get; set; }

        public string FbLink { get; set; }

        public string TwitterLink { get; set; }

        public string GPlusLink { get; set; }

        public string LinkedInLink { get; set; }

    }
}