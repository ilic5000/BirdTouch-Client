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
    [Activity(Label = "BusinessDetailActivity", Theme = "@style/Theme.DesignDemo")]
    public class BusinessDetailActivity : AppCompatActivity
    {
        private BusinessInfoModel _user;
        private ImageView _imageView;

        private TextView _companyNameWrapper;
        private TextView _descriptionWrapper;
        private TextView _emailWrapper;
        private TextView _adressWrapper;
        private TextView _phoneWrapper;
        private TextView _websiteWrapper;

        private CardView _companyNameCardView;
        private CardView _descriptionCardView;
        private CardView _emailCardView;
        private CardView _adressCardView;
        private CardView _phoneCardView;
        private CardView _websiteCardView;

        private FloatingActionButton _fabSaveUser;

        private bool _isSaved;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.BusinessDetailActivity);

            // Deserialize user info recevied from fragment
            _user = JsonConvert.DeserializeObject<BusinessInfoModel>(Intent.GetStringExtra("userInformation"));
            _isSaved = Intent.GetBooleanExtra("isSaved", false);

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_business_userinfo_show_detail);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = string.Empty;

            // Setting image
            _imageView = FindViewById<ImageView>(Resource.Id.profile_picture_business_userinfo_show_detail);
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
                _imageView.SetImageResource(Resource.Drawable.blank_business);
            }

            // Find all components
            _fabSaveUser = FindViewById<FloatingActionButton>(Resource.Id.fabBusinessUserInfoSaveUser);
            _companyNameWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessCompanyNameShowDetail);
            _descriptionWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessDescription);
            _websiteWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessWebsiteShowDetail);
            _emailWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessEmailShowDetail);
            _adressWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessAdressShowDetail);
            _phoneWrapper = FindViewById<TextView>(Resource.Id.textViewBusinessPhoneNumberShowDetail);
            _companyNameCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessCompanyName);
            _descriptionCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessDescription);
            _emailCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessEmail);
            _phoneCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessPhoneNumber);
            _adressCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessAdress);
            _websiteCardView = FindViewById<CardView>(Resource.Id.cardViewBusinessWebsite);

            // Fill components with values from logged in user
            FillDataAndSetOnClickEvents();

            // Set OnClick events
            _fabSaveUser.Click += FabSaveUser_Click;

            if (_isSaved)
            {
                _fabSaveUser.SetImageResource(Resource.Drawable.ic_done);
            }
            else
            {
                _fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
            }
        }

        private void FillDataAndSetOnClickEvents()
        {
            if (!string.IsNullOrEmpty(_user.CompanyName))
            {
                _companyNameWrapper.Text = _user.CompanyName;
            }
            else
            {
                _companyNameCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.Description))
            {
                _descriptionWrapper.Text = _user.Description;
            }
            else
            {
                _descriptionCardView.Visibility = ViewStates.Gone;
            }

            if (!string.IsNullOrEmpty(_user.Website))
            {
                _websiteWrapper.Text = WebUtility.UrlDecode(_user.Website);
                _websiteCardView.Click += WebsiteCardView_Click;
            }
            else
            {
                _websiteCardView.Visibility = ViewStates.Gone;
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
        }

        private void WebsiteCardView_Click(object sender, EventArgs e)
        {
            if (!_user.Website.Contains("http"))
            {
                _user.Website = "http://" + WebUtility.UrlDecode(_user.Website);
            }

            var uri = Android.Net.Uri.Parse(WebUtility.UrlDecode(_user.Website));
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
                string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                if (serializedDictionary != String.Empty)
                {
                    var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                        <Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>>(serializedDictionary);

                    dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)].RemoveAll(a => a.FkUserId == _user.FkUserId);
                    edit.Remove("SavedBusinessUsersDictionary");
                    edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();


                    Fragment2_Business refToSavedUsersFragment = (Fragment2_Business)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.BUSINESS));
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment2_BusinessSavedUsers refToSavedUsersFragment2 =
                        (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDBUSINESS));
                    refToSavedUsersFragment2.SetUpRecyclerView();
                }

                _fabSaveUser.SetImageResource(Resource.Drawable.ic_save_white_24dp);
                _isSaved = false;
            }
            else
            {
                if (!pref.Contains("SavedBusinessUsersDictionary"))
                {
                    var dictionary = new Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>();

                    dictionary.Add(userId, new Dictionary<int, List<BusinessInfoModel>>());
                    dictionary[userId].Add(int.Parse(ActiveModeConstants.BUSINESS), new List<BusinessInfoModel>());
                    dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)].Add(_user);

                    edit.Remove("SavedBusinessUsersDictionary");
                    edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                    edit.Apply();
                    Fragment2_Business refToSavedUsersFragment = (Fragment2_Business)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.BUSINESS));
                    refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    Fragment2_BusinessSavedUsers refToSavedUsersFragment2 =
                        (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDBUSINESS));
                    refToSavedUsersFragment2.SetUpRecyclerView();

                }
                else //vec postoji dictionary
                {
                    string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>>(serializedDictionary);

                        if (!dictionary.ContainsKey(userId))
                        {//ako user nije uopste dodavao usere
                            dictionary.Add(userId, new Dictionary<int, List<BusinessInfoModel>>());
                        }
                        if (!dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.BUSINESS)))
                        {//ako nije dodavao private usere
                            dictionary[userId].Add(int.Parse(ActiveModeConstants.BUSINESS), new List<BusinessInfoModel>());
                        }

                        //samo dodamo private usera iz recyclerViewa
                        dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)].Add(_user);
                        edit.Remove("SavedBusinessUsersDictionary");
                        edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment2_Business refToSavedUsersFragment = (Fragment2_Business)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.BUSINESS));
                        refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                        Fragment2_BusinessSavedUsers refToSavedUsersFragment2 =
                            (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDBUSINESS));
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