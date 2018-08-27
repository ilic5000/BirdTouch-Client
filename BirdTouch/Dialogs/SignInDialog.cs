using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using BirdTouch.Activities;
using BirdTouch.Constants;
using BirdTouch.Helpers;
using BirdTouch.Models;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;

namespace BirdTouch.Dialogs
{
    [Activity(Label = "SignInFragment", Theme = "@style/Theme.DesignDemo")]
    class SignInDialog : Android.Support.V4.App.DialogFragment
    {
        private EditText _editTxtUsername;
        private EditText _editTxtPassword;
        private Button _btnSignIn;
        private TextInputLayout _passwordWrapper;
        private ProgressBar _progressBar;
        private WebClient _webClientLogin;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Create view
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.dialog_signin, container, false);

            // Find components
            _btnSignIn = view.FindViewById<Button>(Resource.Id.btnDialogSignIn);
            _editTxtUsername = view.FindViewById<EditText>(Resource.Id.txtUsernameSignIn);
            _editTxtPassword = view.FindViewById<EditText>(Resource.Id.txtPasswordSignIn);
            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBarSignIn);
            _passwordWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutSignInPassword);

            // Initialize web clients
            _webClientLogin = new WebClient();

            // Set up events for web clients
            _webClientLogin.UploadStringCompleted += WebClientLogin_UploadStringCompleted;

            // Log in user
            _btnSignIn.Click += SignInBtnClick;

            return view;
        }

        public void SignInBtnClick(object sender, EventArgs e)
        {
            // Check if internet access is available
            if (Reachability.IsOnline(Activity)
                && !_webClientLogin.IsBusy)
            {
                _progressBar.Visibility = ViewStates.Visible;

                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_getUserLogin);

                LoginCredentials loginCredentials = new LoginCredentials()
                {
                    Username = _editTxtUsername.Text,
                    Password = _editTxtPassword.Text
                };

                _webClientLogin.Headers.Clear();
                _webClientLogin.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                _webClientLogin.UploadStringAsync(uri, "POST", loginCredentials.ToJson());
            }
            else
            {
                Snackbar.Make(
                    this.View,
                    Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
        }

        private void WebClientSignedIn_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Maybe notify users about the different types of error (connectivity issues, credentials etc.)
                _passwordWrapper.Error = "There was some error with JWT Token, try again.";
            }
            else
            {
                _passwordWrapper.Error = string.Empty;

                Intent intent = new Intent(this.Activity, typeof(StartPageActivity));
                intent.PutExtra(IntentConstants.LOGGED_IN_USER, Encoding.UTF8.GetString(e.Result));
                this.StartActivity(intent);
                this.Activity.Finish();
            }
        }

        private void WebClientLogin_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Maybe notify users about the different types of error (connectivity issues, credentials etc.)
                _passwordWrapper.Error = "Wrong username/password, try again";
            }
            else
            {
                var response = JsonConvert.DeserializeObject<LoginResponse>(e.Result);
                _passwordWrapper.Error = string.Empty;

                // Add token for the next time you run app
                JwtTokenHelper.AddTokenToSharedPreferences(Context, response.JwtToken);

                Intent intent = new Intent(this.Activity, typeof(StartPageActivity));
                intent.PutExtra(IntentConstants.LOGGED_IN_USER, JsonConvert.SerializeObject(response.User));
                this.StartActivity(intent);
                this.Activity.Finish();
            }

            // Remove progress bar, because request has been completed
            _progressBar.Visibility = ViewStates.Gone;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            // Remove title bar
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            base.OnActivityCreated(savedInstanceState);

            //Animation for entering, from the botom to center
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
        }
    }
}