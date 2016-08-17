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

namespace BirdTouch
{
    [Activity(Label = "EditUserInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditPrivateUserInfoActivity : AppCompatActivity
    {
        private User user;
        private ImageView imageView;
        private TextInputLayout firstNameWrapper;
        private TextInputLayout lastNameWrapper;
        private TextInputLayout emailWrapper;
        private TextInputLayout adressWrapper;
        private TextInputLayout phoneWrapper;
        private TextInputLayout dateOfBirthWrapper;
        private TextInputLayout facebookLinkWrapper;
        private TextInputLayout twitterLinkWrapper;
        private TextInputLayout gPlusLinkWrapper;
        private TextInputLayout linkedInLinkWrapper;
        private CollapsingToolbarLayout collapsingToolBar;
        private FloatingActionButton fabSaveChanges;
        private FloatingActionButton fabInsertPhoto;
        private WebClient webClient;

        private Uri uri;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditPrivateUserInfo);

            imageView = FindViewById<ImageView>(Resource.Id.profile_picture_edit_private_info);
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_edit_private_info); //nije isti toolbar kao u startpage

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = "";

            //popunjavanje polja iz baze
            user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userLoggedInJson"));


            if (user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(user.ProfilePictureData, 0, user.ProfilePictureData.Length).Result;
                imageView.SetImageBitmap(bm);
            }
            else
            {   //defaultni image kada korisnik jos uvek nije promenio, mada moze i u axml da se postavi
                imageView.SetImageResource(Resource.Drawable.blank_user_profile);
            }

            collapsingToolBar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar_edit_private_info);
            firstNameWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserFirstNameWrapper);
            lastNameWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserLastNameWrapper);
            emailWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserEmailWrapper);
            adressWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserAdressWrapper);
            phoneWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserPhoneWrapper);
            dateOfBirthWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserDateOfBirthWrapper);
            facebookLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserFacebookWrapper);
            twitterLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserTwitterWrapper);
            gPlusLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserGPlusWrapper);
            linkedInLinkWrapper = FindViewById<TextInputLayout>(Resource.Id.txtEditPrivateUserLinkedInWrapper);

            
            firstNameWrapper.EditText.Text=user.FirstName;
            lastNameWrapper.EditText.Text=user.LastName;
            emailWrapper.EditText.Text=user.Email;
            adressWrapper.EditText.Text=user.Adress;
            phoneWrapper.EditText.Text=user.PhoneNumber;
            dateOfBirthWrapper.EditText.Text = user.DateOfBirth;
            facebookLinkWrapper.EditText.Text=user.FbLink;
            twitterLinkWrapper.EditText.Text=user.TwitterLink;
            gPlusLinkWrapper.EditText.Text=user.GPlusLink;
            linkedInLinkWrapper.EditText.Text=user.LinkedInLink;
           // collapsingToolBar.Title = user.Username;

                  
            imageView.Click += ImageView_Click;

            webClient = new WebClient();
            webClient.UploadDataCompleted += WebClient_UploadDataCompleted;

 
            fabSaveChanges = FindViewById<FloatingActionButton>(Resource.Id.fabEditPrivateUserInfoSaveChanges);
            fabInsertPhoto = FindViewById<FloatingActionButton>(Resource.Id.fabEditPrivateUserInfoInsertPhoto);

            fabInsertPhoto.Click += FabInsertPhoto_Click;

            fabSaveChanges.Click += (o, e) => //o is sender, sender is button, button is a view
            {
                View view = o as View;
                if (Reachability.isOnline(this) && !webClient.IsBusy)
                { 

                    //zbog parametara mora da postoje sva polja kada se salju, makar privremeno 
                    checkIfEditTextsAreEmptyAndTurnThemToNULLString();

                   
                    //get ImageView (profileImage) as  array of bytes
                    imageView.BuildDrawingCache(true);
                    Bitmap bitmap = imageView.GetDrawingCache(true);
                    MemoryStream memStream = new MemoryStream();
                    bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, memStream); //moze i drugi format //max img size je 61kB
                    byte[] picData = memStream.ToArray();
                    //String picDataEncoded = Convert.ToBase64String(picData);
                    imageView.DestroyDrawingCache();



                    //insert parameters for header for web request
                    NameValueCollection parameters = new NameValueCollection();
                    parameters.Add("firstname", firstNameWrapper.EditText.Text);
                    parameters.Add("lastname", lastNameWrapper.EditText.Text);
                    parameters.Add("email", emailWrapper.EditText.Text);
                    parameters.Add("phone", phoneWrapper.EditText.Text);
                    parameters.Add("adress", adressWrapper.EditText.Text);
                    parameters.Add("dateofbirth", dateOfBirthWrapper.EditText.Text);
                    parameters.Add("fblink", facebookLinkWrapper.EditText.Text);
                    parameters.Add("twlink", twitterLinkWrapper.EditText.Text);
                    parameters.Add("gpluslink", gPlusLinkWrapper.EditText.Text);
                    parameters.Add("linkedinlink", linkedInLinkWrapper.EditText.Text);
                    parameters.Add("id", user.Id.ToString());

                    String restUriString = GetString(Resource.String.server_ip_changePrivateUser);
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
                StartPageActivity.ab.Title = firstNameWrapper.EditText.Text + " " + lastNameWrapper.EditText.Text; //update title u glavnoj activity, jer je ime i prezime sada promenjeno
           //   StartPageActivity.profilePictureNavigationHeader.SetImageBitmap(DecodeBitmapFromStream(data.Data, 400, 300));
            }
        }

        private void ImageView_Click(object sender, EventArgs e) //klik na sliku, da bi se promenila profilna slika
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            this.StartActivityForResult(Intent.CreateChooser(intent, "Select a Photo"), 0);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data) //kada se dobije iz galerije nazad neki podatak
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(resultCode == Result.Ok)
            {
                System.IO.Stream stream = ContentResolver.OpenInputStream(data.Data);
            //  imageView.SetImageBitmap(BitmapFactory.DecodeStream(stream)); neefikasan nacin ucitavanja slika, nema skaliranja
                imageView.SetImageBitmap(DecodeBitmapFromStream(data.Data, 400, 300)); //mozda su prevelike dimenzije, moze da se podesi
            
            }
        }

        

        private void checkIfEditTextsAreEmptyAndTurnThemToNULLString()
        {
            //da bi moglo da postoji i prazno polje, jer mora da postoje svi parametri u rest servisu
            if (firstNameWrapper.EditText.Text.Equals("")) firstNameWrapper.EditText.Text = "NULL";
            if (lastNameWrapper.EditText.Text.Equals("")) lastNameWrapper.EditText.Text = "NULL";
            if (emailWrapper.EditText.Text.Equals("")) emailWrapper.EditText.Text = "NULL";
            if (adressWrapper.EditText.Text.Equals("")) adressWrapper.EditText.Text = "NULL";
            if (phoneWrapper.EditText.Text.Equals("")) phoneWrapper.EditText.Text = "NULL";
            if (dateOfBirthWrapper.EditText.Text.Equals("")) dateOfBirthWrapper.EditText.Text = "NULL";
            if (facebookLinkWrapper.EditText.Text.Equals("")) facebookLinkWrapper.EditText.Text = "NULL";
            if (twitterLinkWrapper.EditText.Text.Equals("")) twitterLinkWrapper.EditText.Text = "NULL";
            if (gPlusLinkWrapper.EditText.Text.Equals("")) gPlusLinkWrapper.EditText.Text = "NULL";
            if (linkedInLinkWrapper.EditText.Text.Equals("")) linkedInLinkWrapper.EditText.Text = "NULL";
        }

        private void returnAllNullEditTextsToEmpty()
        {
            //da bi moglo da postoji i prazno polje, jer mora da postoje svi parametri u rest servisu
            //sada vracamo na prazno ako je prosledjeno NULL kao parametar
            if (firstNameWrapper.EditText.Text.Equals("NULL")) firstNameWrapper.EditText.Text = "";
            if (lastNameWrapper.EditText.Text.Equals("NULL")) lastNameWrapper.EditText.Text = "";
            if (emailWrapper.EditText.Text.Equals("NULL")) emailWrapper.EditText.Text = "";
            if (adressWrapper.EditText.Text.Equals("NULL")) adressWrapper.EditText.Text = "";
            if (phoneWrapper.EditText.Text.Equals("NULL")) phoneWrapper.EditText.Text = "";
            if (dateOfBirthWrapper.EditText.Text.Equals("NULL")) dateOfBirthWrapper.EditText.Text = "";
            if (facebookLinkWrapper.EditText.Text.Equals("NULL")) facebookLinkWrapper.EditText.Text = "";
            if (twitterLinkWrapper.EditText.Text.Equals("NULL")) twitterLinkWrapper.EditText.Text = "";
            if (gPlusLinkWrapper.EditText.Text.Equals("NULL")) gPlusLinkWrapper.EditText.Text = "";
            if (linkedInLinkWrapper.EditText.Text.Equals("NULL")) linkedInLinkWrapper.EditText.Text = "";
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

        private Bitmap DecodeBitmapFromStream(Android.Net.Uri data, int requestedWidth, int requestedHeight)
        {
            //Decode with inJustDecodeBounds = true to check dimensions
            //proveravamo samo velicinu slike, da nije neka prevelika slika koja bi napunila memoriju
            Stream stream = ContentResolver.OpenInputStream(data);
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InJustDecodeBounds = true;
            BitmapFactory.DecodeStream(stream,null,options);

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

            if(height > requestedHeight || width > requestedWidth)
            {
                //slika je veca nego sto nam treba
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while((halfHeight/inSampleSize)>=requestedHeight && (halfWidth / inSampleSize) >= requestedWidth)
                {
                    inSampleSize *= 2;
                }
            }
            Console.WriteLine();
            Console.WriteLine("SampleSizeBitmap: " + inSampleSize.ToString());
            Console.WriteLine();
            return inSampleSize;
        }

    }
}









