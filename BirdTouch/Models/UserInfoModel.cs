using System;

namespace BirdTouch.Models
{
    public class UserInfoModel
    {
        public Guid Id { get; set; }
        public Guid FkUserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DateOfBirth { get; set; }
        public string Adress { get; set; }
        public string Description { get; set; }
        public byte[] ProfilePictureData { get; set; }
        public string FbLink { get; set; }
        public string TwitterLink { get; set; }
        public string GPlusLink { get; set; }
        public string LinkedInLink { get; set; }
    }
}