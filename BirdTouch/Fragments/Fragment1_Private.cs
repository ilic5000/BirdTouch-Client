using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using SupportFragment = Android.Support.V4.App.Fragment;
using Android.Support.V7.Widget;
using Android.Graphics;
using Android.Content.Res;
using Android.Support.Design.Widget;
using Android.Locations;
using System.Linq;
using Android.Runtime;
using System.Net;
using System.Collections.Specialized;
using Android.Text;
using System.Text;
using BirdTouch.Models;
using Android.Views.Animations;
using BirdTouch.Helpers;
using BirdTouch.Activities;
using BirdTouch.Constants;
using BirdTouch.Extensions;
using BirdTouch.RecyclerViewCustom;

namespace BirdTouch.Fragments
{
    public class Fragment1_Private : SupportFragment, ILocationListener
    {
        public static SwitchCompat switchVisibility;

        private Clans.Fab.FloatingActionButton _fab_menu_refresh;
        private Clans.Fab.FloatingActionButton _fab_menu_gps;
        private Clans.Fab.FloatingActionMenu _fabMenu;

        private FrameLayout _frameLay;
        private LinearLayout _linearLayout;
        private RecyclerView _recycleView;

        private Location _currLocation;
        private LocationManager _locationManager;
        String _locationProvider;
        // in ms
        private long _locationTimeIntervalForChecking;
        // in meter
        private float _locationDistanceNeededForUpdateToTrigger;

        private ProgressBar _progressBarLocation;
        private ProgressBar _progressBarGetPrivateUsers;

        private WebClient _webClientMakeUserVisible;
        private WebClient _webClientMakeUserInvisible;
        private WebClient _webClientGetPrivateUsersNearMe;

        private bool _visible;
        private bool _gpsUpdateIndeterminate;

        private List<UserInfoModel> _listOfUsersAroundMe;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _visible = false;
            _gpsUpdateIndeterminate = false;
            _listOfUsersAroundMe = new List<UserInfoModel>();

            _locationTimeIntervalForChecking = 0;
            _locationDistanceNeededForUpdateToTrigger = 15;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inflate view with resource layout
            View view = inflater.Inflate(Resource.Layout.Fragment1_private, container, false);

            // Get location manager
            _locationManager = (LocationManager)Activity.GetSystemService(Context.LocationService);

