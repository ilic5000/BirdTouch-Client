using System;
using System.Collections.Generic;
using System.Linq;
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

namespace BirdTouch
{
    [Activity(Label = "EditUserInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditPrivateUserInfoActivity : AppCompatActivity
    {
        private User user;
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
        private WebClient webClient;
        private Uri uri;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditPrivateUserInfo);

            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_edit_private_info); //nije isti toolbar kao u startpage
            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userLoggedInJson"));
            
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
            facebookLinkWrapper.EditText.Text=user.FbLink;
            twitterLinkWrapper.EditText.Text=user.TwitterLink;
            gPlusLinkWrapper.EditText.Text=user.GPlusLink;
            linkedInLinkWrapper.EditText.Text=user.LinkedInLink;
            collapsingToolBar.Title = user.Username;



            firstNameWrapper.ClearFocus();

            webClient = new WebClient();
            webClient.DownloadDataCompleted += WebClient_DownloadDataCompleted;

            fabSaveChanges = FindViewById<FloatingActionButton>(Resource.Id.fabEditPrivateUserInfoSaveChanges);
            fabSaveChanges.Click += (o, e) => //o is sender, sender is button, button is a view
            {
                View view = o as View;
                if (Reachability.isOnline(this))
                { //provera da li je aplikaciji dostupan net
                    

                    //NameValueCollection parameters = new NameValueCollection();
                    //parameters.Add("id2", user.Id.ToString());
                    //parameters.Add("firstname2", firstNameWrapper.EditText.Text);
                    //parameters.Add("lastname2", lastNameWrapper.EditText.Text);
                    //parameters.Add("email2", emailWrapper.EditText.Text);
                    //parameters.Add("phone2", phoneWrapper.EditText.Text);
                    //parameters.Add("adress2", adressWrapper.EditText.Text);
                    //parameters.Add("dateofbirth2", dateOfBirthWrapper.EditText.Text);
                    //parameters.Add("fblink2", facebookLinkWrapper.EditText.Text);
                    //parameters.Add("twitter2", twitterLinkWrapper.EditText.Text);
                    //parameters.Add("gpluslink2", gPlusLinkWrapper.EditText.Text);
                    //parameters.Add("linkedin2", linkedInLinkWrapper.EditText.Text);



                    String restUriString = GetString(Resource.String.server_ip_changePrivateUser) 
                    + user.Id + "/" + firstNameWrapper.EditText.Text + "/" + lastNameWrapper.EditText.Text + "/" + emailWrapper.EditText.Text
                    + "/" + phoneWrapper.EditText.Text + "/" + adressWrapper.EditText.Text + "/" + dateOfBirthWrapper.EditText.Text + "/" 
                    + facebookLinkWrapper.EditText.Text + "/" 
                    + twitterLinkWrapper.EditText.Text + "/" + gPlusLinkWrapper.EditText.Text + "/" + linkedInLinkWrapper.EditText.Text;

                    uri = new Uri(restUriString);
                    
                    webClient.DownloadDataAsync(uri);
                }
                else
                {

                    Snackbar.Make(view, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

                }

            };

            LoadBackDrop();
        }

        private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno
                
                Console.WriteLine("*******Error webclient data save changes error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
               
                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                Snackbar.Make(fabSaveChanges, Html.FromHtml("<font color=\"#ffffff\">Changes saved successfully</font>"), Snackbar.LengthLong).Show();


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



        private void LoadBackDrop()
        {
            ImageView imageView = FindViewById<ImageView>(Resource.Id.backdrop_edit_private_info);
            imageView.SetImageResource(Fragments.CheeseHelper.Cheeses.RandomCheeseDrawable);
        }
    }
}