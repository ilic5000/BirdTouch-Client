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
using Android.Support.V7.Widget;

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
        private TextView websiteWrapper;

        private CardView companyNameCardView;      
        private CardView emailCardView;
        private CardView adressCardView;
        private CardView phoneCardView;
        private CardView websiteCardView;

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
                imageView.SetImageResource(Resource.Drawable.blank_business);
            }



            companyNameWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessCompanyNameShowDetail);
            websiteWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessWebsiteShowDetail);
            emailWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessEmailShowDetail);
            adressWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessAdressShowDetail);
            phoneWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessPhoneNumberShowDetail);

            companyNameCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessCompanyName);          
            emailCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessEmail);
            phoneCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessPhoneNumber);
            adressCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessAdress);
            websiteCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessWebsite);

            FillDataIfThereIsSomethingToFillWith();

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

        private void FillDataIfThereIsSomethingToFillWith()
        {
            if (user.CompanyName != null && !user.CompanyName.Equals(""))
                companyNameWrapper.Text = user.CompanyName;
            else
                companyNameCardView.Visibility = ViewStates.Gone;

            if (user.Website != null && !user.Website.Equals(""))
            {
                websiteWrapper.Text = user.Website;
                websiteCardView.Click += WebsiteCardView_Click;
            }
            else
                websiteCardView.Visibility = ViewStates.Gone;

            if (user.Email != null && !user.Email.Equals(""))
                emailWrapper.Text = user.Email;
            else
                emailCardView.Visibility = ViewStates.Gone;

            if (user.Adress != null && !user.Adress.Equals(""))
                adressWrapper.Text = user.Adress;
            else
                adressCardView.Visibility = ViewStates.Gone;

            if (user.PhoneNumber != null && !user.PhoneNumber.Equals(""))
                phoneWrapper.Text = user.PhoneNumber;
            else
                phoneCardView.Visibility = ViewStates.Gone;
        }

        private void WebsiteCardView_Click(object sender, EventArgs e)
        {
            if (!user.Website.Contains("http"))
            {
                user.Website = "http://" + user.Website;
            }

            var uri = Android.Net.Uri.Parse(user.Website);
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);

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
                    dictionary[userId].Add(1, new List<Business>());// 1 je private mode, zbog drugog dictionaryja, sada je to visak, ali mozda u buducnosti bude neko sortiranje private korisnika, za svaki slucaj
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