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

        private bool isSaved;

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

            fabSaveUser = FindViewById<FloatingActionButton>(Resource.Id.fabPrivateUserInfoSaveUser);

            user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userInformation"));
            isSaved = Intent.GetBooleanExtra("isSaved",false);

            if (user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(user.ProfilePictureData, 0, user.ProfilePictureData.Length).Result;
                imageView.SetImageBitmap(bm);
            }
            else
            {   //defaultni image kada korisnik jos uvek nije promenio, mada moze i u axml da se postavi
                imageView.SetImageResource(Resource.Drawable.blank_navigation);
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


            if (isSaved)
            {
                fabSaveUser.SetImageResource(Resource.Drawable.ic_done);
            }else
            {
                fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
            }

            fabSaveUser.Click += FabSaveUser_Click;
        }

        private void FabSaveUser_Click(object sender, EventArgs e)
        {

            //int userId = StartPageActivity.user.Id;

            //ISharedPreferences pref = ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
            //ISharedPreferencesEditor edit = pref.Edit();

            //if (pref.Contains("SavedUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
            //{
            //    string serializedDictionary = pref.GetString("SavedUsersDictionary", String.Empty);
            //    if (serializedDictionary != String.Empty)
            //    {

            //        Dictionary<int, Dictionary<int, List<User>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<User>>>>(serializedDictionary);
            //        if (!dictionary.ContainsKey(userId))
            //        {//ako user nije uopste dodavao usere
            //            dictionary.Add(userId, new Dictionary<int, List<User>>());
            //        }
            //        if (!dictionary[userId].ContainsKey(1))
            //        {//ako nije dodavao private usere
            //            dictionary[userId].Add(1, new List<User>());
            //        }

            //        //samo dodamo private usera iz recyclerViewa
            //        dictionary[userId][1].Add(mValues[position]);
            //        edit.Clear();
            //        edit.PutString("SavedUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
            //        edit.Apply();
            //        Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
            //        refToSavedUsersFragment.SetUpRecyclerView();

            //    }
            //}



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