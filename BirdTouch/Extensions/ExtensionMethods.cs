using System;
using Android.Views;
using Android.Support.V7.Widget;

namespace BirdTouch.Extensions
{
    public static class ExtensionMethods
    {
        // TODO: Remove if we dont need this
        public static void SetItemClickListener(this RecyclerView rv, Action<RecyclerView, int, View> action)
        {
            rv.AddOnChildAttachStateChangeListener(new AttachStateChangeListener(rv, action));
        }
    }

    // TODO: Remove if we dont need this
    public class AttachStateChangeListener : Java.Lang.Object, RecyclerView.IOnChildAttachStateChangeListener
    {
        private RecyclerView mRecyclerview;
        private Action<RecyclerView, int, View> mAction;

        public AttachStateChangeListener(RecyclerView rv, Action<RecyclerView, int, View> action) : base()
        {
            mRecyclerview = rv;
            mAction = action;
        }

        public void OnChildViewAttachedToWindow(View view)
        {
            view.Click += View_Click;
        }

        public void OnChildViewDetachedFromWindow(View view)
        {
            view.Click -= View_Click;
        }

        private void View_Click(object sender, EventArgs e)
        {
            RecyclerView.ViewHolder holder = mRecyclerview.GetChildViewHolder(((View)sender));
            mAction.Invoke(mRecyclerview, holder.AdapterPosition, ((View)sender));
        }
    }
}