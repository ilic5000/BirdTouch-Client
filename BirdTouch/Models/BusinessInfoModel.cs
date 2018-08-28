using System;

namespace BirdTouch.Models
{
    public class BusinessInfoModel
    {
        public Guid Id { get; set; }
        public Guid FkUserId { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Website { get; set; }
        public string Adress { get; set; }
        public string Description { get; set; }
        public byte[] ProfilePictureData { get; set; }
    }
}