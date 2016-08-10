using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;
using Android.Support.Design.Widget;

namespace BirdTouch
{
    [Activity(Label = "AboutActivity", Theme = "@style/Theme.DesignDemo")]
    public class AboutActivity : AppCompatActivity 
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.About_Activity);

            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_about); //MALO B, nije isti toolbar kao u startpage
            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.Title = "About";
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
          
            CollapsingToolbarLayout collapsingToolBar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar_about);

            ImageView imageView = FindViewById<ImageView>(Resource.Id.author_picture);
            imageView.SetImageResource(Resource.Drawable.author);
            
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