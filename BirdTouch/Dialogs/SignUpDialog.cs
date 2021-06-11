using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using BirdTouch.Activities;
using BirdTouch.Constants;
using BirdTouch.Helpers;
using BirdTouch.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Android.Text;

namespace BirdTouch.Dialogs
{
    [Activity(Label = "SignUpFragment", Theme = "@style/Theme.DesignDemo")]
    class SignUpDialog : Android.Support.V4.App.DialogFragment
    {
        private TextInputLayout _usernameWrapper;
        private TextInputLayout _passwordWrapper;
        private TextInputLayout _passwordCheckWrapper;
        private TextInputLayout _firstnameWrapper;
        private TextInputLayout _lastnameWrapper;
        private TextInputLayout _descriptionCheckWrapper;

        private ProgressBar _progressBar;
        private WebClient _webClientUsernameAvailabilityChecker;
        private WebClient _webClientRegister;
        private Button _btnRegister;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Create view
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.dialog_register, container, false);

            // Find components
            _usernameWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterUsername);
            _passwordWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterPassword);
            _passwordCheckWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterPasswordCheck);
            _firstnameWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterFirstname);
            _lastnameWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterLastname);
            _descriptionCheckWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterDescription);
            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBarRegister);
            _btnRegister = view.FindViewById<Button>(Resource.Id.btnDialogRegister);

            // Set starting values for some components
            _usernameWrapper.Error = string.Empty;
            _passwordWrapper.Error = string.Empty;
            _passwordCheckWrapper.Error = string.Empty;

            // Initialize web clients
            _webClientUsernameAvailabilityChecker = new WebClient();
            _webClientRegister = new WebClient();

            // Set up events for web clients
            _webClientUsernameAvailabilityChecker.DownloadDataCompleted += WebClient_DownloadDataCompleted;
            _webClientRegister.UploadStringCompleted += WebClientRegister_UploadStringCompleted;

            // Set up events for text input components
            _usernameWrapper.EditText.FocusChange += UsernameEditText_AfterFocusChanged;
            _usernameWrapper.EditText.AfterTextChanged += UsernameEditText_AfterTextChanged;
            _passwordCheckWrapper.EditText.AfterTextChanged += PasswordCheckEditText_AfterTextChanged;

            // Sign up new user
            _btnRegister.Click += RegisterButtonClick;

            return view;
        }

        private void RegisterButtonClick(object sender, EventArgs e)
        {
            // Check if internet access is available
            if (Reachability.IsOnline(Activity) && !_webClientRegister.IsBusy)
            {
                // Checks if text fields are not empty, and if passwords match
                if (string.IsNullOrEmpty(_usernameWrapper.Error)
                    && string.IsNullOrEmpty(_passwordCheckWrapper.Error)
                    && !string.IsNullOrEmpty(_firstnameWrapper.EditText.Text)
                    && !string.IsNullOrEmpty(_lastnameWrapper.EditText.Text)
                    && !string.IsNullOrEmpty(_descriptionCheckWrapper.EditText.Text)
                    && !string.IsNullOrEmpty(_usernameWrapper.EditText.Text)
                    && !string.IsNullOrEmpty(_passwordWrapper.EditText.Text)
                    && !string.IsNullOrEmpty(_passwordCheckWrapper.EditText.Text)
                    && _passwordCheckWrapper.EditText.Text.Equals(_passwordWrapper.EditText.Text))
                {
                    _progressBar.Visibility = ViewStates.Visible;

                    var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_registerUser);

                    LoginCredentials loginCredentials = new LoginCredentials()
                    {
                        Username = _usernameWrapper.EditText.Text,
                        Password = _passwordCheckWrapper.EditText.Text,
                        Firstname = _firstnameWrapper.EditText.Text,
                        Lastname = _lastnameWrapper.EditText.Text,
                        Description = _descriptionCheckWrapper.EditText.Text,
                    };

                    _webClientRegister.Headers.Clear();
                    _webClientRegister.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                    _webClientRegister.UploadStringAsync(uri, "POST", loginCredentials.ToJson());
                }
                else
                {
                    Snackbar.Make(
                        this.View,
                        Android.Text.Html.FromHtml("<font color=\"#ffffff\">Pease fill out all of the fields in the form.</font>"),
                        Snackbar.LengthLong)
                         .Show();
                }
            }
            else
            {
                Snackbar.Make(
                    this.View,
                    Android.Text.Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
        }

        private void WebClientRegister_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                var errorDescription = ((System.Net.WebException)e.Error).Response.Headers.Get(name: "user-creation-first-error");
                // TODO: Notify users about the type of error
                Snackbar.Make(
                    this.View,
                    Android.Text.Html.FromHtml($"<font color=\"#ffffff\">{errorDescription}</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                var response = JsonConvert.DeserializeObject<LoginResponse>(e.Result);

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

        private void UsernameEditText_AfterTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            if (_usernameWrapper.EditText.Text.Contains(" "))
            {
                _usernameWrapper.Error = "Invalid username";
            }
            else
            {
                _usernameWrapper.Error = string.Empty;
            }
        }

        private void PasswordCheckEditText_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            if (!_passwordWrapper.EditText.Text.Equals(_passwordCheckWrapper.EditText.Text))
            {
                _passwordCheckWrapper.Error = "Password missmatch";
            }
            else
            {
                _passwordCheckWrapper.Error = string.Empty;
            }
        }

        private void UsernameEditText_AfterFocusChanged(object sender, View.FocusChangeEventArgs e)
        {
            if (!string.IsNullOrEmpty(_usernameWrapper.Error))
            {
                return;
            }

            if (!string.IsNullOrEmpty(_usernameWrapper.EditText.Text)
                && Reachability.IsOnline(Activity)
                && !_webClientUsernameAvailabilityChecker.IsBusy)
            {
                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_doesUsernameExist);

                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("username", _usernameWrapper.EditText.Text);

                _webClientUsernameAvailabilityChecker.QueryString.Clear();
                _webClientUsernameAvailabilityChecker.QueryString.Add(parameters);
                _webClientUsernameAvailabilityChecker.DownloadDataAsync(uri);
            }
        }

        private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Notify users about the type of error
                // TODO: Decide how to handle this, empty or some error
                _usernameWrapper.Error = string.Empty;
            }
            else
            {
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                var response = JsonConvert.DeserializeObject<UserExistResponse>(jsonResult);

                if (response.UserExists)
                {
                    _usernameWrapper.Error = "Username already exists";
                }
                else
                {
                    _usernameWrapper.Error = string.Empty;
                }
            }
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