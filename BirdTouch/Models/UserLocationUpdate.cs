using Newtonsoft.Json;
using System;

namespace BirdTouch.Models
{
    public class UserLocationUpdate
    {
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public string ActiveMode { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}