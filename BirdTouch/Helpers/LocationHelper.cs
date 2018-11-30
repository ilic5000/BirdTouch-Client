using Android.App;
using Android.Content;
using Android.Locations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BirdTouch.Helpers
{
    public static class LocationHelper
    {
        //TODO: Maybe do better location search
        public static string TryToFindLocationProvider(LocationManager locationManager, Activity activity)
        {
            locationManager = (LocationManager)activity.GetSystemService(Context.LocationService);

            // Network location criteria
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Coarse
            };

            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService,
                                                                                     true);

            acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService,
                                                                       true);

            if (acceptableLocationProviders.Any())
            {
                return acceptableLocationProviders.First();
            }

            // GPS location criteria
            criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };

            if (acceptableLocationProviders.Any())
            {
                return acceptableLocationProviders.First();
            }

            // If no location providers are found (location is turned off)
            return string.Empty;
        }
    }
}