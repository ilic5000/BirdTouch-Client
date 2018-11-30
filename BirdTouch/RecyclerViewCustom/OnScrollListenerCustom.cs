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
using Android.Support.V7.Widget;

namespace BirdTouch.RecyclerViewCustom
{
    public class OnScrollListenerCustom : RecyclerView.OnScrollListener
    {
        private Clans.Fab.FloatingActionMenu _fab_menu_reference;

        public OnScrollListenerCustom(Clans.Fab.FloatingActionMenu fbm)
        {
            _fab_menu_reference = fbm;
        }

        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            base.OnScrolled(recyclerView, dx, dy);

            if (dy <= 0)
            {
                // Scrolling down
                _fab_menu_reference.ShowMenuButton(animate: true);
            }
            else
            {
                // Scrolling up
                _fab_menu_reference.HideMenuButton(animate: true);
            }
        }
    }
}