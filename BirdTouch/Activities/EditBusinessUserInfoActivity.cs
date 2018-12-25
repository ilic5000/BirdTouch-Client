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
using Com.Yalantis.Ucrop;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;

namespace BirdTouch.Activities
{
    [Activity(Label = "EditBusinessInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditBusinessUserInfoActivity : AppCompatActivity
    {
        private BusinessInfoModel _business;
        private ImageView _imageView;
        private TextInputLayout _companyNameWrapper;
        private TextInputLayout _emailWrapper;
        private TextInputLayout _descriptionWrapper;
        private TextInputLayout _phoneWrapper;
        private TextInputLayout _websiteWrapper;
        private TextInputLayout _adressWrapper;
        private CollapsingToolbarLayout _collapsingToolBar;
        private FloatingActionButton _fabSaveChanges;
        private FloatingActionButton _fabInsertPhoto;
        private WebClient _webClient;
        private bool pictureChanged = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditBusinessUserInfo);

            // Deserialize business info recevied from StartPageActivity
            _business = Newtonsoft.Json.JsonConvert.DeserializeObject<BusinessInfoModel>
                (Intent.GetStringExtra(IntentConstants.LOGGED_IN_BUSINESS_USER));

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>
                (Resource.Id.toolbar_edit_business_info);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = string.Empty;

            // Setting image
            _imageView = FindViewById<ImageView>(Resource.Id.profile_picture_edit_business_info);

            if (_business.ProfilePictureData != null)
            {
                _imageView.SetImageBitmap(
                    BitmapFactory.DecodeByteArrayAsync(
                        _business.ProfilePictureData,
                        0, _business.ProfilePictureData.Length)
                         .Result);
            }
            else
            {   // The default image when user has not saved any business profile image
                _imageView.SetImageResource(Resource.Drawable.blank_business_profile);
            }

            // Find all components
            _collapsingToolBar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar_edit_business_info);
            _companyNameWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessCompanyNameWrapper);
            _emailWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessEmailWrapper);
            _descriptionWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessDescriptionWrapper);
            _adressWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessAdressWrapper);
            _phoneWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessPhoneWrapper);
            _websiteWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessWebsiteWrapper);

            // Fill components with values from logged in business user
            _companyNameWrapper.EditText.Text = _business.CompanyName;
            _emailWrapper.EditText.Text = _business.Email;
            _descriptionWrapper.EditText.Text = _business.Description;
            _adressWrapper.EditText.Text = _business.Adress;
            _phoneWrapper.EditText.Text = _business.PhoneNumber;
            _websiteWrapper.EditText.Text = WebUtility.UrlDecode(_business.Website);
            _fabSaveChanges = FindViewById<FloatingActionButton>(Resource.Id.fabEditBusinessUserInfoSaveChanges);
            _fabInsertPhoto = FindViewById<FloatingActionButton>(Resource.Id.fabEditBusinessUserInfoInsertPhoto);
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
                //get ImageView (businessProfileImage) as  array of bytes
                _imageView.BuildDrawingCache(true);
                Bitmap bitmap = _imageView.GetDrawingCache(true);
                MemoryStream memStream = new MemoryStream();

                //TODO: Decide if jpeg is best
                // max img size je 61kB
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, memStream);
                byte[] picData = memStream.ToArray();
                _imageView.DestroyDrawingCache();

                var businessInfoForUpload = new BusinessInfoModel()
                {
                    Id = _business.Id,
                    Adress = _adressWrapper.EditText.Text,
                    CompanyName = _companyNameWrapper.EditText.Text,
                    Description = _descriptionWrapper.EditText.Text,
                    Email = _emailWrapper.EditText.Text,
                    PhoneNumber = _phoneWrapper.EditText.Text,
                    Website = WebUtility.UrlEncode(_websiteWrapper.EditText.Text)
                };

                if (pictureChanged)
                {
                    businessInfoForUpload.ProfilePictureData = picData;
                }
                else
                {
                    businessInfoForUpload.ProfilePictureData = null;
                }

                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_changeBusinessUser);

                _webClient.Headers.Clear();
                _webClient.Headers.Add(
                    HttpRequestHeader.ContentType,
                    "application/json");
                _webClient.Headers.Add(
                   HttpRequestHeader.Authorization,
                   "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(ApplicationContext));

                _webClient.UploadStringAsync(uri, "PATCH", JsonConvert.SerializeObject(businessInfoForUpload));
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
                    StartPageActivity.profilePictureNavigationHeader,
                    Html.FromHtml("<font color=\"#ffffff\">Changes saved successfully</font>"),
                    Snackbar.LengthLong)
                     .Show();
                Finish();
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

            switch (resultCode)
            {
                case Result.Ok:
                    switch (requestCode)
                    {
                        case 0:
                            var stream = ContentResolver.OpenInputStream(data.Data);

                            var cropOptions = new UCrop.Options();
                            cropOptions.SetShowCropGrid(false);
                            cropOptions.SetShowCropFrame(true);
                            cropOptions.SetHideBottomControls(true);

                            cropOptions.SetToolbarTitle("Please crop your business card");

                            UCrop.Of(data.Data, Android.Net.Uri.FromFile(new Java.IO.File(CacheDir, Guid.NewGuid().ToString())))
                             .WithOptions(cropOptions)
                             .WithAspectRatio(16, 9)
                                .WithMaxResultSize(600, 600)
                                .Start(this);
                            break;

                        case UCrop.RequestCrop:
                            // TODO: Maybe change dimensions
                            var resultCropUri = UCrop.GetOutput(data);

                            _imageView.SetImageBitmap(BitmapHelper.DecodeBitmapFromStream(
                                                        ContentResolver,
                                                        resultCropUri,
                                                        400,
                                                        400));
                            pictureChanged = true;
                            break;

                        default:
                            Snackbar.Make(this._imageView,
                                          Html.FromHtml("<font color=\"#ffffff\">Unexpected result.</font>"),
                                           Snackbar.LengthLong).Show();
                            break;
                    }
                    break;

                default:
                    break;
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