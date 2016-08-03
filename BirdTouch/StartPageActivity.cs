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
using BirdTouch.Models;
using Android.Support.V4.Widget;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using SupportActionBar = Android.Support.V7.App.ActionBar;
using SupportFragment = Android.Support.V4.App.Fragment;
using SupportFragmentManager = Android.Support.V4.App.FragmentManager;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.App;
using Java.Lang;

namespace BirdTouch
{
    [Activity(Label = "StartPageActivity", Theme = "@style/Theme.DesignDemo")]
    public class StartPageActivity : AppCompatActivity //zbog design library nije obican activity
    {

        private DrawerLayout drawerLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.StartPage);

            SupportToolbar toolBar = FindViewById<SupportToolbar>(Resource.Id.toolBar);
            SetSupportActionBar(toolBar);

            SupportActionBar ab = SupportActionBar;
            ab.SetHomeAsUpIndicator(Resource.Drawable.ic_menu); //hamburger menu indicator
            ab.SetDisplayHomeAsUpEnabled(true); //enablovan za home button

            
           User user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userLoggedInJson"));
           // Console.WriteLine("SSSSS {0} - {1}", user.FirstName, user.LastName);
           // ab.Title = user.Username; //kada se uloguje user, da vidimo njegov username u action baru
              ab.Title = user.FirstName + " " + user.LastName;
            // Create your application here

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            if(navigationView != null)
            {

                SetUpDrawerContent(navigationView);

            }

            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);

            ViewPager viewPager = FindViewById<ViewPager>(Resource.Id.viewpager);

            SetUpViewPager(viewPager);

            tabs.SetupWithViewPager(viewPager);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += (o, e) => //o is sender, sender is button, button is a view
            {
                View anchor = o as View;
                Snackbar.Make(anchor, "Yay Snackbar!!", Snackbar.LengthLong).SetAction("Action", v=>
                {
                    //Do something here
                    //Intent intent new Intent();
                }).Show();
            };
        }

        private void SetUpViewPager(ViewPager viewPager)
        {
            TabAdapter adapter = new TabAdapter(SupportFragmentManager);
            adapter.AddFragment(null, "Private");
            adapter.AddFragment(null, "Business");
            adapter.AddFragment(null, "Celebrity");
            viewPager.Adapter = adapter;
        }

        private void SetUpDrawerContent(NavigationView navigationView)
        {
           
        }

        public class TabAdapter : FragmentPagerAdapter //ovo poziva viewpager kako bi znao koji fragment u kom tabu 
        {

            public List<SupportFragment> Fragments { get; set; }

            public List<string> FragmentNames { get; set; }

            public TabAdapter(SupportFragmentManager sfm) : base(sfm)
            {
                Fragments = new List<SupportFragment>();
                FragmentNames = new List<string>();
            }

            public void AddFragment(SupportFragment fragment, string name)
            {
                Fragments.Add(fragment);
                FragmentNames.Add(name);
            }

            public override int Count
            {
                get
                {
                    return Fragments.Count;
                }
            }

            public override SupportFragment GetItem(int position)
            {
                return Fragments[position];
            }

            public override ICharSequence GetPageTitleFormatted(int position)
            {
                return new Java.Lang.String(FragmentNames[position]);
            }
        }

        //public override bool OnCreateOptionsMenu(IMenu menu)
        //{
        //    var inflater = MenuInflater;
        //    inflater.Inflate(Resource.Menu.menu_main, menu);
        //    return base.OnCreateOptionsMenu(menu);
        //}

        //public override bool OnOptionsItemSelected(IMenuItem item)
        //{
        //    int id = item.ItemId;
        //    if(id == Resource.Id.privateUserActionBarButton) {
        //        Toast.MakeText(this, "Private user activity starting...", ToastLength.Short).Show();
        //    }
        //    if (id == Resource.Id.bussinessUserActionBarButton)
        //    {
        //        Toast.MakeText(this, "Bussiness user activity starting...", ToastLength.Short).Show();
        //    }

        //    return base.OnOptionsItemSelected(item);
        //}

    }


    


}