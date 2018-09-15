using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;
using Android.Support.Design.Widget;

namespace BirdTouch.Activities
{
    [Activity(Label = "AboutActivity", Theme = "@style/Theme.DesignDemo")]
    public class AboutActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.About_Activity);

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>
                (Resource.Id.toolbar_about);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.Title = string.Empty;
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);

            CollapsingToolbarLayout collapsingToolBar = FindViewById<CollapsingToolbarLayout>
                                                            (Resource.Id.collapsing_toolbar_about);

            ImageView imageView = FindViewById<ImageView>(Resource.Id.author_picture);

            // Set title
            collapsingToolBar.Title = "About";
            collapsingToolBar.Visibility = ViewStates.Visible;

            // Set author image
            //imageView.SetImageResource(Resource.Drawable.blank);
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