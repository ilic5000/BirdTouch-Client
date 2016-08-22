using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BirdTouch.Models
{
    public class Business
    {

        public int IdBusinessOwner { get; set; }
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