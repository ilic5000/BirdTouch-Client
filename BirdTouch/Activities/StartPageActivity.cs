using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using BirdTouch.Adapters;
using BirdTouch.Constants;
using BirdTouch.Fragments;
using BirdTouch.Helpers;
using BirdTouch.Models;
using Com.Github.Amlcurran.Showcaseview;
using Com.Github.Amlcurran.Showcaseview.Targets;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using static Android.Support.Design.Widget.TabLayout;
using SupportActionBar = Android.Support.V7.App.ActionBar;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;

namespace BirdTouch.Activities
{
    [Activity(Label = "StartPageActivity", Theme = "@style/Theme.DesignDemo")]
    public class StartPageActivity : AppCompatActivity, View.IOnClickListener
    {
        public static SupportActionBar actionBar;
        public static ImageView profilePictureNavigationHeader;
        public static TabAdapter adapter;

        public static byte[] picDataProfileNavigation;

        public static UserInfoModel user;
        public static Bitmap navBitmap;

        private DrawerLayout _drawerLayout;
        private NavigationView _navigationView;
        private SupportToolbar _toolBar;
        private TabLayout _tabs;
        private ViewPager _viewPager;

        private BusinessInfoModel _businessInfo;

        private WebClient _webClientUserPrivateDataUponOpeningEditDataActivity;
        private WebClient _webClientUserBusinessDataUponOpeningEditDataActivity;

        private ShowcaseView _showcaseView;
        private int _countShowcase = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Create view
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.StartPage);

            user = Newtonsoft.Json.JsonConvert.DeserializeObject
             <UserInfoModel>(Intent.GetStringExtra(IntentConstants.LOGGEDINUSER));

            // Initialize web clients
            _webClientUserPrivateDataUponOpeningEditDataActivity = new WebClient();
            _webClientUserBusinessDataUponOpeningEditDataActivity = new WebClient();

            // Set up events for web clients
            _webClientUserPrivateDataUponOpeningEditDataActivity.DownloadDataCompleted +=
                WebClientUserPrivateDataUponOpeningEditDataActivity_DownloadDataCompleted;
            _webClientUserBusinessDataUponOpeningEditDataActivity.DownloadDataCompleted +=
                WebClientUserBusinessDataUponOpeningEditDataActivity_DownloadDataCompleted;

            // Find components
            _toolBar = FindViewById<SupportToolbar>(Resource.Id.toolBar);
            SetSupportActionBar(_toolBar);
            _navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            profilePictureNavigationHeader = _navigationView.GetHeaderView(0)
                .FindViewById<ImageView>(Resource.Id.nav_header_imgViewHeader);

            // Set up action bar
            actionBar = SupportActionBar;

