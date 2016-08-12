using System;
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
using BirdTouch.Models;
using Android.Text;
using System.Net;
using System.Collections.Specialized;
using Android.Graphics;
using System.IO;

namespace BirdTouch
{
    [Activity(Label = "EditCelebrityInfoActivity", Theme = "@style/Theme.DesignDemo")]
    public class EditCelebrityUserInfoActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            

            // Create your application here
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Activity_EditCelebrityUserInfo);

            
            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolbar_edit_celebrity_info); //nije isti toolbar kao u startpage

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