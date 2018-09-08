using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Text;
using Android.Widget;
using BirdTouch.Activities;
using BirdTouch.Constants;
using BirdTouch.Dialogs;
using BirdTouch.Helpers;
using System;
using System.Net;
using System.Text;

namespace BirdTouch
{
    [Activity(Label = "BirdTouch v1.0.7", MainLauncher = true, Icon = "@drawable/Logo", Theme = "@style/Theme.DesignDemo")]
    public class MainActivity : FragmentActivity
    {
        private Button _btnRegister;
        private Button _btnSignIn;
        private WebClient _webClientSignedIn;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // If User is already signed in
            if (JwtTokenHelper.IsUserSignedIn(ApplicationContext)
                && Reachability.IsOnline(this))
            {
                // Set our view from the "main" layout resource
                SetContentView(Resource.Layout.MainAlreadySignedIn);

                // Initialize web clients
                _webClientSignedIn = new WebClient();

                // Set up events for web clients
                _webClientSignedIn.DownloadDataCompleted += WebClientSignedIn_DownloadDataCompleted;

                SignInUserFromJwtTokenCredentials();
            }
            else
            {
                // Set our view from the "main" layout resource
                SetContentView(Resource.Layout.Main);

                // Find buttons
                _btnRegister = FindViewById<Button>(Resource.Id.btnRegister);
                _btnSignIn = FindViewById<Button>(Resource.Id.btnSignIn);

                // Add desired actions for buttons

                // Show dialog for signing up
                _btnRegister.Click += (object sender, EventArgs e) =>
                {
                    new SignUpDialog().Show(SupportFragmentManager, "Dialog fragment");
                };

                // Show dialog for signing in
                _btnSignIn.Click += (object sender, EventArgs e) =>
                {
                    new SignInDialog().Show(SupportFragmentManager, "Dialog fragment");
                };
            }
        }

        public void SignInUserFromJwtTokenCredentials()
        {
            // Check if internet access is available
            if (Reachability.IsOnline(this)
                && !_webClientSignedIn.IsBusy)
            {
                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_getPrivateInfo);

                _webClientSignedIn.Headers.Clear();
                _webClientSignedIn.Headers.Add(
                    HttpRequestHeader.Authorization,
                    "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(ApplicationContext));

                _webClientSignedIn.DownloadDataAsync(uri);
            }
        }

        private void WebClientSignedIn_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var errorText = FindViewById<TextView>(Resource.Id.txtAlreadySignInServerError);

            if (e.Error == null)
            {
                errorText.Visibility = Android.Views.ViewStates.Gone;
                Intent intent = new Intent(this, typeof(StartPageActivity));
                intent.PutExtra(IntentConstants.LOGGED_IN_USER, Encoding.UTF8.GetString(e.Result));
                this.StartActivity(intent);
                this.Finish();
            }
            else
            {
                errorText.Visibility = Android.Views.ViewStates.Visible;
            }
        }
    }
}

