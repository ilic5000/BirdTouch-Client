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
using BirdTouch.Models;
using Android.Views.Animations;
using System.Net;
using System.Collections.Specialized;
using Android.Support.Design.Widget;
using Android.Text;
using BirdTouch.Helpers;
using BirdTouch.Activities;
using BirdTouch.Extensions;
using BirdTouch.Constants;
using Newtonsoft.Json;

namespace BirdTouch.Fragments
{
    public class Fragment1_PrivateSavedUsers : SupportFragment
    {
        private FrameLayout _frameLay;
        private RecyclerView _recycleView;

        private Clans.Fab.FloatingActionButton _fabMenuSave;
        private Clans.Fab.FloatingActionButton _fabMenuLoad;
        private Clans.Fab.FloatingActionMenu _fabMenu;

        private WebClient _webClientSaveList;
        private WebClient _webClientLoadList;

        private List<UserInfoModel> _listSavedPrivateUsers;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inflate view with resource layout
            View view = inflater.Inflate(Resource.Layout.Fragment1_private_savedUsers, container, false);

            // Find components
            _recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewPrivateSavedUsers);
            _frameLay = view.FindViewById<FrameLayout>(Resource.Id.coordinatorLayoutPrivateSavedUsers);
            _fabMenuLoad = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_load_from_cloud);
            _fabMenuSave = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_save_to_cloud);
            _fabMenu = view.FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu_private_saved);

            // Initialize web clients
            _webClientLoadList = new WebClient();
            _webClientSaveList = new WebClient();

            // Register events for web clients
            _webClientLoadList.DownloadDataCompleted += WebClientLoadList_DownloadDataCompleted;
            _webClientSaveList.UploadStringCompleted += WebClientSaveList_UploadStringCompleted;

            // Register events for components
            _fabMenuSave.Click += Fab_menu_save_Click;
            _fabMenuLoad.Click += Fab_menu_load_Click;

            // Initialize recycle view
            SetUpRecyclerView();

            return view;
        }

        private void Fab_menu_load_Click(object sender, EventArgs e)
        {
            // TODO: Implement download contacts from server
            Snackbar.Make(
             _frameLay,
             Html.FromHtml("<font color=\"#ffffff\">Currently not implemented</font>"),
             Snackbar.LengthLong)
              .Show();
        }

        private void Fab_menu_save_Click(object sender, EventArgs e)
        {
            SaveListToDatabase();
            _fabMenu.Close(true);
        }

        private void SaveListToDatabase()
        {
            if (_listSavedPrivateUsers.Count > 0)
            {
                if (Reachability.IsOnline(Activity) && !_webClientSaveList.IsBusy)
                {
                    List<Guid> listSavedContactsId = new List<Guid>();

                    foreach (UserInfoModel item in _listSavedPrivateUsers)
                    {
                        listSavedContactsId.Add(item.FkUserId);
                    }

                    var uri = WebApiUrlGenerator
                            .GenerateWebApiUrl(Resource.String.webapi_endpoint_savePrivateSavedList);

                    _webClientSaveList.Headers.Clear();
                    _webClientSaveList.Headers.Add(
                        HttpRequestHeader.ContentType,
                        "application/json");
                    _webClientSaveList.Headers.Add(
                       HttpRequestHeader.Authorization,
                       "Bearer " + JwtTokenHelper.GetTokenFromSharedPreferences(Context));

                    _webClientSaveList.UploadStringAsync(uri, "POST", JsonConvert.SerializeObject(listSavedContactsId));
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
        }

        private void WebClientSaveList_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // TODO: Add error type
                Snackbar.Make(
                    _frameLay,
                    Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"),
                    Snackbar.LengthLong)
                     .Show();
            }
            else
            {
                Snackbar.Make(
                _frameLay,
                Html.FromHtml("<font color=\"#ffffff\">Saved contacts list is sucessfully uploaded to cloud</font>"),
                Snackbar.LengthLong)
                 .Show();
            }
        }

        private void WebClientLoadList_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            // TODO: Implement download contacts from server
            Snackbar.Make(
                _frameLay,
                Html.FromHtml("<font color=\"#ffffff\">Currently not implemented</font>"),
                Snackbar.LengthLong)
                 .Show();
        }

        public void SetUpRecyclerView()
        {
            Guid userId = StartPageActivity.user.Id;
            _listSavedPrivateUsers = new List<UserInfoModel>();

            ISharedPreferences pref = Context
                .ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
            ISharedPreferencesEditor edit = pref.Edit();

            if (pref.Contains("SavedPrivateUsersDictionary"))
            {
                string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                if (serializedDictionary != String.Empty)
                {
                    var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                        <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                    if (dictionary.ContainsKey(userId))
                    {
                        if (dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.PRIVATE)))
                        {
                            _listSavedPrivateUsers = dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)];
                        }
                    }
                }
            }

            _recycleView.SetLayoutManager(
                new LinearLayoutManager(_recycleView.Context));

            _recycleView.SetAdapter(
                new RecyclerViewAdapter(
                    _recycleView.Context,
                    _listSavedPrivateUsers,
                    Activity.Resources,
                    _recycleView));
        }

        //*****************************************************************
        //RecycleView classes

        public class RecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue _typedValue = new TypedValue();
            private int _background;
            private List<UserInfoModel> _values;
            private RecyclerView _recycleView;
            Resources _resource;

            public RecyclerViewAdapter(Context context, List<UserInfoModel> items, Resources res, RecyclerView rv)
            {
                context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackground, _typedValue, true);
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
                simpleHolder._txtViewName.Text = _values[position].FirstName + " " + _values[position].LastName;
                simpleHolder._txtViewDescription.Text = _values[position].Description;

                if (_values[position].ProfilePictureData != null)
                {
                    Bitmap bm = BitmapFactory
                        .DecodeByteArrayAsync(
                            _values[position].ProfilePictureData,
                            0,
                            _values[position].ProfilePictureData.Length)
                             .Result;

                    simpleHolder._imageView.SetImageBitmap(
                        Bitmap.CreateScaledBitmap(
                            bm,
                            200,
                            100,
                            false));
                }
                else
                {
                    BitmapFactory.Options options = new BitmapFactory.Options();
                    options.InJustDecodeBounds = true;

                    BitmapFactory.DecodeResource(_resource, Resource.Drawable.blank_navigation, options);

                    options.InSampleSize = BitmapHelper.CalculateInSampleSize(options, 100, 100);
                    options.InJustDecodeBounds = false;

                    var bitMap = await BitmapFactory.DecodeResourceAsync(_resource, Resource.Drawable.blank_navigation, options);

                    simpleHolder._imageView.SetImageBitmap(bitMap);
                }

                simpleHolder._view.Click -= MView_Click;
                simpleHolder._view.Click += MView_Click;
                simpleHolder._checkbox.Checked = true;

                // in order to have information about position
                simpleHolder._checkbox.Tag = simpleHolder._view;
                simpleHolder._checkbox.CheckedChange -= Checkbox_CheckedChange;
                simpleHolder._checkbox.CheckedChange += Checkbox_CheckedChange;
            }

            private void Checkbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                CheckBox vsender = sender as CheckBox;

                View mView = (View)vsender.Tag;
                int position = _recycleView.GetChildAdapterPosition(mView);
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = vsender.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
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
                            var dictionary =
                                Newtonsoft.Json.JsonConvert.DeserializeObject<
                                    Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                            if (!dictionary.ContainsKey(userId))
                            {
                                dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                            }
                            if (!dictionary[userId].ContainsKey(int.Parse(ActiveModeConstants.PRIVATE)))
                            {
                                dictionary[userId].Add(int.Parse(ActiveModeConstants.PRIVATE), new List<UserInfoModel>());
                            }

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
                        _values = dictionary[userId][int.Parse(ActiveModeConstants.PRIVATE)];
                        NotifyItemRemoved(position);
                        Fragment1_Private refToSavedUsersFragment =
                            (Fragment1_Private)StartPageActivity.adapter.GetItem(int.Parse(AdapterFragmentsOrder.PRIVATE));
                        refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                    }
                }
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