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
using BirdTouch.Fragments;
using System.Net;
using Android.Text;
using Android.Graphics;
using System.IO;
using Clans.Fab;

namespace BirdTouch
{
    [Activity(Label = "StartPageActivity", Theme = "@style/Theme.DesignDemo")]
    public class StartPageActivity : AppCompatActivity //zbog design library nije obican activity
    {  
        //private FloatingActionButton

        private Clans.Fab.FloatingActionButton fab_menu_refresh;
        private Clans.Fab.FloatingActionButton fab_menu_automatically;
        private Clans.Fab.FloatingActionButton fab_menu_gps;
        private Clans.Fab.FloatingActionMenu fab_menu;

        private DrawerLayout drawerLayout;
        private NavigationView navigationView;
        private SupportToolbar toolBar;
        public static SupportActionBar ab;
        public static ImageView profilePictureNavigationHeader;
        private TabLayout tabs;
        private ViewPager viewPager;

        public static User user;
        public static int visibilityMode = 0;
        private Business business;
        private System.String userPassword;

        private WebClient webClientUserPrivateDataUponOpeningEditDataActivity;
        private WebClient webClientUserBusinessDataUponOpeningEditDataActivity;
        private Uri uri;


        private TabAdapter adapter;




        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.StartPage);

            webClientUserPrivateDataUponOpeningEditDataActivity = new WebClient();
            webClientUserPrivateDataUponOpeningEditDataActivity.DownloadDataCompleted += WebClientUserPrivateDataUponOpeningEditDataActivity_DownloadDataCompleted;

            webClientUserBusinessDataUponOpeningEditDataActivity = new WebClient();
            webClientUserBusinessDataUponOpeningEditDataActivity.DownloadDataCompleted += WebClientUserBusinessDataUponOpeningEditDataActivity_DownloadDataCompleted;


            toolBar = FindViewById<SupportToolbar>(Resource.Id.toolBar);
            SetSupportActionBar(toolBar);

            navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            profilePictureNavigationHeader = navigationView.GetHeaderView(0).FindViewById<ImageView>(Resource.Id.nav_header_imgViewHeader);

            ab = SupportActionBar;
            ab.SetHomeAsUpIndicator(Resource.Drawable.ic_menu); //hamburger menu indicator
            ab.SetDisplayHomeAsUpEnabled(true); //enablovan za home button

            userPassword = Intent.GetStringExtra("userPassword");//mozda ne treba ali zbog bolje zastite
            user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userLoggedInJson"));

            ab.Title = user.FirstName + " " + user.LastName;
            ab.SetIcon(Resource.Drawable.app_bar_logov2);

            if (user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(user.ProfilePictureData, 0, user.ProfilePictureData.Length).Result;
                profilePictureNavigationHeader.SetImageBitmap(bm);
            }





            if (navigationView != null)
            {
                SetUpDrawerContent(navigationView);
            }

            navigationView.GetHeaderView(0).FindViewById<Android.Support.V7.Widget.AppCompatTextView>(Resource.Id.nav_header_username_textView).Text = user.Username;
            tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            viewPager = FindViewById<ViewPager>(Resource.Id.viewpager);
            viewPager.OffscreenPageLimit = 4; //jer po defaultu je 2 i onda uvek se pravi novi fragment i opet uzima gps koordinate, ovako je u memoriji vise fragmenta, ali ne poziva svaki cas server

            SetUpViewPager(viewPager);

            tabs.SetupWithViewPager(viewPager);


