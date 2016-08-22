using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using BirdTouch.Models;
using Android.Graphics;
using BirdTouch.Fragments;

namespace BirdTouch
{
    [Activity(Label = "BusinessDetailActivity", Theme = "@style/Theme.DesignDemo")]
    public class BusinessDetailActivity : AppCompatActivity
    {

        private Business user;
        private ImageView imageView;

        private TextView companyNameWrapper;
        private TextView emailWrapper;
        private TextView adressWrapper;
        private TextView phoneWrapper;
        private TextView websiteOfBirthWrapper;

        private FloatingActionButton fabSaveUser;

        private bool isSaved;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.BusinessDetailActivity);

            imageView = FindViewById<ImageView>(Resource.Id.profile_picture_business_userinfo_show_detail);
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_business_userinfo_show_detail); //nije isti toolbar kao u startpage

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = "";

            fabSaveUser = FindViewById<FloatingActionButton>(Resource.Id.fabBusinessUserInfoSaveUser);

            user = Newtonsoft.Json.JsonConvert.DeserializeObject<Business>(Intent.GetStringExtra("userInformation"));
            isSaved = Intent.GetBooleanExtra("isSaved", false);

            if (user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(user.ProfilePictureData, 0, user.ProfilePictureData.Length).Result;
                imageView.SetImageBitmap(bm);
            }
            else
            {   //defaultni image kada korisnik jos uvek nije promenio, mada moze i u axml da se postavi
                imageView.SetImageResource(Resource.Drawable.blank_navigation);
            }



            companyNameWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessCompanyNameShowDetail);
            websiteOfBirthWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessWebsiteShowDetail);
            emailWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessEmailShowDetail);
            adressWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessAdressShowDetail);
            phoneWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessPhoneNumberShowDetail);



            companyNameWrapper.Text = user.CompanyName;
            websiteOfBirthWrapper.Text = user.Website;
            emailWrapper.Text = user.Email;
            adressWrapper.Text = user.Adress;
            phoneWrapper.Text = user.PhoneNumber;
            


            if (isSaved)
            {
                fabSaveUser.SetImageResource(Resource.Drawable.ic_done);
            }
            else
            {
                fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
            }

            fabSaveUser.Click += FabSaveUser_Click;
        }

        private void FabSaveUser_Click(object sender, EventArgs e)
        {
            int userId = StartPageActivity.user.Id;

            ISharedPreferences pref = ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
            ISharedPreferencesEditor edit = pref.Edit();


            if (isSaved)
            {

                string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                if (serializedDictionary != String.Empty)
                {
                    Dictionary<int, Dictionary<int, List<Business>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<Business>>>>(serializedDictionary);
                    dictionary[userId][1].RemoveAll(a => a.IdBusinessOwner == user.IdBusinessOwner);
                    edit.Remove("SavedBusinessUsersDictionary");
                    edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();


                    Fragment2_Business refToSavedUsersFragment = (Fragment2_Business)StartPageActivity.adapter.GetItem(2);
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment2_BusinessSavedUsers refToSavedUsersFragment2 = (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(3);
                    refToSavedUsersFragment2.SetUpRecyclerView();
                }

                fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
                isSaved = false;
            }
            else
            {


                if (!pref.Contains("SavedBusinessUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                {
                    // Snackbar.Make((View)sender, Android.Text.Html.FromHtml("<font color=\"#ffffff\">does not contain</font>"), Snackbar.LengthLong).Show();

                    Dictionary<int, Dictionary<int, List<Business>>> dictionary = new Dictionary<int, Dictionary<int, List<Business>>>();
                    dictionary.Add(userId, new Dictionary<int, List<Business>>());
                    dictionary[userId].Add(1, new List<Business>());// 1 je private mode
                    dictionary[userId][1].Add(user);

                    edit.Remove("SavedBusinessUsersDictionary");
                    edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();
                    Fragment2_Business refToSavedUsersFragment = (Fragment2_Business)StartPageActivity.adapter.GetItem(2);
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment2_BusinessSavedUsers refToSavedUsersFragment2 = (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(3);
                    refToSavedUsersFragment2.SetUpRecyclerView();

                }
                else //vec postoji dictionary
                {
                    string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {

                        Dictionary<int, Dictionary<int, List<Business>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<Business>>>>(serializedDictionary);
                        if (!dictionary.ContainsKey(userId))
                        {//ako user nije uopste dodavao usere
                            dictionary.Add(userId, new Dictionary<int, List<Business>>());
                        }
                        if (!dictionary[userId].ContainsKey(1))
                        {//ako nije dodavao private usere
                            dictionary[userId].Add(1, new List<Business>());
                        }

                        //samo dodamo private usera iz recyclerViewa
                        dictionary[userId][1].Add(user);
                        edit.Remove("SavedBusinessUsersDictionary");
                        edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment2_Business refToSavedUsersFragment = (Fragment2_Business)StartPageActivity.adapter.GetItem(2);
                        refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                        Fragment2_BusinessSavedUsers refToSavedUsersFragment2 = (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(3);
                        refToSavedUsersFragment2.SetUpRecyclerView();

                    }

                }

                fabSaveUser.SetImageResource(Resource.Drawable.ic_done);
                isSaved = true;
            }

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