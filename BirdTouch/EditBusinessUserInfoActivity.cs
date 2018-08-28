using System;
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
using Android.Text;
using System.Net;
using System.Collections.Specialized;
using Android.Graphics;
using System.IO;
using BirdTouch.Helpers;
using BirdTouch.Constants;
using Newtonsoft.Json;

namespace BirdTouch
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

        private void FabInsertPhoto_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            this.StartActivityForResult(Intent.CreateChooser(intent, "Select a Photo"), 0);
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
            }
        }

        private void ImageView_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            this.StartActivityForResult(Intent.CreateChooser(intent, "Select a Photo"), 0);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data) //kada se dobije iz galerije nazad neki podatak
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == Result.Ok)
            {
                System.IO.Stream stream = ContentResolver.OpenInputStream(data.Data);
                //  imageView.SetImageBitmap(BitmapFactory.DecodeStream(stream)); neefikasan nacin ucitavanja slika, nema skaliranja
                _imageView.SetImageBitmap(DecodeBitmapFromStream(data.Data, 400, 300)); //mozda su prevelike dimenzije, moze da se podesi
                pictureChanged = true;
            }
        }

        private Bitmap DecodeBitmapFromStream(Android.Net.Uri data, int requestedWidth, int requestedHeight)
        {
            //Decode with inJustDecodeBounds = true to check dimensions
            //proveravamo samo velicinu slike, da nije neka prevelika slika koja bi napunila memoriju
            Stream stream = ContentResolver.OpenInputStream(data);
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InJustDecodeBounds = true;
            BitmapFactory.DecodeStream(stream, null, options);

            int imageHeight = options.OutHeight;
            int imageWidth = options.OutWidth;


            //Calculate InSampleSize
            options.InSampleSize = CalculateInSampleSize(options, requestedWidth, requestedHeight);

            //Decode bitmap with InSampleSize set
            stream = ContentResolver.OpenInputStream(data); //must read again
            options.InJustDecodeBounds = false;
            Bitmap bitmap = BitmapFactory.DecodeStream(stream, null, options);

            return bitmap;
        }
        private int CalculateInSampleSize(BitmapFactory.Options options, int requestedWidth, int requestedHeight)
        {
            //Raw height and width of image
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > requestedHeight || width > requestedWidth)
            {
                //slika je veca nego sto nam treba
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while ((halfHeight / inSampleSize) >= requestedHeight && (halfWidth / inSampleSize) >= requestedWidth)
                {
                    inSampleSize *= 2;
                }
            }
            Console.WriteLine();
            Console.WriteLine("SampleSizeBitmap: " + inSampleSize.ToString());
            Console.WriteLine();
            return inSampleSize;
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