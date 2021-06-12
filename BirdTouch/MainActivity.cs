using Android;
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
using IO.Blushine.Android.UI.Showcase;
using System;
using System.Net;
using System.Text;

namespace BirdTouch
{
    [Activity(Label = "BirdTouch", MainLauncher = true, Icon = "@drawable/Logo", Theme = "@style/Theme.DesignDemo")]
    public class MainActivity : FragmentActivity
    {
        private Button _btnRegister;
        private Button _btnSignIn;
        private Button _btnLoginSettings;
        private WebClient _webClientSignedIn;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            ISharedPreferences pref = ApplicationContext.GetSharedPreferences(SharedPreferencesConstants.FIRST_TIME_RUN, FileCreationMode.Private);
            if (!pref.GetBoolean($"{SharedPreferencesConstants.FIRST_TIME_RUN_PERMISSIONS_REQUESTED}", defValue: false))
            {
                pref.Edit().PutBoolean($"{SharedPreferencesConstants.FIRST_TIME_RUN_PERMISSIONS_REQUESTED}", value: true).Commit();

                var neededPermissions = new string[]
                        {
                            Manifest.Permission.AccessCoarseLocation,
                            Manifest.Permission.AccessFineLocation,
                            Manifest.Permission.ReadExternalStorage,
                            Manifest.Permission.WriteExternalStorage
                        };

                ActivityCompat.RequestPermissions(this, neededPermissions, 225);
            }

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
                _btnLoginSettings = FindViewById<Button>(Resource.Id.btnLoginSettings);

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

                // Show dialog for login settings
                _btnLoginSettings.Click += (object sender, EventArgs e) =>
                {
                    new LoginSettingsDialog().Show(SupportFragmentManager, "Dialog fragment");
                };

                pref = ApplicationContext.GetSharedPreferences(SharedPreferencesConstants.FIRST_TIME_RUN, FileCreationMode.Private);
                if (!pref.GetBoolean($"{SharedPreferencesConstants.FIRST_TIME_RUN_SERVER_SETTINGS_WAS_SHOWN}", defValue: false))
                {
                    pref.Edit().PutBoolean($"{SharedPreferencesConstants.FIRST_TIME_RUN_SERVER_SETTINGS_WAS_SHOWN}", value: true).Commit();

                    var _loginPageSequence = new MaterialShowcaseSequence(this, "server-settings-showcase");

                    _loginPageSequence.AddSequenceItem(new MaterialShowcaseView.Builder(this)
                        .SetTarget(_btnLoginSettings)
                        .SetTitleText("Server settings")
                        .SetDismissText("GOT IT")
                        .SetContentText("Here you can modify server address. \nOnce we go LIVE this option will be removed.")
                        .SetSingleUse(showcaseId: "server-settings-showcase")
                        .SetDelay(200) // optional but starting animations immediately in onCreate can make them choppy
                        .Build());

                    _loginPageSequence._showNow();
                }
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

