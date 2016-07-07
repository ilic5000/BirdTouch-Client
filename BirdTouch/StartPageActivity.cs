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

namespace BirdTouch
{
    [Activity(Label = "StartPageActivity")]
    public class StartPageActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.StartPage);

            
            User user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(Intent.GetStringExtra("userLoggedInJson"));
            ActionBar.Title = user.Username; //kada se uloguje user, da vidimo njegov username u action baru
            
            // Create your application here
        }



        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            var inflater = MenuInflater;
            inflater.Inflate(Resource.Menu.menu_main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if(id == Resource.Id.privateUserActionBarButton) {
                Toast.MakeText(this, "Private user activity starting...", ToastLength.Short).Show();
            }
            if (id == Resource.Id.bussinessUserActionBarButton)
            {
                Toast.MakeText(this, "Bussiness user activity starting...", ToastLength.Short).Show();
            }

            return base.OnOptionsItemSelected(item);
        }

    }


    


}