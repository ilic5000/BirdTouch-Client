using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V7.App;
using Android.Support.Design.Widget;

namespace BirdTouch
{
    [Activity(Label ="UserDetailActivity", Theme = "@style/Theme.DesignDemo")]
    public class UserDetailActivity : AppCompatActivity
    {

        public const string EXTRA_NAME = "user_name";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_Detail);

            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar); //MALO B, nije isti toolbar kao u startpage
            SetSupportActionBar(toolBar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            string userName = Intent.GetStringExtra(EXTRA_NAME); //treba da se promeni sve za usera, da nije sir
            CollapsingToolbarLayout collapsingToolBar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar);
            collapsingToolBar.Title = userName;

            LoadBackDrop();

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


    
        private void LoadBackDrop()
        {
            ImageView imageView = FindViewById<ImageView>(Resource.Id.backdrop);
            imageView.SetImageResource(Fragments.CheeseHelper.Cheeses.RandomCheeseDrawable);
        }
    }
}