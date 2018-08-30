using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using BirdTouch.Constants;
using BirdTouch.Helpers;
using BirdTouch.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;

namespace BirdTouch.Activities
{
    [Activity(Label = "EditUserInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditPrivateUserInfoActivity : AppCompatActivity
    {
        private UserInfoModel _user;
        private bool pictureChanged = false;

        private ImageView _imageView;
        private TextInputLayout _firstNameWrapper;
        private TextInputLayout _lastNameWrapper;
        private TextInputLayout _emailWrapper;
        private TextInputLayout _descriptionWrapper;
        private TextInputLayout _adressWrapper;
        private TextInputLayout _phoneWrapper;
        private TextInputLayout _dateOfBirthWrapper;
        private TextInputLayout _facebookLinkWrapper;
        private TextInputLayout _twitterLinkWrapper;
        private TextInputLayout _gPlusLinkWrapper;
        private TextInputLayout _linkedInLinkWrapper;
        private CollapsingToolbarLayout _collapsingToolBar;
        private FloatingActionButton _fabSaveChanges;
        private FloatingActionButton _fabInsertPhoto;
        private WebClient _webClient;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditPrivateUserInfo);

            // Deserialize user info recevied from StartPageActivity
            _user = Newtonsoft.Json.JsonConvert.DeserializeObject
                <UserInfoModel>(Intent.GetStringExtra(IntentConstants.LOGGED_IN_USER));

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>
                (Resource.Id.toolbar_edit_private_info);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = string.Empty;

            // Setting image
            _imageView = FindViewById<ImageView>(Resource.Id.profile_picture_edit_private_info);

            if (_user.ProfilePictureData != null)
            {
                _imageView.SetImageBitmap(
                    BitmapFactory.DecodeByteArrayAsync(
                        _user.ProfilePictureData,
                        0,
                        _user.ProfilePictureData.Length)
                         .Result);
            }
            else
            {   // The default image when user has not saved any profile image
                _imageView.SetImageResource(Resource.Drawable.blank_user_profile);
            }

            // Find all components
            _collapsingToolBar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar_edit_private_info);
            _firstNameWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserFirstNameWrapper);
            _lastNameWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserLastNameWrapper);
            _emailWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserEmailWrapper);
            _descriptionWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserDescriptionWrapper);
            _adressWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserAdressWrapper);
            _phoneWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserPhoneWrapper);
            _dateOfBirthWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserDateOfBirthWrapper);
            _facebookLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserFacebookWrapper);
            _twitterLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserTwitterWrapper);
            _gPlusLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserGPlusWrapper);
            _linkedInLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserLinkedInWrapper);
            _fabSaveChanges = FindViewById<FloatingActionButton>(Resource.Id.fabEditPrivateUserInfoSaveChanges);
            _fabInsertPhoto = FindViewById<FloatingActionButton>(Resource.Id.fabEditPrivateUserInfoInsertPhoto);

            // Fill components with values from logged in user
            _firstNameWrapper.EditText.Text = _user.FirstName;
            _lastNameWrapper.EditText.Text = _user.LastName;
            _emailWrapper.EditText.Text = _user.Email;
            _descriptionWrapper.EditText.Text = _user.Description;
            _adressWrapper.EditText.Text = _user.Adress;
            _phoneWrapper.EditText.Text = _user.PhoneNumber;
            _dateOfBirthWrapper.EditText.Text = _user.DateOfBirth;
            _facebookLinkWrapper.EditText.Text = WebUtility.UrlDecode(_user.FbLink);
            _twitterLinkWrapper.EditText.Text = WebUtility.UrlDecode(_user.TwitterLink);
            _gPlusLinkWrapper.EditText.Text = WebUtility.UrlDecode(_user.GPlusLink);
            _linkedInLinkWrapper.EditText.Text = WebUtility.UrlDecode(_user.LinkedInLink);
            _collapsingToolBar.Title = string.Empty;

            // Initialize web clients
            _webClient = new WebClient();

            // Set up events for web clients
            _webClient.UploadStringCompleted += WebClient_UploadStringCompleted;

            // Set OnClick events
            _imageView.Click += ImageView_Click;
            _fabInsertPhoto.Click += FabInsertPhoto_Click;
            _fabSaveChanges.Click += FabSaveChanges_Click;
        }

        private void FabSaveChanges_Click(object sender, EventArgs e)
        {
            View view = sender as View;

            if (Reachability.IsOnline(this) && !_webClient.IsBusy)
            {
                //get ImageView (profileImage) as  array of bytes
                _imageView.BuildDrawingCache(true);
                Bitmap bitmap = _imageView.GetDrawingCache(true);
                MemoryStream memStream = new MemoryStream();

                //TODO: Decide if jpeg is best
                // max img size je 61kB
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, memStream);
                byte[] picData = memStream.ToArray();
                _imageView.DestroyDrawingCache();

                var userInfoForUpload = new UserInfoModel()
                {
                    Id = _user.Id,
                    Username = _user.Username,
                    FirstName = _firstNameWrapper.EditText.Text,
                    LastName = _lastNameWrapper.EditText.Text,
                    Email = _emailWrapper.EditText.Text,
                    Description = _descriptionWrapper.EditText.Text,
                    PhoneNumber = _phoneWrapper.EditText.Text,
                    Adress = _adressWrapper.EditText.Text,
                    DateOfBirth = _dateOfBirthWrapper.EditText.Text,
                    FbLink = WebUtility.UrlEncode(_facebookLinkWrapper.EditText.Text),
                    TwitterLink = WebUtility.UrlEncode(_twitterLinkWrapper.EditText.Text),
                    GPlusLink = WebUtility.UrlEncode(_gPlusLinkWrapper.EditText.Text),
                    LinkedInLink = WebUtility.UrlEncode(_linkedInLinkWrapper.EditText.Text),
                };

                if (pictureChanged)
                {
                    userInfoForUpload.ProfilePictureData = picData;
                    StartPageActivity.picDataProfileNavigation = picData;
                }
                else
                {
                    userInfoForUpload.ProfilePictureData = null;
                }

                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_changePrivateUser);

                _webClient.Headers.Clear();
                _webClient.Headers.Add(
                    HttpRequestHeader.ContentType,
                    "application/json");
                _webClient.Headers.Add(
                   HttpRequestHeader.Authorization,
                   "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(ApplicationContext));

                _webClient.UploadStringAsync(uri, "PATCH", JsonConvert.SerializeObject(userInfoForUpload));
            }
            else
            {
                Snackbar.Make(
                    view,
                    Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
        }

        private void WebClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add type of error
                Snackbar.Make(
                    _fabSaveChanges,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                Snackbar.Make(
                    _fabSaveChanges,
                    Html.FromHtml("<font color=\"#ffffff\">Changes saved successfully</font>"),
                    Snackbar.LengthLong)
                     .Show();

                // Update title in action bar with new Firstname and Lastname
                StartPageActivity.actionBar.Title =
                    _firstNameWrapper.EditText.Text + " " + _lastNameWrapper.EditText.Text;

                // Update image in navigation menu with newly set image
                if (pictureChanged)
                {
                    StartPageActivity.UpdateProfileImage();
                }
            }
        }

        private void FabInsertPhoto_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            this.StartActivityForResult(Intent.CreateChooser(intent, "Select a Photo"), 0);
        }

        private void ImageView_Click(object sender, EventArgs e)
        {
            FabInsertPhoto_Click(sender, e);
        }

        // When we get some result from the gallery
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(resultCode == Result.Ok)
            {
                var stream = ContentResolver.OpenInputStream(data.Data);

                // TODO: Maybe change dimensions
                _imageView.SetImageBitmap(BitmapHelper.DecodeBitmapFromStream(
                                            ContentResolver,
                                            data.Data,
                                            400,
                                            300));
                pictureChanged = true;
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