//nepotrebno


//String restUriString = GetString(Resource.String.server_ip_changePrivateUser)
//+ user.Id + "/" + firstNameWrapper.EditText.Text + "/" + lastNameWrapper.EditText.Text + "/" + emailWrapper.EditText.Text
//+ "/" + phoneWrapper.EditText.Text + "/" + adressWrapper.EditText.Text + "/" + dateOfBirthWrapper.EditText.Text + "/"
//+ facebookLinkWrapper.EditText.Text + "/"
//+ twitterLinkWrapper.EditText.Text + "/" + gPlusLinkWrapper.EditText.Text + "/" + linkedInLinkWrapper.EditText.Text;

//uri = new Uri(restUriString);
//webClient.DownloadDataAsync(uri);

//  webClient.UploadDataAsync(uri, changedUser);






//private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
//{
//    if (e.Error != null)
//    {
//        //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno

//        returnAllNullEditTextsToEmpty();
//        Console.WriteLine("*******Error webclient data save changes error");
//        Console.WriteLine(e.Error.Message);
//        Console.WriteLine("******************************************************");
//        Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();

//    }
//    else
//    {
//        returnAllNullEditTextsToEmpty();
//        Console.WriteLine("Success!");
//        string jsonResult = Encoding.UTF8.GetString(e.Result);
//        Console.Out.WriteLine(jsonResult);
//        Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">Changes saved successfully</font>"), Snackbar.LengthLong).Show();
//        StartPageActivity.ab.Title = firstNameWrapper.EditText.Text + " " + lastNameWrapper.EditText.Text; //update title u glavnoj activity, jer je ime i prezime sada promenjeno





