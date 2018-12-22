using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using Android.Text;
using System;
using BirdTouch.Helpers;
using Android.Content;
using System.Net;

namespace BirdTouch.Activities
{
    [Activity(Label = "SettingsActivity", Theme = "@style/Theme.DesignDemo")]
    public class SettingsActivity : AppCompatActivity
    {
        private TextView _searchRadiusCurrent;
        private SeekBar _seekBar;
        private ImageView _imageView;
        private Button _buttonRemoveAccount;
        private WebClient _webClientRemoveAccount;

        private int _progressMinimumValue = 10;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Settings_Activity);

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>
                (Resource.Id.toolbar_settings);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.Title = string.Empty;
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            CollapsingToolbarLayout collapsingToolBar = FindViewById<CollapsingToolbarLayout>
                                                            (Resource.Id.collapsing_toolbar_settings);

            // Set title without image (different than in other views)
            collapsingToolBar.TitleEnabled = false;
            collapsingToolBar.Title = "Settings";
            toolBar.Title = "Settings";
            collapsingToolBar.Visibility = ViewStates.Visible;

            // Find components
            _seekBar = FindViewById<SeekBar>
                (Resource.Id.radiusSearchSeekBarId);

            _searchRadiusCurrent = FindViewById<TextView>
                (Resource.Id.seekBarInfoCurrentSearchRadius);

            _buttonRemoveAccount = FindViewById<Button>
                (Resource.Id.buttonRemoveAccount);

            _imageView = FindViewById<ImageView>(Resource.Id.settings_picture);

            // Initialize web clients
            _webClientRemoveAccount = new WebClient();

            // Set up events for web clients
            _webClientRemoveAccount.UploadStringCompleted += _webClientRemoveAccount_UploadStringCompleted;

            _seekBar.Progress = SearchRadiusSettingsHelper.GetSearchRadiusFromSharedPreferences(BaseContext) - _progressMinimumValue;
            _searchRadiusCurrent.Text = String.Format("{0} meters", _seekBar.Progress + _progressMinimumValue);

            _seekBar.ProgressChanged += SeekBar_ProgressChanged;
            _seekBar.StopTrackingTouch += SeekBar_StopTrackingTouch;

            _buttonRemoveAccount.Click += _buttonRemoveAccount_Click;
        }

        private void _buttonRemoveAccount_Click(object sender, EventArgs e)
        {
            new Android.Support.V7.App.AlertDialog.Builder(this).SetTitle("Confirm account deletion?")
                        .SetMessage("Are you sure? There is no going back (we do not keep backups of your data).")
                        .SetPositiveButton("YES", _buttonRemoveAccount_Click_Yes)
                        .SetNegativeButton("NO", _buttonRemoveAccount_Click_No)
                        .Create()
                        .Show();
        }

        private void _buttonRemoveAccount_Click_Yes(object sender, EventArgs e)
        {
            new Android.Support.V7.App.AlertDialog.Builder(this).SetTitle("Confirm one more time")
                        .SetMessage("This action will permanently remove all data associated with your account. This is what you really want?")
                        .SetPositiveButton("YES, DELETE EVERYTHING", _buttonRemoveAccount_Click_YesConfirmed)
                        .SetNegativeButton("NO", _buttonRemoveAccount_Click_No)
                        .Create()
                        .Show();
        }

        private void _buttonRemoveAccount_Click_No(object sender, EventArgs e)
        {
        }

        private void _buttonRemoveAccount_Click_YesConfirmed(object sender, EventArgs e)
        {
            var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_deleteAccount);

            _webClientRemoveAccount.Headers.Clear();
            _webClientRemoveAccount.Headers.Add(
                HttpRequestHeader.ContentType,
                "application/json");
            _webClientRemoveAccount.Headers.Add(
               HttpRequestHeader.Authorization,
               "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(ApplicationContext));

            _webClientRemoveAccount.UploadStringAsync(uri, "DELETE", string.Empty);
        }

        private void _webClientRemoveAccount_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add type of error
                Snackbar.Make(
                    _seekBar,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                JwtTokenHelper.RemoveTokenFromSharedPreferences(ApplicationContext);
                new Android.Support.V7.App.AlertDialog.Builder(this).SetTitle("Farewell")
                       .SetMessage("We are sad to see you go, godspeed.")
                       .SetPositiveButton("Close application", _buttonRemoveAccount_Click_YesAccountDeleted)
                       .Create()
                       .Show();
            }
        }

        private void _buttonRemoveAccount_Click_YesAccountDeleted(object sender, EventArgs e)
        {
            MoveTaskToBack(true);
            Process.KillProcess(Process.MyPid());
            System.Environment.Exit(1);
        }

        private void SeekBar_StopTrackingTouch(object sender, SeekBar.StopTrackingTouchEventArgs e)
        {
            var actualRadiusSet = e.SeekBar.Progress + _progressMinimumValue;

            Snackbar.Make(
                    sender as SeekBar,
                    Html.FromHtml(string
                    .Format("<font color=\"#ffffff\">Search radius is set to {0} meters</font>",
                            actualRadiusSet)),
                    Snackbar.LengthLong)
                     .Show();

            SearchRadiusSettingsHelper.AddSearchRadiusToSharedPreferences(e.SeekBar.Context,
                                                                          actualRadiusSet.ToString());
        }

        private void SeekBar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            _searchRadiusCurrent.Text = String.Format("{0} meters", e.Progress + _progressMinimumValue);
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