            // Find components
            _recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewPrivate);
            _progressBarLocation = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetLocation);
            _progressBarGetPrivateUsers = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetPrivateUsers);
            _frameLay = view.FindViewById<FrameLayout>(Resource.Id.coordinatorLayoutPrivate);
            _linearLayout = view.FindViewById<LinearLayout>(Resource.Id.fragment1LinearLayoutWrapper);
            switchVisibility = view.FindViewById<SwitchCompat>(Resource.Id.activatePrivateSwitch);
            _fab_menu_refresh = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_refresh_private);
            _fab_menu_gps = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_gps_private);
            _fabMenu = view.FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu_private);
            _fabMenu.Visibility = ViewStates.Gone;

            // Initialize web clients
            _webClientMakeUserVisible = new WebClient();
            _webClientMakeUserInvisible = new WebClient();
            _webClientGetPrivateUsersNearMe = new WebClient();

            // Register events for web clients
            _webClientMakeUserVisible.UploadStringCompleted += WebClientMakeUserVisible_UploadStringCompleted;
            _webClientMakeUserInvisible.UploadStringCompleted += WebClientMakeUserInvisible_UploadStringCompleted;
            _webClientGetPrivateUsersNearMe.DownloadDataCompleted += WebClientGetPrivateUsersNearMe_DownloadDataCompleted;

            // Register events for components
            _fabMenu.MenuToggle += Fab_menu_MenuToggle;
            _fab_menu_gps.Click += Fab_menu_gps_Click;
            _fab_menu_refresh.Click += Fab_menu_refresh_Click;
            switchVisibility.CheckedChange += SwitchVisibility_CheckedChange;

            // Initialize recycle view (although its empty at the moment)
            SetUpRecyclerView(_recycleView, _listOfUsersAroundMe);

            // Add custom scroll listener for the recycler view (for hiding/showing fab menu button)
            _recycleView.AddOnScrollListener(new OnScrollListenerCustom(_fabMenu));

            return view;
        }

        private void Fab_menu_MenuToggle(object sender, Clans.Fab.FloatingActionMenu.MenuToggleEventArgs e)
        {
            // When fab menu is opened, we hide recycle view
            // TODO: Maybe there is better way to handle uneccesary clicks on recycle view, maybe this is overkill
            if (e.Opened)
            {
                _linearLayout.Click += LinearLayoutClick;
                _recycleView.Visibility = ViewStates.Invisible;
            }
            else
            {
                _linearLayout.Click -= LinearLayoutClick;
                _recycleView.Visibility = ViewStates.Visible;
            }
        }

        private void LinearLayoutClick(object sender, EventArgs e)
        {
            _fabMenu.Close(true);
        }

        private void Fab_menu_gps_Click(object sender, EventArgs e)
        {
            // If gps update is already in process, we will cancel it
            if (_gpsUpdateIndeterminate)
            {
                _fab_menu_gps.SetIndeterminate(false);
                _locationManager.RemoveUpdates(this);
                _gpsUpdateIndeterminate = false;
            }
            else
            {
                _gpsUpdateIndeterminate = true;
                _fab_menu_gps.SetIndeterminate(true);
                _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
            }
        }

        // TODO: Implement automatically update gps location
        private void Fab_menu_automatically_Click(object sender, EventArgs e)
        {
            _fabMenu.Close(true);
        }

        private void Fab_menu_refresh_Click(object sender, EventArgs e)
        {
            GetPrivateUsersNearMe();
            _fabMenu.Close(true);
        }

        private void SwitchVisibility_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            // Visibility on
            if (e.IsChecked)
            {
                if (string.IsNullOrEmpty(StartPageActivity.user.FirstName)
                    && string.IsNullOrEmpty(StartPageActivity.user.LastName))
                {
                    Snackbar.Make(
                        _frameLay,
                        Html.FromHtml("<font color=\"#ffffff\">You need to set at least first and last name for your account.</font>"),
                        Snackbar.LengthLong)
                         .Show();
                    switchVisibility.Checked = false;

                    return;
                }

                // Everytime we switch visibility on, we check if we can get location provider (gps or network)
                _locationProvider = LocationHelper.TryToFindLocationProvider(_locationManager, Activity);

                // If there are some location providers (set in InitializeLocationManager), then we proceed to make user visible
                if (!_locationProvider.Equals(string.Empty))
                {
                    _locationManager.RequestLocationUpdates(
                        _locationProvider,
                        _locationTimeIntervalForChecking,
                        _locationDistanceNeededForUpdateToTrigger,
                        this);

                    _progressBarLocation.Visibility = ViewStates.Visible;
                }
                else
                {
                    Snackbar.Make(
                        _frameLay,
                        Html.FromHtml("<font color=\"#ffffff\">No GPS or Network Geolocation available</font>"),
                        Snackbar.LengthLong)
                         .Show();
                }
            }
            // Visibility off
            else
            {
                _fabMenu.Visibility = ViewStates.Gone;
                _locationManager.RemoveUpdates(this);
                _progressBarLocation.Visibility = ViewStates.Invisible;
                GoInvisible();
            }
        }

        public void OnLocationChanged(Location location)
        {
            _currLocation = location;

            if (location == null)
            {
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">Unable to determine location</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                SendLocationToDatabase();
            }

            // TODO: Maybe enable refreshing all the time
            // Remove listening for location change, we dont want to refresh all the time
            _locationManager.RemoveUpdates(this);
        }

        private void SendLocationToDatabase()
        {
            if (Reachability.IsOnline(Activity)
                && !_webClientMakeUserVisible.IsBusy)
            {
                var userLocationUpdate = new UserLocationUpdate()
                {
                    ActiveMode = ActiveModeConstants.PRIVATE,
                    LocationLatitude = _currLocation.Latitude,
                    LocationLongitude = _currLocation.Longitude
                };

                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_makeUserVisible);

                _webClientMakeUserVisible.Headers.Clear();
                _webClientMakeUserVisible.Headers.Add(
                                HttpRequestHeader.Authorization,
                                "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(Context));
                _webClientMakeUserVisible.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                _webClientMakeUserVisible.UploadStringAsync(uri, "POST", userLocationUpdate.ToJson());
            }
            else
            {
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
        }

        private void WebClientMakeUserVisible_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add error type
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();

                _visible = false;
                _fab_menu_gps.SetIndeterminate(false);
                _fabMenu.Close(true);
            }
            else
            {
                _visible = true;
                _progressBarLocation.Visibility = ViewStates.Invisible;
                _fab_menu_gps.SetIndeterminate(false);
                _gpsUpdateIndeterminate = false;
                _fabMenu.Close(true);

                // If location is succesfully updated
                GetPrivateUsersNearMe();
            }
        }

        private void GoInvisible()
        {
            if (Reachability.IsOnline(Activity)
                && !_webClientMakeUserInvisible.IsBusy)
            {
                var userLocationUpdate = new UserLocationUpdate()
                {
                    ActiveMode = ActiveModeConstants.PRIVATE
                };

                var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_makeUserInvisible);

                _webClientMakeUserInvisible.Headers.Clear();
                _webClientMakeUserInvisible.Headers.Add(
                                HttpRequestHeader.Authorization,
                                "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(Context));
                _webClientMakeUserInvisible.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                _webClientMakeUserInvisible.UploadStringAsync(uri, "DELETE", userLocationUpdate.ToJson());
            }
            else
            {
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
        }

        private void WebClientMakeUserInvisible_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
                _visible = true;
            }
            else
            {
                _visible = false;
            }
        }

        private void GetPrivateUsersNearMe()
        {
            if (Reachability.IsOnline(Activity)
                && !_webClientGetPrivateUsersNearMe.IsBusy)
            {
                // You can get other users near you only if you are visible
                if (_visible)
                {
                    _progressBarGetPrivateUsers.Visibility = ViewStates.Visible;

                    var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_getPrivateUsersNearMe);

                    _webClientGetPrivateUsersNearMe.Headers.Clear();
                    _webClientGetPrivateUsersNearMe.Headers.Add(
                                HttpRequestHeader.Authorization,
                                "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(Context));

                    NameValueCollection parameters = new NameValueCollection();
                    parameters.Add("activeMode", Constants.ActiveModeConstants.PRIVATE);
                    parameters.Add("radiusOfSearch",
                                   SearchRadiusSettingsHelper.GetSearchRadiusInKm(Context).ToString());

                    _webClientGetPrivateUsersNearMe.QueryString.Clear();
                    _webClientGetPrivateUsersNearMe.QueryString.Add(parameters);
                    _webClientGetPrivateUsersNearMe.DownloadDataAsync(uri);
                }
                else
                {
                    Snackbar.Make(
                        _frameLay,
                        Html.FromHtml("<font color=\"#ffffff\">You are not visible to others</font>"),
                        Snackbar.LengthLong)
                         .Show();
                }
            }
            else
            {
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
        }

        private void WebClientGetPrivateUsersNearMe_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add error type
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
                _progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
            }
            else
            {
                List<UserInfoModel> newListOfUsersAroundMe = Newtonsoft.Json.JsonConvert.DeserializeObject
                                                                <List<UserInfoModel>>(Encoding.UTF8.GetString(e.Result));

                SetUpRecyclerView(_recycleView, newListOfUsersAroundMe);

                _fabMenu.Visibility = ViewStates.Visible;
                _progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
            }
        }

        //LocationListener interface
        // TODO: Maybe implement in future
        public void OnProviderDisabled(string provider)
        {
            Snackbar.Make(
                _frameLay,
                Html.FromHtml("<font color=\"#ffffff\">" + provider + " is disabled</font>"),
                Snackbar.LengthLong)
                 .Show();
        }

        // TODO: Maybe implement in future
        public void OnProviderEnabled(string provider)
        {
            Snackbar.Make(
                _frameLay,
                Html.FromHtml("<font color=\"#ffffff\">" + provider + " is enabled</font>"),
                Snackbar.LengthLong)
                 .Show();
        }

        // TODO: Maybe implement in future
        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {

        }

        //public override void OnPause()
        //{
        //    base.OnPause();
        //    locationManager.RemoveUpdates(this);
        //}

        //public override void OnResume()
        //{
        //    base.OnResume();
        //    locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
        //}
        //*********************************************

        //RecycleView setup
        private void SetUpRecyclerView(RecyclerView recyclerView, List<UserInfoModel> listOfUsersAroundMe)
        {
            recyclerView.SetLayoutManager(
                new LinearLayoutManager(recyclerView.Context));

            recyclerView.SetAdapter(
                new RecyclerViewAdapter(
                    recyclerView.Context,
                    listOfUsersAroundMe,
                    Activity.Resources,
                    _recycleView));
        }

        public void NotifyDataSetChangedFromAnotherFragment()
        {
            _recycleView.GetAdapter().NotifyDataSetChanged();
        }

        public class RecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue _typedValue = new TypedValue();
            private int _background;
            private List<UserInfoModel> _values;
            private RecyclerView _recycleView;
            Resources _resource;

            public RecyclerViewAdapter(Context context, List<UserInfoModel> items, Resources res, RecyclerView rv)
            {
                context.Theme.ResolveAttribute(
                    Resource.Attribute.selectableItemBackground,
                    _typedValue,
                    true);

                _background = _typedValue.ResourceId;
                _values = items;
                _resource = res;
                _recycleView = rv;
            }

            public override int ItemCount
            {
                get
                {
                    return _values.Count;
                }
            }

            public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var simpleHolder = holder as ViewHolder;

                simpleHolder._boundString = _values[position].Id.ToString();

                if (string.IsNullOrEmpty(_values[position].FirstName)
                    && string.IsNullOrEmpty(_values[position].LastName)
                    && string.IsNullOrEmpty(_values[position].Description))
                {
                    simpleHolder._txtViewDescription.Text = "waiting on a user info update";
                }
                else
                {
                    simpleHolder._txtViewName.Text = _values[position].FirstName + " " + _values[position].LastName;
                    simpleHolder._txtViewDescription.Text = _values[position].Description;
                }

                if (_values[position].ProfilePictureData != null)
                {
                    Bitmap bm = BitmapFactory.DecodeByteArrayAsync(
                        _values[position].ProfilePictureData,
                        0,
                        _values[position].ProfilePictureData.Length)
                         .Result;

                    // TODO: Maybe this parameters can be played with
                    simpleHolder._profileImageView.SetImageBitmap(
                        Bitmap.CreateScaledBitmap(
                            bm,
                            200,
                            100,
                            false));
                }
                else
                {
                    // If user does not have custom profile image
                    BitmapFactory.Options options = new BitmapFactory.Options();
                    options.InJustDecodeBounds = true;

                    BitmapFactory.DecodeResource(
                        _resource,
                        Resource.Drawable.blank_navigation,
                        options);

                    options.InSampleSize = BitmapHelper.CalculateInSampleSize(options, 100, 100);
                    options.InJustDecodeBounds = false;

                    var bitMap = await BitmapFactory.DecodeResourceAsync(
                        _resource,
                        Resource.Drawable.blank_navigation,
                        options);

                    simpleHolder._profileImageView.SetImageBitmap(bitMap);
                }

                // In order to not stack delegats
                simpleHolder._view.Click -= MView_Click;
                simpleHolder._view.Click += MView_Click;

                // TODO: Animation are causing troubles, maybe investigate when have more time
                // Random rand = new Random();
                //if(rand.Next() % 2 == 1)
                // setScaleAnimation(holder.ItemView);
                //else
                //setFadeAnimation(holder.ItemView);

                // Needs to be here in order to prevent lines below to trigger event listener
                simpleHolder._checkbox.CheckedChange -= Checkbox_CheckedChange;

                if (IsUserInSavedContacts(_values[position].Id, simpleHolder))
                {
                    simpleHolder._checkbox.Checked = true;
                }
                else
                {
                    simpleHolder._checkbox.Checked = false;
                }

                // Needed because we need position in adapter in Checkbox_CheckedChange
                simpleHolder._checkbox.Tag = simpleHolder._view;
                simpleHolder._checkbox.CheckedChange += Checkbox_CheckedChange;
            }


            private bool IsUserInSavedContacts(Guid userIdRecyclerView, ViewHolder svh)
            {
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = svh.ItemView.Context
                    .ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                List<UserInfoModel> listSavedPrivateUsers = new List<UserInfoModel>();

                if (pref.Contains("SavedPrivateUsersDictionary"))
                {
                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        // If user has already added some users to saved
                        if (dictionary.ContainsKey(userId))
                        {
                            if (dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.PRIVATE)))
                            {
                                listSavedPrivateUsers = dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)];
                            }
                        }
                    }
                }

                if (!(listSavedPrivateUsers.Find(a => a.Id == userIdRecyclerView) == null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private void Checkbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                CheckBox vsender = sender as CheckBox;

                View mView = (View)vsender.Tag;
                int position = _recycleView.GetChildAdapterPosition(mView);
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = vsender.Context.ApplicationContext
                    .GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                if (e.IsChecked)
                {
                    if (!pref.Contains("SavedPrivateUsersDictionary"))
                    {
                        var dictionary = new Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>();

                        dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                        dictionary[userId].Add(int.Parse(ActiveModeConstants.PRIVATE), new List<UserInfoModel>());
                        dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)].Add(_values[position]);

                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment =
                            (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDPRIVATE));
                        refToSavedUsersFragment.SetUpRecyclerView();
                    }
                    else
                    {
                        string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                        if (serializedDictionary != String.Empty)
                        {
                            var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                                 <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                            if (!dictionary.ContainsKey(userId))
                            {
                                dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                            }
                            if (!dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.PRIVATE)))
                            {
                                dictionary[userId].Add(int.Parse(ActiveModeConstants.PRIVATE), new List<UserInfoModel>());
                            }

                            // Add private user from recycler view
                            dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)].Add(_values[position]);
                            edit.Remove("SavedPrivateUsersDictionary");
                            edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                            edit.Apply();
                            Fragment1_PrivateSavedUsers refToSavedUsersFragment =
                                (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDPRIVATE));
                            refToSavedUsersFragment.SetUpRecyclerView();
                        }
                    }
                }
                else
                {
                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)].RemoveAll(a => a.Id == _values[position].Id);
                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment =
                            (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDPRIVATE));
                        refToSavedUsersFragment.SetUpRecyclerView();
                    }
                }
            }

            private void SetFadeAnimation(View view)
            {
                int FADE_DURATION = 1400; // in milliseconds
                AlphaAnimation anim = new AlphaAnimation(0.0f, 1.0f);
                anim.Duration = FADE_DURATION;
                view.StartAnimation(anim);
            }

            private void SetScaleAnimation(View view)
            {
                int FADE_DURATION = 1000; // in milliseconds
                ScaleAnimation anim = new ScaleAnimation(0.0f, 1.0f, 0.0f, 1.0f);
                anim.Duration = FADE_DURATION;
                view.StartAnimation(anim);
            }

            private void MView_Click(object sender, EventArgs e)
            {
                int position = _recycleView.GetChildAdapterPosition((View)sender);
                ViewHolder svh = (ViewHolder)_recycleView.GetChildViewHolder((View)sender);

                Context context = _recycleView.Context;
                Intent intent = new Intent(context, typeof(UserDetailActivity));
                intent.PutExtra("userInformation", Newtonsoft.Json.JsonConvert.SerializeObject(_values[position]));
                intent.PutExtra("isSaved", (svh._checkbox.Checked));
                context.StartActivity(intent);
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                View view = LayoutInflater
                    .From(parent.Context).Inflate(Resource.Layout.List_Item, parent, false);
                view.SetBackgroundResource(_background);

                return new ViewHolder(view);
            }
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            public string _boundString;
            public readonly View _view;
            public readonly ImageView _profileImageView;
            public readonly TextView _txtViewName;
            public readonly TextView _txtViewDescription;
            public readonly CheckBox _checkbox;

            public ViewHolder(View view) : base(view)
            {
                _view = view;
                _profileImageView = view.FindViewById<ImageView>(Resource.Id.avatar);
                _txtViewName = view.FindViewById<TextView>(Resource.Id.text1);
                _txtViewDescription = view.FindViewById<TextView>(Resource.Id.text2);
                _checkbox = view.FindViewById<CheckBox>(Resource.Id.checkboxSaveUserRecycleViewRow);
            }

            public override string ToString()
            {
                return base.ToString() + " '" + _txtViewName.Text;
            }
        }
    }
}
