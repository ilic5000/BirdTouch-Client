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

namespace BirdTouch.Fragments
{
    public class Fragment2_Business : SupportFragment, ILocationListener
    {
        public static SwitchCompat switchVisibility;

        private Clans.Fab.FloatingActionButton _fab_menu_refresh;
        private Clans.Fab.FloatingActionButton _fab_menu_gps;
        private Clans.Fab.FloatingActionMenu _fab_menu;

        private FrameLayout _frameLay;
        private LinearLayout _linearLayout;
        private RecyclerView _recycleView;

        private Location _currLocation;
        private LocationManager _locationManager;
        private ProgressBar _progressBarLocation;
        private ProgressBar _progressBarGetBusinessUsers;
        string _locationProvider;
        // in ms
        private long _locationTimeIntervalForChecking;
        // in meter
        private float _locationDistanceNeededForUpdateToTrigger;
        // TODO: Implement setting page
        private double _radiusOfSearch = 0.5;

        private WebClient _webClientMakeUserVisible;
        private WebClient _webClientMakeUserInvisible;
        private WebClient _webClientGetBusinessUsersNearMe;

        private bool _visible = false;
        private bool _gpsUpdateIndeterminate = false;
        private List<BusinessInfoModel> _listOfUsersAroundMe;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _visible = false;
            _gpsUpdateIndeterminate = false;
            _listOfUsersAroundMe = new List<BusinessInfoModel>();

            _locationTimeIntervalForChecking = 0;
            _locationDistanceNeededForUpdateToTrigger = 15;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inflate view with resource layout
            View view = inflater.Inflate(Resource.Layout.Fragment2_business, container, false);

            // Get location manager
            _locationManager = (LocationManager)Activity.GetSystemService(Context.LocationService);

