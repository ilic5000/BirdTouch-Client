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

namespace BirdTouch.Activities
{
    [Activity(Label = "SettingsActivity", Theme = "@style/Theme.DesignDemo")]
    public class SettingsActivity : AppCompatActivity
    {
        private TextView _searchRadiusCurrent;
        private SeekBar _seekBar;
        private ImageView _imageView;
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

            _imageView = FindViewById<ImageView>(Resource.Id.settings_picture);

            _seekBar.Progress = SearchRadiusSettingsHelper.GetSearchRadiusFromSharedPreferences(BaseContext) - _progressMinimumValue;
            _searchRadiusCurrent.Text = String.Format("{0} meters", _seekBar.Progress + _progressMinimumValue);

            _seekBar.ProgressChanged += SeekBar_ProgressChanged;
            _seekBar.StopTrackingTouch += SeekBar_StopTrackingTouch;
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