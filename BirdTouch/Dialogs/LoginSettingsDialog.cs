using Android.App;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using BirdTouch.Helpers;
using System;
using System.Net;
using System.Threading;

namespace BirdTouch.Dialogs
{
    [Activity(Label = "LoginSettingsFragment", Theme = "@style/Theme.DesignDemo")]
    class LoginSettingsDialog : Android.Support.V4.App.DialogFragment
    {
        private EditText _editWebApiProtocol;
        private EditText _editWebApiAddress;
        private EditText _editWebApiPort;
        private Button _btnSave;
        private ProgressBar _progressBar;
        private WebClient _webClientLoginSettings;
        private string _newProtocol;
        private string _newAddress;
        private string _newPort;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Create view
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.dialog_login_settings, container, false);

            // Find components
            _btnSave = view.FindViewById<Button>(Resource.Id.btnDialogLoginSetting);
            _editWebApiProtocol = view.FindViewById<EditText>(Resource.Id.txtApiServerProtocol);
            _editWebApiAddress = view.FindViewById<EditText>(Resource.Id.txtApiServerAddress);
            _editWebApiPort = view.FindViewById<EditText>(Resource.Id.txtApiServerPort);
            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBarLoginSetting);

            // Initialize web clients
            _webClientLoginSettings = new WebClient();

            // Set up events for web clients
            _webClientLoginSettings.DownloadDataCompleted += WebClientLoginSettings_DownloadDataCompleted;

            var sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            _editWebApiProtocol.Text = WebApiUrlGenerator.GetProtocol(sharedPreferences);
            _editWebApiAddress.Text = WebApiUrlGenerator.GetIpAddress(sharedPreferences);
            _editWebApiPort.Text = WebApiUrlGenerator.GetPort(sharedPreferences);

            // Save changes
            _btnSave.Click += SaveBtnClick;

            return view;
        }

        private void WebClientLoginSettings_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            _progressBar.Visibility = ViewStates.Gone;

            if (e.Error != null)
            {
                Snackbar.Make(
                    this.View,
                    Html.FromHtml("<font color=\"#ffffff\">Something is wrong...</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                WebApiUrlGenerator.SetProtocol(_newProtocol);
                WebApiUrlGenerator.SetIpAddress(_newAddress);
                WebApiUrlGenerator.SetPort(_newPort);

                // Sleep for the effect of something happening
                Thread.Sleep(300);
                Dismiss();
            }
        }

        public void SaveBtnClick(object sender, EventArgs e)
        {
            _newProtocol = string.IsNullOrEmpty(_editWebApiProtocol.Text)? null : _editWebApiProtocol.Text.Trim().ToLower();
            _newAddress = string.IsNullOrEmpty(_editWebApiAddress.Text) ? null : _editWebApiAddress.Text.Trim().ToLower();
            _newPort = string.IsNullOrEmpty(_editWebApiPort.Text) ? null : _editWebApiPort.Text.Trim().ToLower();

            try
            {
                var uri = new Uri($"{_newProtocol}://{_newAddress}:{_newPort}/{Application.Context.GetString(Resource.String.webapi_endpoint_welcome)}");

                _webClientLoginSettings.Headers.Clear();
                _webClientLoginSettings.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                _progressBar.Visibility = ViewStates.Visible;
                _webClientLoginSettings.DownloadDataAsync(uri);
            }
            catch (Exception)
            {
                Snackbar.Make(
                    this.View,
                    Html.FromHtml("<font color=\"#ffffff\">Something is wrong...</font>"),
                    Snackbar.LengthLong)
                     .Show();
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