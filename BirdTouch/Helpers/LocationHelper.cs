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

            // GPS location criteria
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };

            IList<string> acceptableLocationProviders = locationManager.GetProviders(
                                                            criteriaForLocationService,
                                                            true);

            if (acceptableLocationProviders.Any())
            {
                return acceptableLocationProviders.First();
            }

            // Network location criteria
            Criteria criteriaForLocationServiceBackup = new Criteria
            {
                Accuracy = Accuracy.Coarse
            };

            acceptableLocationProviders = locationManager.GetProviders(
                                            criteriaForLocationServiceBackup,
                                            true);

            if (acceptableLocationProviders.Any())
            {
                return acceptableLocationProviders.First();
            }

            // If no location providers are found (location is turned off)
            return string.Empty;
        }
    }
}