            // Set up hamburger icon indicator
            actionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);

            // Enable it for home button
            actionBar.SetDisplayHomeAsUpEnabled(true);

            actionBar.Title = user.FirstName + " " + user.LastName;
            actionBar.SetIcon(Resource.Drawable.app_bar_logov2);

            if (user.ProfilePictureData != null)
            {
                Bitmap bm = BitmapFactory.DecodeByteArrayAsync(
                    user.ProfilePictureData,
                    0,
                    user.ProfilePictureData.Length)
                     .Result;

                profilePictureNavigationHeader.SetImageBitmap(bm);
            }

            if (_navigationView != null)
            {
                SetUpDrawerContent(_navigationView);
            }

            // Set username in drawer header
            _navigationView.GetHeaderView(0).FindViewById
                <Android.Support.V7.Widget.AppCompatTextView>
                (Resource.Id.nav_header_username_textView)
                 .Text = user.Username;

            _tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            _viewPager = FindViewById<ViewPager>(Resource.Id.viewpager);

            // TODO: Investigate what does this mean exactly
            _viewPager.OffscreenPageLimit = 5;

            // Setup fragments
            SetUpViewPager(_viewPager);
            _tabs.SetupWithViewPager(_viewPager);

            // First start of the app for this user
            // If first start, then it needs to show guide, but next time, guide will not be shown
            // pref.GetBoolean returns true if cannot find value in preferences
            // Be careful with childview, if changed, then order in guide will be wrong, or something can be null
            ISharedPreferences pref = ApplicationContext.GetSharedPreferences("FirstTimeRun", FileCreationMode.Private);
            if (pref.GetBoolean(user.Username + "FirstRodeo", true))
            {
                var target = new ViewTarget(((SlidingTabStrip)_tabs.GetChildAt(0)).GetChildAt(0));

                _showcaseView = new ShowcaseView.Builder(this)
                  .SetTarget(target)
                  .SetContentTitle("Private users")
                  .SetContentText("Here you can see all users who are currently using application for social purposes")
                  .SetStyle(Resource.Style.CustomShowcaseTheme)
                  .HideOnTouchOutside()
                  .Build();

                //this because this activity inherits ionclicklistener
                // for more logic, please see View.IOnClickListener.OnClick in this file
                _showcaseView.OverrideButtonClick(this);

                // Disable show guide for next time login
                pref.Edit().PutBoolean(user.Username + "FirstRodeo", false).Commit();
            }
        }

        /// <summary>
        /// Updates image on navigation menu from some other activity
        /// </summary>
        public static void UpdateProfileImage()
        {
            if (picDataProfileNavigation != null)
            {
                var bm = BitmapFactory.DecodeByteArrayAsync(
                    picDataProfileNavigation,
                    0,
                    picDataProfileNavigation.Length)
                     .Result;

                profilePictureNavigationHeader.SetImageBitmap(bm);
            }
        }

        /// <summary>
        /// Create fragments for view pager used on start page
        /// </summary>
        /// <param name="viewPager"></param>
        private void SetUpViewPager(ViewPager viewPager)
        {
            adapter = new TabAdapter(SupportFragmentManager);

            adapter.AddFragment(new Fragment1_Private(), "Private");
            adapter.AddFragment(new Fragment1_PrivateSavedUsers(), "Saved private");
            adapter.AddFragment(new Fragment2_Business(), "Business");
            adapter.AddFragment(new Fragment2_BusinessSavedUsers(), "Saved business");

            // TODO: Add later on
            // adapter.AddFragment(new Fragment3_Celebrity(), "Celebrity");

            viewPager.Adapter = adapter;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            // When clicked on hamburger icon
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    _drawerLayout.OpenDrawer((int)GravityFlags.Left);
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void SetUpDrawerContent(NavigationView navigationView)
        {
            navigationView.NavigationItemSelected += (object sender, NavigationView.NavigationItemSelectedEventArgs e) =>
             {
                 // Disable highlighting of current/selected activities
                 e.MenuItem.SetChecked(false);

                 switch (e.MenuItem.ItemId)
                 {
                     // When clicked on private user edit info
                     case Resource.Id.nav_private:
                         if (Reachability.IsOnline(this))
                         {
                             var uri = WebApiUrlGenerator
                                .GenerateWebApiUrl(Resource.String.webapi_endpoint_getPrivateInfo);

                             _webClientUserPrivateDataUponOpeningEditDataActivity.Headers.Clear();
                             _webClientUserPrivateDataUponOpeningEditDataActivity.Headers.Add(
                                HttpRequestHeader.Authorization,
                                "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(ApplicationContext));

                             _webClientUserPrivateDataUponOpeningEditDataActivity.DownloadDataAsync(uri);
                         }
                         else
                         {
                             Snackbar.Make(
                                 navigationView,
                                 Android.Text.Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                                 Snackbar.LengthLong)
                                  .Show();
                         }
                         break;

                     // When clicked on business user edit info
                     case Resource.Id.nav_business:
                         if (Reachability.IsOnline(this))
                         {
                             System.String restUriString = GetString(Resource.String.webapi_endpoint_getBusiness) + user.Id;
                             var uri = new Uri(restUriString);
                             _webClientUserBusinessDataUponOpeningEditDataActivity.DownloadDataAsync(uri);
                         }
                         else
                         {
                             Snackbar.Make(
                                 navigationView,
                                 Android.Text.Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                                 Snackbar.LengthLong)
                                  .Show();
                         }
                         break;

                     // When clicked on celebrity user edit info
                     case Resource.Id.nav_celebrity:
                         Context context22 = navigationView.Context;
                         Intent intent22 = new Intent(context22, typeof(EditCelebrityUserInfoActivity));
                         context22.StartActivity(intent22);
                         break;

                     // When clicked on about
                     case Resource.Id.nav_about:
                         Context context2 = navigationView.Context;
                         Intent intent2 = new Intent(context2, typeof(AboutActivity));
                         context2.StartActivity(intent2);
                         break;

                     // When clicked on logout
                     case Resource.Id.nav_logout:
                         JwtTokenHelper.RemoveTokenFromSharedPreferences(ApplicationContext);
                         this.Finish();
                         Context context = navigationView.Context;
                         Intent intent = new Intent(context, typeof(MainActivity));
                         context.StartActivity(intent);
                         break;
                 }

                 _drawerLayout.CloseDrawers();
             };
        }

        private void WebClientUserPrivateDataUponOpeningEditDataActivity_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add error type
                Snackbar.Make(_navigationView,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                string jsonResult = Encoding.UTF8.GetString(e.Result);

                // Update global user object
                // TODO: HOW TO HANDLE JWT
                user = Newtonsoft.Json.JsonConvert.DeserializeObject<UserInfoModel>(jsonResult);

                Context context = _navigationView.Context;
                Intent intent = new Intent(context, typeof(EditPrivateUserInfoActivity));
                string userSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(user);


                intent.PutExtra(IntentConstants.LOGGEDINUSER, userSerialized);
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
                Snackbar.Make(_navigationView, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                _businessInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<BusinessInfoModel>(jsonResult);//ZBOG DEBUGA

                Context context = _navigationView.Context;
                Intent intent = new Intent(context, typeof(EditBusinessUserInfoActivity));
                string userSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(_businessInfo);
                intent.PutExtra("businessLoggedInJson", userSerialized);

                context.StartActivity(intent);
            }
        }

        /// <summary>
        /// When clicked on back button on phone
        /// </summary>
        public override void OnBackPressed()
        {
            // Minimizes app
            // TODO: Decide if this is the best way to handle back
            MoveTaskToBack(true);
        }

        /// <summary>
        /// Guide for new users
        /// My remarks, cannot understand them right now:
        /// "mora ovako jer c# ne dozvoljava pravljenje OnClickListenera unutar OverrideButtonClick kao sto bi trebalo.
        /// Zato sam nasledio taj interfejs i ovde ga overridovao i onda u overridebuttonclick ga povezem preko this."
        /// </summary>
        /// <param name="v"></param>
        void View.IOnClickListener.OnClick(View v)
        {
            _countShowcase++;
            _showcaseView.Hide();
            switch (_countShowcase)
            {
                case 1:
                    var target = new ViewTarget(((SlidingTabStrip)_tabs.GetChildAt(0)).GetChildAt(1));
                    _showcaseView = new ShowcaseView.Builder(this)
                        .SetTarget(target)
                        .SetContentTitle("Saved private users")
                        .SetContentText("You can save private users to have them for future use. Here you can find all private users that you saved. You can see them anytime.")
                        .SetStyle(Resource.Style.CustomShowcaseTheme)
                        .HideOnTouchOutside()
                        .Build();
                    _showcaseView.OverrideButtonClick(this);
                    break;

                case 2:
                    target = new ViewTarget(((SlidingTabStrip)_tabs.GetChildAt(0)).GetChildAt(2));
                    _showcaseView = new ShowcaseView.Builder(this)
                      .SetTarget(target)
                      .SetContentTitle("Business users")
                      .SetContentText("Here you can see all users who are currently using application for business purposes. If some user is using application for both social and business purposes, in this tab you will only see his business info.")
                      .SetStyle(Resource.Style.CustomShowcaseTheme)
                      .HideOnTouchOutside()
                      .Build();
                    _showcaseView.OverrideButtonClick(this);
                    break;

                case 3:
                    target = new ViewTarget(((SlidingTabStrip)_tabs.GetChildAt(0)).GetChildAt(3));
                    _showcaseView = new ShowcaseView.Builder(this)
                      .SetTarget(target)
                      .SetContentTitle("Saved business users")
                      .SetContentText("You can save business users to have them for future use. Here you can find all business users that you saved. You can see them anytime.")
                      .SetStyle(Resource.Style.CustomShowcaseTheme)
                      .HideOnTouchOutside()
                      .Build();
                    _showcaseView.OverrideButtonClick(this);
                    break;

                case 4:
                    target = new ViewTarget(_viewPager.GetChildAt(0).
                        FindViewById<SwitchCompat>(Resource.Id.activatePrivateSwitch));
                    _showcaseView = new ShowcaseView.Builder(this)
                      .SetTarget(target)
                      .SetContentTitle("Be visible")
                      .SetContentText("Make yourself visible to others. You can also scan for nearby Birdtouch users. Remember: if you are not visible, they cannot find you. Also, you cannot find them.")
                      .SetStyle(Resource.Style.CustomShowcaseTheme)
                      .HideOnTouchOutside()
                      .Build();
                    _showcaseView.OverrideButtonClick(this);
                    break;

                case 5:
                    target = new ViewTarget(_toolBar.GetChildAt(1));
                    _showcaseView = new ShowcaseView.Builder(this)
                      .SetTarget(target)
                      .SetContentTitle("Update your information")
                      .SetContentText("Change your personal/business information")
                      .SetStyle(Resource.Style.CustomShowcaseThemeFinalShowcase)
                      .HideOnTouchOutside()
                      .Build();
                    break;

                default:
                    break;
            }
        }
    }
}