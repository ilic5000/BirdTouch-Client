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

namespace BirdTouch
{
    [Activity(Label = "EditBusinessInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditBusinessUserInfoActivity : AppCompatActivity

    {

        private BusinessInfoModel business;
        private ImageView imageView;
        private TextInputLayout companyNameWrapper;
        private TextInputLayout emailWrapper;
        private TextInputLayout phoneWrapper;
        private TextInputLayout websiteWrapper;
        private TextInputLayout adressWrapper;
        private CollapsingToolbarLayout collapsingToolBar;
        private FloatingActionButton fabSaveChanges;
        private FloatingActionButton fabInsertPhoto;
        private WebClient webClient;
        private Uri uri;

        private bool pictureChanged = false;
        protected override void OnCreate(Bundle savedInstanceState)
        {


            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditBusinessUserInfo);

            imageView = FindViewById<ImageView>(Resource.Id.profile_picture_edit_business_info);
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_edit_business_info); //nije isti toolbar kao u startpage

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = "";
            //popunjavanje polja iz baze
            business = Newtonsoft.Json.JsonConvert.DeserializeObject<BusinessInfoModel>
                (Intent.GetStringExtra(IntentConstants.LOGGED_IN_BUSINESS_USER));


            if (business.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(business.ProfilePictureData, 0, business.ProfilePictureData.Length).Result;
                imageView.SetImageBitmap(bm);
            }
            else
            {   //defaultni image kada korisnik jos uvek nije promenio, mada moze i u axml da se postavi
                imageView.SetImageResource(Resource.Drawable.blank_business_profile);
            }

            collapsingToolBar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar_edit_business_info);
            companyNameWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessCompanyNameWrapper);
            emailWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessEmailWrapper);
            adressWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessAdressWrapper);
            phoneWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessPhoneWrapper);
            websiteWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditBusinessWebsiteWrapper);



            companyNameWrapper.EditText.Text = business.CompanyName;
            emailWrapper.EditText.Text = business.Email;
            adressWrapper.EditText.Text = business.Adress;
            phoneWrapper.EditText.Text = business.PhoneNumber;
            websiteWrapper.EditText.Text = business.Website;

            collapsingToolBar.Title = "";

            imageView.Click += ImageView_Click;

            webClient = new WebClient();
            webClient.UploadDataCompleted += WebClient_UploadDataCompleted;

            fabSaveChanges = FindViewById<FloatingActionButton>(Resource.Id.fabEditBusinessUserInfoSaveChanges);
            fabInsertPhoto = FindViewById<FloatingActionButton>(Resource.Id.fabEditBusinessUserInfoInsertPhoto);

            fabInsertPhoto.Click += FabInsertPhoto_Click;

            fabSaveChanges.Click += (o, e) => //o is sender, sender is button, button is a view
            {
                View view = o as View;
                if (Reachability.IsOnline(this) && !webClient.IsBusy)
                {

                    //zbog parametara mora da postoje sva polja kada se salju, makar privremeno
                    checkIfEditTextsAreEmptyAndTurnThemToNULLString();


                    //get ImageView (profileImage) as  array of bytes
                    imageView.BuildDrawingCache(true);
                    Bitmap bitmap = imageView.GetDrawingCache(true);
                    MemoryStream memStream = new MemoryStream();
                    bitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, memStream); //moze i drugi format //max img size je 61kB
                    byte[] picData = memStream.ToArray();
                    //String picDataEncoded = Convert.ToBase64String(picData);
                    imageView.DestroyDrawingCache();

                    if (!pictureChanged)
                    {
                        picData = new byte[1]; //server pita, ako je duzine 1 ovaj niz, onda ne upisuje sliku jer nije bilo promene od strane korisnika
                    }

                    //insert parameters for header for web request
                    NameValueCollection parameters = new NameValueCollection();
                    parameters.Add("companyname", companyNameWrapper.EditText.Text);
                    parameters.Add("email", emailWrapper.EditText.Text);
                    parameters.Add("website", websiteWrapper.EditText.Text);
                    parameters.Add("phone", phoneWrapper.EditText.Text);
                    parameters.Add("adress", adressWrapper.EditText.Text);
                    parameters.Add("id", business.FkUserId.ToString());

                    String restUriString = GetString(Resource.String.webapi_endpoint_changeBusinessUser);
                    uri = new Uri(restUriString);

                    webClient.Headers.Clear();
                    webClient.Headers.Add(parameters);
                    webClient.UploadDataAsync(uri, picData);

                }
                else
                {

                    Snackbar.Make(view, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

                }

            };

        }


        private void FabInsertPhoto_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            this.StartActivityForResult(Intent.CreateChooser(intent, "Select a Photo"), 0);
        }

        private void WebClient_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno

                returnAllNullEditTextsToEmpty();
                Console.WriteLine("*******Error webclient data save changes error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
                returnAllNullEditTextsToEmpty();
                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">Changes saved successfully</font>"), Snackbar.LengthLong).Show();

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
                imageView.SetImageBitmap(DecodeBitmapFromStream(data.Data, 400, 300)); //mozda su prevelike dimenzije, moze da se podesi
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

        private void checkIfEditTextsAreEmptyAndTurnThemToNULLString()
        {
            //da bi moglo da postoji i prazno polje, jer mora da postoje svi parametri u rest servisu
            if (companyNameWrapper.EditText.Text.Equals("")) companyNameWrapper.EditText.Text = "NULL";
            if (emailWrapper.EditText.Text.Equals("")) emailWrapper.EditText.Text = "NULL";
            if (websiteWrapper.EditText.Text.Equals("")) websiteWrapper.EditText.Text = "NULL";
            if (adressWrapper.EditText.Text.Equals("")) adressWrapper.EditText.Text = "NULL";
            if (phoneWrapper.EditText.Text.Equals("")) phoneWrapper.EditText.Text = "NULL";

        }

        private void returnAllNullEditTextsToEmpty()
        {
            //da bi moglo da postoji i prazno polje, jer mora da postoje svi parametri u rest servisu
            //sada vracamo na prazno ako je prosledjeno NULL kao parametar
            if (companyNameWrapper.EditText.Text.Equals("NULL")) companyNameWrapper.EditText.Text = "";
            if (emailWrapper.EditText.Text.Equals("NULL")) emailWrapper.EditText.Text = "";
            if (websiteWrapper.EditText.Text.Equals("NULL")) websiteWrapper.EditText.Text = "";
            if (adressWrapper.EditText.Text.Equals("NULL")) adressWrapper.EditText.Text = "";
            if (phoneWrapper.EditText.Text.Equals("NULL")) phoneWrapper.EditText.Text = "";

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