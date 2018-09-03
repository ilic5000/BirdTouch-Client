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
    public class Fragment1_Private : SupportFragment, ILocationListener
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
        String _locationProvider;
        // in ms
        private long _locationTimeIntervalForChecking;
        // in meter
        private float _locationDistanceNeededForUpdateToTrigger;
        // TODO: Implement setting page
        private double _radiusOfSearch = 0.5;

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
            _fab_menu = view.FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu_private);
            _fab_menu.Visibility = ViewStates.Gone;

            // Initialize web clients
            _webClientMakeUserVisible = new WebClient();
            _webClientMakeUserInvisible = new WebClient();
            _webClientGetPrivateUsersNearMe = new WebClient();

            // Register events for web clients
            _webClientMakeUserVisible.UploadStringCompleted += WebClientMakeUserVisible_UploadStringCompleted;
            _webClientMakeUserInvisible.UploadStringCompleted += WebClientMakeUserInvisible_UploadStringCompleted;
            _webClientGetPrivateUsersNearMe.DownloadDataCompleted += WebClientGetPrivateUsersNearMe_DownloadDataCompleted;

            // Register events for components
            _fab_menu.MenuToggle += Fab_menu_MenuToggle;
            _fab_menu_gps.Click += Fab_menu_gps_Click;
            _fab_menu_refresh.Click += Fab_menu_refresh_Click;
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
                _linearLayout.Click += linearLayoutClick;
                _recycleView.Visibility = ViewStates.Invisible;
            }
            else
            {
                _linearLayout.Click -= linearLayoutClick;
                _recycleView.Visibility = ViewStates.Visible;
            }
        }

        private void linearLayoutClick(object sender, EventArgs e)
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
            GetPrivateUsersNearMe();
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
                    parameters.Add("radiusOfSearch", _radiusOfSearch.ToString());

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

                _fab_menu.Visibility = ViewStates.Visible;
                _progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
            }
        }

        //LocationListener interface
        // TODO: Maybe implement in future
        public void OnProviderDisabled(string provider)
        {
            Snackbar.Make(
                _frameLay,
                Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is disabled</font>"),
                Snackbar.LengthLong)
                 .Show();
        }

        // TODO: Maybe implement in future
        public void OnProviderEnabled(string provider)
        {
            Snackbar.Make(
                _frameLay,
                Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is enabled</font>"),
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
        private void SetUpRecyclerView(RecyclerView recyclerView, List<UserInfoModel> listOfUsersAroundMe) //ovde da se napravi lista dobijenih korisnika
        {
            recyclerView.SetLayoutManager(
                new LinearLayoutManager(recyclerView.Context));

            recyclerView.SetAdapter(
                new SimpleStringRecyclerViewAdapter(
                    recyclerView.Context,
                    listOfUsersAroundMe,
                    Activity.Resources,
                    _recycleView));
        }


        public void NotifyDataSetChangedFromAnotherFragment()
        {
            _recycleView.GetAdapter().NotifyDataSetChanged();
        }


        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue mTypedValue = new TypedValue();
            private int mBackground;
            private List<UserInfoModel> mValues;
            private RecyclerView recycleView;
            Resources mResource;

            public SimpleStringRecyclerViewAdapter(Context context, List<UserInfoModel> items, Resources res, RecyclerView rv)
            {
                context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackground, mTypedValue, true);
                mBackground = mTypedValue.ResourceId;
                mValues = items;
                mResource = res;
                recycleView = rv;
            }

            public override int ItemCount
            {
                get
                {
                    return mValues.Count;
                }
            }

            public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var simpleHolder = holder as SimpleViewHolder;

                simpleHolder.mBoundString = mValues[position].Id.ToString();
                simpleHolder.mTxtView.Text = mValues[position].FirstName + " " + mValues[position].LastName;
                simpleHolder.mTxtViewDescription.Text = mValues[position].Description;

                if (mValues[position].ProfilePictureData != null)
                {
                    Bitmap bm = BitmapFactory.DecodeByteArrayAsync(mValues[position].ProfilePictureData, 0, mValues[position].ProfilePictureData.Length).Result;
                    simpleHolder.mImageView.SetImageBitmap(Bitmap.CreateScaledBitmap(bm, 200, 100, false));// mozda treba malo da se igra sa ovim
                }
                else
                {
                    //ako korisnik nije postavio profilnu sliku

                    BitmapFactory.Options options = new BitmapFactory.Options();
                    options.InJustDecodeBounds = true;

                    BitmapFactory.DecodeResource(mResource, Resource.Drawable.blank_navigation, options);

                    options.InSampleSize = CalculateInSampleSize(options, 100, 100);
                    options.InJustDecodeBounds = false;

                    var bitMap = await BitmapFactory.DecodeResourceAsync(mResource, Resource.Drawable.blank_navigation, options);

                    simpleHolder.mImageView.SetImageBitmap(bitMap);
                }

                simpleHolder.mView.Click -= MView_Click; //da se ne bi gomilali delegati
                simpleHolder.mView.Click += MView_Click;

                // Random rand = new Random(); //igramo se, ali pravi probleme
                //if(rand.Next() % 2 == 1)
                // setScaleAnimation(holder.ItemView);
                //else
                //setFadeAnimation(holder.ItemView);

                simpleHolder.checkbox.CheckedChange -= Checkbox_CheckedChange;//stavljeno ovde da ne bi redovi ispod izazvali pozivanje event listenera

                if (isUserInSavedContacts(mValues[position].Id, simpleHolder)) simpleHolder.checkbox.Checked = true; //da li je vec u saved contacts
                else simpleHolder.checkbox.Checked = false;

                simpleHolder.checkbox.Tag = simpleHolder.mView; //potrebno zbog pozicije u adapteru koja nam je potrebna u Checkbox_CheckedChange
                simpleHolder.checkbox.CheckedChange += Checkbox_CheckedChange;
            }


            private bool isUserInSavedContacts(Guid userIdRecyclerView, SimpleViewHolder svh)
            {
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = svh.ItemView.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                List<UserInfoModel> listSavedPrivateUsers = new List<UserInfoModel>();

                if (pref.Contains("SavedPrivateUsersDictionary"))
                {
                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        if (dictionary.ContainsKey(userId))
                        {//ako je user dodavao usere
                            if (dictionary[userId].ContainsKey(1))
                            {//ako je dodavao private usere
                                listSavedPrivateUsers = dictionary[userId][1];
                            }
                        }

                    }

                }

                if (!(listSavedPrivateUsers.Find(a => a.Id == userIdRecyclerView) == null)) return true;
                else return false;
            }

            private void Checkbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                CheckBox vsender = sender as CheckBox;

                View mView = (View)vsender.Tag;
                int position = recycleView.GetChildAdapterPosition(mView);
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = vsender.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                if (e.IsChecked)
                {//checked

                    if (!pref.Contains("SavedPrivateUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                    {

                        var dictionary = new Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>();

                        dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                        dictionary[userId].Add(1, new List<UserInfoModel>());// 1 je private mode
                        dictionary[userId][1].Add(mValues[position]);

                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                        refToSavedUsersFragment.SetUpRecyclerView();

                    }
                    else //vec postoji dictionary
                    {
                        string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                        if (serializedDictionary != String.Empty)
                        {
                            var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                                 <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                            if (!dictionary.ContainsKey(userId))
                            {//ako user nije uopste dodavao usere
                                dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                            }
                            if (!dictionary[userId].ContainsKey(1))
                            {//ako nije dodavao private usere
                                dictionary[userId].Add(1, new List<UserInfoModel>());
                            }

                            //samo dodamo private usera iz recyclerViewa
                            dictionary[userId][1].Add(mValues[position]);
                            edit.Remove("SavedPrivateUsersDictionary");
                            edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                            edit.Apply();
                            Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                            refToSavedUsersFragment.SetUpRecyclerView();
                        }
                    }
                }
                else
                {//unchecked

                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        dictionary[userId][1].RemoveAll(a => a.Id == mValues[position].Id);
                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                        refToSavedUsersFragment.SetUpRecyclerView();
                    }
                }
            }

            private void setFadeAnimation(View view)
            {
                int FADE_DURATION = 1400; // in milliseconds
                AlphaAnimation anim = new AlphaAnimation(0.0f, 1.0f);
                anim.Duration = FADE_DURATION;
                view.StartAnimation(anim);
            }

            private void setScaleAnimation(View view)
            {
                int FADE_DURATION = 1000; // in milliseconds
                ScaleAnimation anim = new ScaleAnimation(0.0f, 1.0f, 0.0f, 1.0f);
                anim.Duration = FADE_DURATION;
                view.StartAnimation(anim);
            }

            private void MView_Click(object sender, EventArgs e)
            {
                int position = recycleView.GetChildAdapterPosition((View)sender);
                SimpleViewHolder svh = (SimpleViewHolder)recycleView.GetChildViewHolder((View)sender);

                Context context = recycleView.Context;
                Intent intent = new Intent(context, typeof(UserDetailActivity));
                intent.PutExtra("userInformation", Newtonsoft.Json.JsonConvert.SerializeObject(mValues[position]));
                intent.PutExtra("isSaved", (svh.checkbox.Checked));
                context.StartActivity(intent);
            }

            private int CalculateInSampleSize(BitmapFactory.Options options, int requestedWidth, int requestedHeight)
            {
                //Raw height and width of image
                int height = options.OutHeight;
                int width = options.OutWidth;
                int inSampleSize = 1;

                if (height > requestedHeight || width > requestedWidth)
                {
                    //slika je veca nego sto nam treba
                    int halfHeight = height / 2;
                    int halfWidth = width / 2;

                    while ((halfHeight / inSampleSize) >= requestedHeight && (halfWidth / inSampleSize) >= requestedWidth)
                    {
                        inSampleSize *= 2;
                    }
                }
                Console.WriteLine();
                Console.WriteLine("SampleSizeBitmap: " + inSampleSize.ToString());
                Console.WriteLine();
                return inSampleSize;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.List_Item, parent, false);
                view.SetBackgroundResource(mBackground);

                return new SimpleViewHolder(view);
            }
        }

        public class SimpleViewHolder : RecyclerView.ViewHolder
        {
            public string mBoundString;
            public readonly View mView;
            public readonly ImageView mImageView;
            public readonly TextView mTxtView;
            public readonly TextView mTxtViewDescription;
            public readonly CheckBox checkbox;

            public SimpleViewHolder(View view) : base(view)
            {
                mView = view;
                mImageView = view.FindViewById<ImageView>(Resource.Id.avatar); //profilna slika usera
                mTxtView = view.FindViewById<TextView>(Resource.Id.text1); //ime + prezime usera
                mTxtViewDescription = view.FindViewById<TextView>(Resource.Id.text2);
                checkbox = view.FindViewById<CheckBox>(Resource.Id.checkboxSaveUserRecycleViewRow);
            }

            public override string ToString()
            {
                return base.ToString() + " '" + mTxtView.Text;
            }
        }
    }
}
