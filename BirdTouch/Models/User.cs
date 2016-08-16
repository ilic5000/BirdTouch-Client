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
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DateOfBirth { get; set; }
        public string Adress { get; set; }

        public byte[] ProfilePictureData
        {
            get
            {
                if(ProfilePictureDataEncoded != "" && ProfilePictureDataEncoded != null)
                {
                    byte[] image = Convert.FromBase64String(ProfilePictureDataEncoded);
                    return image;
                }
                return null;
            }
        }

        public String ProfilePictureDataEncoded { get; set; }

        public string FbLink { get; set; }

        public string TwitterLink { get; set; }

        public string GPlusLink { get; set; }

        public string LinkedInLink { get; set; }
    }
}