            fab_menu_refresh = FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_refresh);
            fab_menu_automatically = FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_automatically);
            fab_menu_gps = FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_gps);
            fab_menu = FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu);

            fab_menu_refresh.Click += Fab_menu_refresh_Click;
            fab_menu_automatically.Click += Fab_menu_automatically_Click;
            fab_menu_gps.Click += Fab_menu_gps_Click;
        }


        private void Fab_menu_gps_Click(object sender, EventArgs e)
        {
            
            Snackbar.Make(fab_menu, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Clicked on GPS</font>"), Snackbar.LengthLong).Show();
        }

        private void Fab_menu_automatically_Click(object sender, EventArgs e)
        {
            Snackbar.Make(fab_menu, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Clicked on Automatically</font>"), Snackbar.LengthLong).Show();

        }

        private void Fab_menu_refresh_Click(object sender, EventArgs e)
        {
            Snackbar.Make(fab_menu, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Clicked on Refresh</font>"), Snackbar.LengthLong).Show();

        }
        private void SetUpViewPager(ViewPager viewPager)
        {
            /*TabAdapter */adapter = new TabAdapter(SupportFragmentManager);
            adapter.AddFragment(new Fragment1_Private(), "Private");
            adapter.AddFragment(new Fragment1_Private(), "Business");
            adapter.AddFragment(new Fragment3_Celebrity(), "Celebrity");
            adapter.AddFragment(new Fragment3_Celebrity(), "Celebrity2");
            viewPager.Adapter = adapter;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {//kada se klikne na hamburger, sta se dogadja
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    drawerLayout.OpenDrawer((int)GravityFlags.Left);
                    return true;

                
                default:
                    return base.OnOptionsItemSelected(item);
            }
           
        }
        private void SetUpDrawerContent(NavigationView navigationView)
        {
            navigationView.NavigationItemSelected += (object sender, NavigationView.NavigationItemSelectedEventArgs e) =>
             {
                 

                 e.MenuItem.SetChecked(false); //za sada mi ne treba, jer kada se otvori neki drugi activity hamburger menu nestaje, pa nije potrebno highlightovano gde se nalazimo u navigaciji
                 
                 switch (e.MenuItem.ItemId)
                 {
                     case Resource.Id.nav_private: //kada se klikne na private user edit info
                         if (Reachability.isOnline(this))
                         {
                             System.String restUriString = GetString(Resource.String.server_ip_getUserLogin) + user.Username + "/" + userPassword;
                             uri = new Uri(restUriString);
                             webClientUserPrivateDataUponOpeningEditDataActivity.DownloadDataAsync(uri);
                         }
                         else
                         {
                             Snackbar.Make(fab_menu, Android.Text.Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();
                         }
                      break;


                     case Resource.Id.nav_business: //kada se klikne na business user edit info
                         if (Reachability.isOnline(this))
                         {
                             System.String restUriString = GetString(Resource.String.server_ip_getBusiness) + user.Id;
                             uri = new Uri(restUriString);
                             webClientUserBusinessDataUponOpeningEditDataActivity.DownloadDataAsync(uri);
                         }
                         else
                         {
                             Snackbar.Make(fab_menu, Android.Text.Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();
                         }
                     break;

                     case Resource.Id.nav_celebrity: //kada se klikne na celebrity user edit info
                         Context context22 = navigationView.Context;
                         Intent intent22 = new Intent(context22, typeof(EditCelebrityUserInfoActivity));
                         context22.StartActivity(intent22);
                         break;

                     case Resource.Id.nav_logout: //kada se klikne na logout
                         this.Finish();
                         Context context = navigationView.Context;
                         Intent intent = new Intent(context, typeof(MainActivity));
                         context.StartActivity(intent);
                         break;

                     case Resource.Id.nav_about: //kada se klikne na about
                         Context context2 = navigationView.Context;
                         Intent intent2 = new Intent(context2, typeof(AboutActivity));
                         context2.StartActivity(intent2);
                         break;

                 }


                 drawerLayout.CloseDrawers();
             };

            
        }


        private void WebClientUserPrivateDataUponOpeningEditDataActivity_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno
                Console.WriteLine("*******Error webclient data save changes error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(fab_menu, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();

            }
            else
            {              
                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(jsonResult);//menjamo usera koji je stigao iz signIn sa novim updateovanim

                Context context = navigationView.Context;
                Intent intent = new Intent(context, typeof(EditPrivateUserInfoActivity));
                string userSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(user);
                intent.PutExtra("userLoggedInJson", userSerialized);
                
                context.StartActivity(intent);
            }
        }

        private void WebClientUserBusinessDataUponOpeningEditDataActivity_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno
                Console.WriteLine("*******Error webclient data save changes error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(fab_menu, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                business = Newtonsoft.Json.JsonConvert.DeserializeObject<Business>(jsonResult);//ZBOG DEBUGA

                Context context = navigationView.Context;
                Intent intent = new Intent(context, typeof(EditBusinessUserInfoActivity));
                string userSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(business);
                intent.PutExtra("businessLoggedInJson", userSerialized);

                context.StartActivity(intent);
            }
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


        public static void CheckVisibilityMode() //ovde treba dodati i za business i celebrity eventualno...
        {
            if (Fragment1_Private.switchVisibility.Checked) visibilityMode = 1;
            else visibilityMode = 0;
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


        //fab.Click += (o, e) => //o is sender, sender is button, button is a view
        //{
        //    //adapter.AddFragment(new Fragment3_Celebrity(), "TEST");
        //   // adapter.Fragments[1]=new Fragment3_Celebrity();
        //   // adapter.NotifyDataSetChanged();
        //    View anchor = o as View;
        //    Snackbar.Make(anchor, "Yay Snackbar!!", Snackbar.LengthLong).SetAction("Action", v =>
        //    {
        //        //Do something here
        //        //Intent intent new Intent();
        //    }).Show();
        //};

    }





}