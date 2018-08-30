using Android.App;
using Android.OS;
using Android.Views;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;

namespace BirdTouch.Activities
{
    [Activity(Label = "EditCelebrityInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditCelebrityUserInfoActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            //TODO: Celebrity mode is currently not implemented

            // Create your application here
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditCelebrityUserInfo);

            // Not the same toolbar as in startpage
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_edit_celebrity_info);

            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_arrow_back_black_24dp);
            SupportActionBar.Title = "";

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