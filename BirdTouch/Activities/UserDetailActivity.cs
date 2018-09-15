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
using System.Net;
using Newtonsoft.Json;
using BirdTouch.Constants;

namespace BirdTouch.Activities
{
    [Activity(Label = "UserDetailActivity", Theme = "@style/Theme.DesignDemo")]
    public class UserDetailActivity : AppCompatActivity
    {
        private UserInfoModel _user;
        private ImageView _imageView;

        private TextView _firstNameWrapper;
        private TextView _lastNameWrapper;
        private TextView _descriptionWrapper;
        private TextView _emailWrapper;
        private TextView _adressWrapper;
        private TextView _phoneWrapper;
        private TextView _dateOfBirthWrapper;

        private CardView _firstNameCardView;
        private CardView _lastNameCardView;
        private CardView _descriptionCardView;
        private CardView _emailCardView;
        private CardView _adressCardView;
        private CardView _phoneCardView;
        private CardView _dateOfBirthCardView;

        private ImageView _fbLogo;
        private ImageView _twLogo;
        private ImageView _gpLogo;
        private ImageView _liLogo;

        private FloatingActionButton _fabSaveUser;

        private bool _isSaved;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.UserDetailActivity);

            // Deserialize user info recevied from fragment
            _user = JsonConvert.DeserializeObject<UserInfoModel>(Intent.GetStringExtra("userInformation"));
            _isSaved = Intent.GetBooleanExtra("isSaved", false);

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_private_userinfo_show_detail);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = string.Empty;

            // Setting image
            _imageView = FindViewById<ImageView>(Resource.Id.profile_picture_private_userinfo_show_detail);
            if (_user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(
                    _user.ProfilePictureData,
                    0,
                    _user.ProfilePictureData.Length)
                     .Result;
                _imageView.SetImageBitmap(bm);
            }
            else
            { // The default image when user has not saved any profile image
                _imageView.SetImageResource(Resource.Drawable.blank_navigation);
            }

            // Find all components
            _fabSaveUser = FindViewById<FloatingActionButton>(Resource.Id.fabPrivateUserInfoSaveUser);
            _firstNameWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailFirstname);
            _lastNameWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailLastname);
            _descriptionWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailDescription);
            _emailWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailEmail);
            _adressWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailAdress);
            _phoneWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailPhoneNumber);
            _dateOfBirthWrapper = FindViewById<TextView>(Resource.Id.textViewPrivateUserShowDetailDateOfBirth);
            _firstNameCardView = FindViewById<CardView>(Resource.Id.cardViewFirstName);
            _lastNameCardView = FindViewById<CardView>(Resource.Id.cardViewLastName);
            _descriptionCardView = FindViewById<CardView>(Resource.Id.cardViewDescription);
            _emailCardView = FindViewById<CardView>(Resource.Id.cardViewEmail);
            _phoneCardView = FindViewById<CardView>(Resource.Id.cardViewPhoneNumber);
            _adressCardView = FindViewById<CardView>(Resource.Id.cardViewAdress);
            _dateOfBirthCardView = FindViewById<CardView>(Resource.Id.cardViewDateOfBirth);
            _fbLogo = FindViewById<ImageView>(Resource.Id.facebookLinkLogo);
            _twLogo = FindViewById<ImageView>(Resource.Id.twitterLinkLogo);
            _gpLogo = FindViewById<ImageView>(Resource.Id.gPlusLinkLogo);
            _liLogo = FindViewById<ImageView>(Resource.Id.linkedInLinkLogo);

            // Set OnClick events
            SetUrlsOnClickListenersAndFixLinks();
            _fabSaveUser.Click += FabSaveUser_Click;

            // Fill components with values from logged in user
            FillTextviewsWithData();

            if (_isSaved)
            {
                _fabSaveUser.SetImageResource(Resource.Drawable.ic_done);
            }
            else
            {
                _fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
            }
        }

        private void FillTextviewsWithData()
        {
            if (!string.IsNullOrEmpty(_user.FirstName))
            {
                _firstNameWrapper.Text = _user.FirstName;
            }
            else
            {
                _firstNameCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.LastName))
            {
                _lastNameWrapper.Text = _user.LastName;
            }
            else
            {
                _lastNameCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.Description))
            {
                _descriptionWrapper.Text = _user.Description;
            }
            else
            {
                _descriptionCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.Email))
            {
                _emailWrapper.Text = _user.Email;
            }
            else
            {
                _emailCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.Adress))
            {
                _adressWrapper.Text = _user.Adress;
            }
            else
            {
                _adressCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.PhoneNumber))
            {
                _phoneWrapper.Text = _user.PhoneNumber;
            }
            else
            {
                _phoneCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.DateOfBirth))
            {
                _dateOfBirthWrapper.Text = _user.DateOfBirth;
            }
            else
            {
                _dateOfBirthCardView.Visibility = ViewStates.Gone;
            }
        }

        private void SetUrlsOnClickListenersAndFixLinks()
        {
            if (!string.IsNullOrEmpty(_user.FbLink))
            {
                if (!_user.FbLink.Contains("https://"))
                {
                    _user.FbLink = "https://" + WebUtility.UrlDecode(_user.FbLink);
                }

                _fbLogo.Click += FbLogo_Click;
            }
            else
            {
                _fbLogo.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.TwitterLink))
            {
                if (!_user.TwitterLink.Contains("https://"))
                {
                    _user.TwitterLink = "https://" + WebUtility.UrlDecode(_user.TwitterLink);
                }

                _twLogo.Click += TwLogo_Click;
            }
            else
            {
                _twLogo.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.GPlusLink))
            {
                if (!_user.GPlusLink.Contains("https://"))
                {
                    _user.GPlusLink = "https://" + WebUtility.UrlDecode(_user.GPlusLink);
                }
                _gpLogo.Click += GpLogo_Click;
            }
            else
            {
                _gpLogo.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.LinkedInLink))
            {
                if (!_user.LinkedInLink.Contains("https://"))
                {
                    _user.LinkedInLink = "https://" + WebUtility.UrlDecode(_user.LinkedInLink);
                }
                _liLogo.Click += LiLogo_Click;
            }
            else
            {
                _liLogo.Visibility = ViewStates.Gone;
            }
        }

        private void LiLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(WebUtility.UrlDecode(_user.LinkedInLink));
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void GpLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(WebUtility.UrlDecode(_user.GPlusLink));
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void TwLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(WebUtility.UrlDecode(_user.TwitterLink));
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void FbLogo_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse(WebUtility.UrlDecode(_user.FbLink));
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void FabSaveUser_Click(object sender, EventArgs e)
        {
            Guid userId = StartPageActivity.user.Id;

            ISharedPreferences pref =
                ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);

            ISharedPreferencesEditor edit = pref.Edit();

            if (_isSaved)
            {
                string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                if (serializedDictionary != String.Empty)
                {
                    var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                        <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                    dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)].RemoveAll(a => a.Id == _user.Id);
                    edit.Remove("SavedPrivateUsersDictionary");
                    edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();

                    Fragment1_Private refToSavedUsersFragment =
                        (Fragment1_Private)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.PRIVATE));
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();

                    Fragment1_PrivateSavedUsers refToSavedUsersFragment2 =
                        (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDPRIVATE));
                    refToSavedUsersFragment2.SetUpRecyclerView();
                }

                _fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
                _isSaved = false;
            }
            else
            {
                if (!pref.Contains("SavedPrivateUsersDictionary"))
                {
                    var dictionary = new Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>();

                    dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                    dictionary[userId].Add(int.Parse(ActiveModeConstants.PRIVATE), new List<UserInfoModel>());
                    dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)].Add(_user);

                    edit.Remove("SavedPrivateUsersDictionary");
                    edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();
                    Fragment1_Private refToSavedUsersFragment =
                        (Fragment1_Private)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.PRIVATE));
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment1_PrivateSavedUsers refToSavedUsersFragment2 =
                        (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDPRIVATE));
                    refToSavedUsersFragment2.SetUpRecyclerView();

                }
                else
                {
                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {

                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        if (!dictionary.ContainsKey(userId))
                        {
                            dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                        }
                        if (!dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.PRIVATE)))
                        {
                            dictionary[userId].Add(int.Parse(ActiveModeConstants.PRIVATE), new List<UserInfoModel>());
                        }

                        dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)].Add(_user);
                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_Private refToSavedUsersFragment =
                            (Fragment1_Private)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.PRIVATE));
                        refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment2 =
                            (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDPRIVATE));
                        refToSavedUsersFragment2.SetUpRecyclerView();
                    }
                }

                _fabSaveUser.SetImageResource(Resource.Drawable.ic_done);
                _isSaved = true;
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
