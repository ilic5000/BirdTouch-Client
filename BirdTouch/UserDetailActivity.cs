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
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using BirdTouch.Models;
using Android.Graphics;

namespace BirdTouch
{
    [Activity(Label ="UserDetailActivity", Theme = "@style/Theme.DesignDemo")]
    public class UserDetailActivity : AppCompatActivity
    {

        private User user;
        private ImageView imageView;

        private TextView firstNameWrapper;
        private TextView lastNameWrapper;
        private TextView emailWrapper;
        private TextView adressWrapper;
        private TextView phoneWrapper;
        private TextView dateOfBirthWrapper;
        private TextView facebookLinkWrapper;
        private TextView twitterLinkWrapper;
        private TextView gPlusLinkWrapper;
        private TextView linkedInLinkWrapper;
        private FloatingActionButton fabSaveUser;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.UserDetailActivity);

            imageView = FindViewById<ImageView>(Resource.Id.profile_picture_private_userinfo_show_detail);
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_private_userinfo_show_detail); //nije isti toolbar kao u startpage

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = "";



            user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userInformation"));


            if (user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(user.ProfilePictureData, 0, user.ProfilePictureData.Length).Result;
                imageView.SetImageBitmap(bm);
            }
            else
            {   //defaultni image kada korisnik jos uvek nije promenio, mada moze i u axml da se postavi
                imageView.SetImageResource(Resource.Drawable.blank_user_profile);
            }


            
            firstNameWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailFirstname);
            lastNameWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailLastname);
            emailWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailEmail);
            adressWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailAdress);
            phoneWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailPhoneNumber);
            dateOfBirthWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailDateOfBirth);
            facebookLinkWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailFacebook);
            twitterLinkWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailTwitter);
            gPlusLinkWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailGooglePlus);
            linkedInLinkWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailLinkedIn);


            firstNameWrapper.Text = user.FirstName;
            lastNameWrapper.Text = user.LastName;
            emailWrapper.Text = user.Email;
            adressWrapper.Text = user.Adress;
            phoneWrapper.Text = user.PhoneNumber;
            dateOfBirthWrapper.Text = user.DateOfBirth;
            facebookLinkWrapper.Text = user.FbLink;
            twitterLinkWrapper.Text = user.TwitterLink;
            gPlusLinkWrapper.Text = user.GPlusLink;
            linkedInLinkWrapper.Text = user.LinkedInLink;


        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;

            }
            return base.OnOptionsItemSelected(item);
        }


    
       
    }
}