//        NameValueCollection nvc = new NameValueCollection();
//        //  nvc.Add("picDataEncoded", picDataEncoded);
//        nvc.Add("test2", "test2 je uspesno stigao");
//        nvc.Add("test3", "test3 je uspesno stigao");

//        String restUriString = "http://192.168.0.102:80/BirdTouchServer/rest/uploadPictureData/" + user.Id.ToString();
//        uri = new Uri(restUriString);
//        //webClientPictureUpload.Headers["Content-Type"] = "application/json";

//        webClientPictureUpload.Headers.Add(nvc);
//        webClientPictureUpload.UploadDataAsync(uri, picData);

//        // webClientPictureUpload.UploadValuesAsync(uri, nvc);           
//    }
//}


//private void WebClientPictureUpload_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
//{
//    if (e.Error != null)
//    {
//        Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">3Changes pic not saved successfully</font>"), Snackbar.LengthLong).Show();

//    }
//    else
//    {
//        Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">3Changes pic saved successfully</font>"), Snackbar.LengthLong).Show();

//    }
//}


//private void WebClientPictureUpload_UploadValuesCompleted(object sender, UploadValuesCompletedEventArgs e)
//{
//    if (e.Error != null)
//    {
//        Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">2Changes pic not saved successfully</font>"), Snackbar.LengthLong).Show();

//    }
//    else
//    {
//        Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">2Changes pic saved successfully</font>"), Snackbar.LengthLong).Show();

//    }
//}






//Bitmap bitmap = BitmapFactory.DecodeStream(stream); //ako hocemo skaliranu, onda koristimo DecodeBitmapFromStream(data.Data, 400, 300)
//MemoryStream memStream = new MemoryStream();
//bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, memStream); //moze i drugi format //max img size je 61kB
//byte[] picData = memStream.ToArray();