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
    [Activity(Label ="UserDetailActivity", Theme = "@style/Theme.DesignDemo")]
    public class UserDetailActivity : AppCompatActivity
    {
        private UserInfoModel user;
        private ImageView imageView;

        private TextView firstNameWrapper;
        private TextView lastNameWrapper;
        private TextView emailWrapper;
        private TextView adressWrapper;
        private TextView phoneWrapper;
        private TextView dateOfBirthWrapper;

        private CardView firstNameCardView;
        private CardView lastNameCardView;
        private CardView emailCardView;
        private CardView adressCardView;
        private CardView phoneCardView;
        private CardView dateOfBirthCardView;

        private ImageView fbLogo;
        private ImageView twLogo;
        private ImageView gpLogo;
        private ImageView liLogo;

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

            user = Newtonsoft.Json.JsonConvert.DeserializeObject<UserInfoModel>(Intent.GetStringExtra("userInformation"));
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

            firstNameWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailFirstname);
            lastNameWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailLastname);
            emailWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailEmail);
            adressWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailAdress);
            phoneWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailPhoneNumber);
            dateOfBirthWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailDateOfBirth);

            firstNameCardView = FindViewById<CardView>(Resource.Id.cardViewFirstName);
            lastNameCardView = FindViewById<CardView>(Resource.Id.cardViewLastName);
            emailCardView = FindViewById<CardView>(Resource.Id.cardViewEmail);
            phoneCardView = FindViewById<CardView>(Resource.Id.cardViewPhoneNumber);
            adressCardView = FindViewById<CardView>(Resource.Id.cardViewAdress);
            dateOfBirthCardView = FindViewById<CardView>(Resource.Id.cardViewDateOfBirth);

            fbLogo = FindViewById<ImageView>(Resource.Id.facebookLinkLogo);
            twLogo = FindViewById<ImageView>(Resource.Id.twitterLinkLogo);
            gpLogo = FindViewById<ImageView>(Resource.Id.gPlusLinkLogo);
            liLogo = FindViewById<ImageView>(Resource.Id.linkedInLinkLogo);

            SetOnClickListenersAndFixUrls();

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
            if (user.FirstName != null && !user.FirstName.Equals(""))
                firstNameWrapper.Text = user.FirstName;
            else
                firstNameCardView.Visibility = ViewStates.Gone;

            if (user.LastName != null && !user.LastName.Equals(""))
                lastNameWrapper.Text = user.LastName;
            else
                lastNameCardView.Visibility = ViewStates.Gone;

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

            if (user.DateOfBirth != null && !user.DateOfBirth.Equals(""))
                dateOfBirthWrapper.Text = user.DateOfBirth;
            else
                dateOfBirthCardView.Visibility = ViewStates.Gone;
        }

        private void SetOnClickListenersAndFixUrls()
        {
            if (user.FbLink != null && !user.FbLink.Equals(""))
            {
                if (!user.FbLink.Contains("https://"))
                {
                    user.FbLink = "https://" + user.FbLink;
                }

                fbLogo.Click += FbLogo_Click;
            }
            else
            {
                fbLogo.Visibility = ViewStates.Gone;
            }

            if (user.TwitterLink != null && !user.TwitterLink.Equals(""))
            {
                if (!user.TwitterLink.Contains("https://"))
                {
                    user.TwitterLink = "https://" + user.TwitterLink;
                }

                twLogo.Click += TwLogo_Click;
            }
            else
            {
                twLogo.Visibility = ViewStates.Gone;
            }

            if (user.GPlusLink != null && !user.GPlusLink.Equals(""))
            {
                if (!user.GPlusLink.Contains("https://"))
                {
                    user.GPlusLink = "https://" + user.GPlusLink;
                }
                gpLogo.Click += GpLogo_Click;
            }
            else
            {
                gpLogo.Visibility = ViewStates.Gone;
            }

            if (user.LinkedInLink != null && !user.LinkedInLink.Equals(""))
            {
                if (!user.LinkedInLink.Contains("https://") )
                {
                    user.LinkedInLink = "https://" + user.LinkedInLink;
                }
                liLogo.Click += LiLogo_Click;
            }
            else
            {
                liLogo.Visibility = ViewStates.Gone;
            }
        }

        private void LiLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(user.LinkedInLink);
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void GpLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(user.GPlusLink);
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void TwLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(user.TwitterLink);
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void FbLogo_Click(object sender, EventArgs e)
        {
                var uri = Android.Net.Uri.Parse(user.FbLink);
                var intent = new Intent(Intent.ActionView, uri);
                StartActivity(intent);
        }

        private void FabSaveUser_Click(object sender, EventArgs e)
        {
            Guid userId = StartPageActivity.user.Id;

            ISharedPreferences pref = ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
            ISharedPreferencesEditor edit = pref.Edit();

            if (isSaved)
            {
                string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                if (serializedDictionary != String.Empty)
                {
                    var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                        <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                    dictionary[userId][1].RemoveAll(a => a.Id == user.Id);
                    edit.Remove("SavedPrivateUsersDictionary");
                    edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();


                    Fragment1_Private refToSavedUsersFragment = (Fragment1_Private)StartPageActivity.adapter.GetItem(0);
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment1_PrivateSavedUsers refToSavedUsersFragment2 = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                    refToSavedUsersFragment2.SetUpRecyclerView();
                }

                fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
                isSaved = false;
            } else
            {
                if (!pref.Contains("SavedPrivateUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                {
                    // Snackbar.Make((View)sender, Android.Text.Html.FromHtml("<font color=\"#ffffff\">does not contain</font>"), Snackbar.LengthLong).Show();

                    var dictionary = new Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>();

                    dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                    dictionary[userId].Add(1, new List<UserInfoModel>());// 1 je private mode
                    dictionary[userId][1].Add(user);

                    edit.Remove("SavedPrivateUsersDictionary");
                    edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();
                    Fragment1_Private refToSavedUsersFragment = (Fragment1_Private)StartPageActivity.adapter.GetItem(0);
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment1_PrivateSavedUsers refToSavedUsersFragment2 = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                    refToSavedUsersFragment2.SetUpRecyclerView();

                }
                else //vec postoji dictionary
                {
                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {

                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        if (!dictionary.ContainsKey(userId))
                        {//ako user nije uopste dodavao usere
                            dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                        }
                        if (!dictionary[userId].ContainsKey(1))
                        {//ako nije dodavao private usere
                            dictionary[userId].Add(1, new List<UserInfoModel>());
                        }

                        //samo dodamo private usera iz recyclerViewa
                        dictionary[userId][1].Add(user);
                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_Private refToSavedUsersFragment = (Fragment1_Private)StartPageActivity.adapter.GetItem(0);
                        refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment2 = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
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