            // Find components
            _recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewBusiness);
            _progressBarLocation = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetLocationBusiness);
            _progressBarGetBusinessUsers = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetBusinessUsers);
            _frameLay = view.FindViewById<FrameLayout>(Resource.Id.coordinatorLayoutBusiness);
            _linearLayout = view.FindViewById<LinearLayout>(Resource.Id.fragment2LinearLayoutWrapper);
            switchVisibility = view.FindViewById<SwitchCompat>(Resource.Id.activateBusinessSwitch);
            _fab_menu_refresh = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_refresh_business);
            _fab_menu_gps = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_gps_business);
            _fab_menu = view.FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu_business);
            _fab_menu.Visibility = ViewStates.Gone;

            // Initialize web clients
            _webClientMakeUserVisible = new WebClient();
            _webClientMakeUserInvisible = new WebClient();
            _webClientGetBusinessUsersNearMe = new WebClient();

            // Register events for web clients
            _webClientMakeUserVisible.UploadStringCompleted += WebClientMakeUserVisible_UploadStringCompleted;
            _webClientMakeUserInvisible.UploadStringCompleted += WebClientMakeUserInvisible_UploadStringCompleted;
            _webClientGetBusinessUsersNearMe.DownloadDataCompleted += WebClientGetBusinessUsersNearMe_DownloadDataCompleted;

            // Register events for components
            _fab_menu_refresh.Click += Fab_menu_refresh_Click;
            _fab_menu_gps.Click += Fab_menu_gps_Click;
            _fab_menu.MenuToggle += Fab_menu_MenuToggle;
            switchVisibility.CheckedChange += SwitchVisibility_CheckedChange;

            // Initialize recycle view (although its empty at the moment)
            SetUpRecyclerView(_recycleView, _listOfUsersAroundMe);

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
            _fab_menu.Close(true);
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
            _fab_menu.Close(true);
        }

        private void Fab_menu_refresh_Click(object sender, EventArgs e)
        {
            GetBusinessUsersNearMe();
            _fab_menu.Close(true);
        }

        private void SwitchVisibility_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            // Visibility on
            if (e.IsChecked)
            {
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
                _fab_menu.Visibility = ViewStates.Gone;
                _locationManager.RemoveUpdates(this);
                GoInvisible();
                _progressBarLocation.Visibility = ViewStates.Invisible;
            }
        }

        public void OnLocationChanged(Location location)
        {
            _currLocation = location;

            if (this._currLocation == null)
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
                    ActiveMode = ActiveModeConstants.BUSINESS,
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
                _fab_menu.Close(true);
            }
            else
            {
                _visible = true;
                _progressBarLocation.Visibility = ViewStates.Invisible;
                _fab_menu_gps.SetIndeterminate(false);
                _gpsUpdateIndeterminate = false;
                _fab_menu.Close(true);

                // If location is succesfully updated
                GetBusinessUsersNearMe();
            }
        }

        private void GoInvisible()
        {
            if (Reachability.IsOnline(Activity)
                && !_webClientMakeUserInvisible.IsBusy)
            {
                var userLocationUpdate = new UserLocationUpdate()
                {
                    ActiveMode = ActiveModeConstants.BUSINESS
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

        private void GetBusinessUsersNearMe()
        {
            if (Reachability.IsOnline(Activity)
                && !_webClientGetBusinessUsersNearMe.IsBusy)
            {
                // You can get other users near you only if you are visible
                if (_visible)
                {
                    _progressBarGetBusinessUsers.Visibility = ViewStates.Visible;

                    var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_getBusinessUsersNearMe);

                    _webClientGetBusinessUsersNearMe.Headers.Clear();
                    _webClientGetBusinessUsersNearMe.Headers.Add(
                                HttpRequestHeader.Authorization,
                                "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(Context));

                    NameValueCollection parameters = new NameValueCollection();
                    parameters.Add("activeMode", Constants.ActiveModeConstants.BUSINESS);
                    parameters.Add("radiusOfSearch", _radiusOfSearch.ToString());

                    _webClientGetBusinessUsersNearMe.QueryString.Clear();
                    _webClientGetBusinessUsersNearMe.QueryString.Add(parameters);
                    _webClientGetBusinessUsersNearMe.DownloadDataAsync(uri);
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

        private void WebClientGetBusinessUsersNearMe_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add error type
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
                _progressBarGetBusinessUsers.Visibility = ViewStates.Gone;
            }
            else
            {
                ;
                List<BusinessInfoModel> newListOfUsersAroundMe = Newtonsoft.Json.JsonConvert.DeserializeObject
                                                                    <List<BusinessInfoModel>>(Encoding.UTF8.GetString(e.Result));
                SetUpRecyclerView(_recycleView, newListOfUsersAroundMe);

                _fab_menu.Visibility = ViewStates.Visible;
                _progressBarGetBusinessUsers.Visibility = ViewStates.Gone;
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
        private void SetUpRecyclerView(RecyclerView recyclerView, List<BusinessInfoModel> listOfUsersAroundMe) //ovde da se napravi lista dobijenih korisnika
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
            private List<BusinessInfoModel> _values;
            private RecyclerView _recycleView;
            Resources _resource;

            public RecyclerViewAdapter(Context context, List<BusinessInfoModel> items, Resources res, RecyclerView rv)
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

                simpleHolder._boundString = _values[position].FkUserId.ToString();

                if ((string.IsNullOrEmpty(_values[position].CompanyName)
                     && string.IsNullOrEmpty(_values[position].Description))
                    || (string.IsNullOrEmpty(_values[position].Email)
                        && string.IsNullOrEmpty(_values[position].PhoneNumber)))
                {
                    simpleHolder._txtViewName.Text = "Incognito user";
                    simpleHolder._checkbox.Visibility = ViewStates.Gone;
                }
                else
                {
                    simpleHolder._txtViewName.Text = _values[position].CompanyName;
                    simpleHolder._txtViewDescription.Text = _values[position].Description;
                }

                if (_values[position].ProfilePictureData != null)
                {
                    Bitmap bm = BitmapFactory
                        .DecodeByteArrayAsync(
                            _values[position].ProfilePictureData,
                            0,
                            _values[position].ProfilePictureData.Length)
                             .Result;

                    // TODO: Maybe this parameters can be played with
                    simpleHolder._imageView.SetImageBitmap(
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
                        Resource.Drawable.blank_business,
                        options);

                    options.InSampleSize = BitmapHelper.CalculateInSampleSize(options, 100, 100);
                    options.InJustDecodeBounds = false;

                    var bitMap = await BitmapFactory.DecodeResourceAsync(_resource, Resource.Drawable.blank_business, options);

                    simpleHolder._imageView.SetImageBitmap(bitMap);
                }

                // In order to not stack delegats
                simpleHolder._view.Click -= MView_Click;
                simpleHolder._view.Click += MView_Click;

                //  Random rand = new Random(); //igramo se, ali pravi probleme
                //  if (rand.Next() % 2 == 1)
                //     setScaleAnimation(holder.ItemView);
                // else
                // setFadeAnimation(holder.ItemView);

                // Needs to be here in order to prevent lines below to trigger event listener
                simpleHolder._checkbox.CheckedChange -= Checkbox_CheckedChange;

                if (IsUserInSavedContacts(_values[position].FkUserId, simpleHolder))
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

                List<BusinessInfoModel> listSavedBusinessUsers = new List<BusinessInfoModel>();

                if (pref.Contains("SavedBusinessUsersDictionary"))
                {
                    string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>>(serializedDictionary);

                        if (dictionary.ContainsKey(userId))
                        {
                            if (dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.BUSINESS)))
                            {
                                listSavedBusinessUsers = dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)];
                            }
                        }
                    }
                }

                if (!(listSavedBusinessUsers.Find(a => a.FkUserId == userIdRecyclerView) == null))
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
                    if (!pref.Contains("SavedBusinessUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                    {
                        var dictionary = new Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>();

                        dictionary.Add(userId, new Dictionary<int, List<BusinessInfoModel>>());
                        dictionary[userId].Add(int.Parse(ActiveModeConstants.BUSINESS), new List<BusinessInfoModel>());
                        dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)].Add(_values[position]);

                        edit.Remove("SavedBusinessUsersDictionary");
                        edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment2_BusinessSavedUsers refToSavedUsersFragment =
                            (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDBUSINESS));
                        refToSavedUsersFragment.SetUpRecyclerView();
                    }
                    else
                    {
                        string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                        if (serializedDictionary != String.Empty)
                        {
                            var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>>(serializedDictionary);

                            if (!dictionary.ContainsKey(userId))
                            {
                                dictionary.Add(userId, new Dictionary<int, List<BusinessInfoModel>>());
                            }
                            if (!dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.BUSINESS)))
                            {
                                dictionary[userId].Add(int.Parse(ActiveModeConstants.BUSINESS), new List<BusinessInfoModel>());
                            }

                            // Add private user from recycler view
                            dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)].Add(_values[position]);
                            edit.Remove("SavedBusinessUsersDictionary");
                            edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                            edit.Apply();
                            Fragment2_BusinessSavedUsers refToSavedUsersFragment =
                                (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDBUSINESS));
                            refToSavedUsersFragment.SetUpRecyclerView();
                        }
                    }
                }
                else
                {
                    string serializedDictionary = pref.GetString("SavedBusinessUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<BusinessInfoModel>>>>(serializedDictionary);

                        dictionary[userId][int.Parse(ActiveModeConstants.BUSINESS)].RemoveAll(a => a.FkUserId == _values[position].FkUserId);
                        edit.Remove("SavedBusinessUsersDictionary");
                        edit.PutString("SavedBusinessUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment2_BusinessSavedUsers refToSavedUsersFragment =
                            (Fragment2_BusinessSavedUsers)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.SAVEDBUSINESS));
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

                if (svh._txtViewName.Text == "Incognito user")
                {
                    return;
                }

                Context context = _recycleView.Context;
                Intent intent = new Intent(context, typeof(BusinessDetailActivity));
                intent.PutExtra("userInformation", Newtonsoft.Json.JsonConvert.SerializeObject(_values[position]));
                intent.PutExtra("isSaved", (svh._checkbox.Checked));
                context.StartActivity(intent);
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                //TODO: Maybe create another one business list item
                View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.List_Item, parent, false);
                view.SetBackgroundResource(_background);

                return new ViewHolder(view);
            }
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            public string _boundString;
            public readonly View _view;
            public readonly ImageView _imageView;
            public readonly TextView _txtViewName;
            public readonly TextView _txtViewDescription;
            public readonly CheckBox _checkbox;

            public ViewHolder(View view) : base(view)
            {
                _view = view;
                _imageView = view.FindViewById<ImageView>(Resource.Id.avatar);
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
