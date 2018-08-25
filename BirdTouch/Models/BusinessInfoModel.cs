using System;

namespace BirdTouch.Models
{
    public class BusinessInfoModel
    {

        public Guid IdBusinessOwner { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Website { get; set; }
        public string Adress { get; set; }

        public byte[] ProfilePictureData
        {
            get
            {
                if (ProfilePictureDataEncoded != "" && ProfilePictureDataEncoded != null)
                {
                    byte[] image = Convert.FromBase64String(ProfilePictureDataEncoded);
                    return image;
                }
                return null;
            }
        }

        public String ProfilePictureDataEncoded { get; set; }

    }
}