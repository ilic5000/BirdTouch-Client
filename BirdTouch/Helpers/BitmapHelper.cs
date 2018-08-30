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
using Android.Graphics;
using System.IO;

namespace BirdTouch.Helpers
{
    public class BitmapHelper
    {
        public static Bitmap DecodeBitmapFromStream(ContentResolver contentResolver, Android.Net.Uri data, int requestedWidth, int requestedHeight)
        {
            // Decode with inJustDecodeBounds = true to check dimensions
            // Check size of the image to make sure its not too big and couldnt fit in memory
            //proveravamo samo velicinu slike, da nije neka prevelika slika koja bi napunila memoriju
            Stream stream = contentResolver.OpenInputStream(data);
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InJustDecodeBounds = true;
            BitmapFactory.DecodeStream(stream, null, options);

            int imageHeight = options.OutHeight;
            int imageWidth = options.OutWidth;


            //Calculate InSampleSize
            options.InSampleSize = CalculateInSampleSize(options, requestedWidth, requestedHeight);

            //Decode bitmap with InSampleSize set
            stream = contentResolver.OpenInputStream(data); //must read again
            options.InJustDecodeBounds = false;
            Bitmap bitmap = BitmapFactory.DecodeStream(stream, null, options);

            return bitmap;
        }

        public static int CalculateInSampleSize(BitmapFactory.Options options, int requestedWidth, int requestedHeight)
        {
            //Raw height and width of image
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            // Image is bigger than we need
            if (height > requestedHeight || width > requestedWidth)
            {
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while ((halfHeight / inSampleSize) >= requestedHeight && (halfWidth / inSampleSize) >= requestedWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }
    }
}