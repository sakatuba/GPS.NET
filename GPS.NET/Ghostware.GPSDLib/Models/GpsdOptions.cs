﻿using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ghostware.GPSDLib.Models
{
    [DataContract]
    public class GpsdOptions
    {
        [DataMember(Name = "enable")]
        public bool Enable { get; set; }

        [DataMember(Name = "json")]
        public bool Json { get; set; }

        [DataMember(Name = "nmea")]
        public bool Nmea { get; set; }

        [DataMember(Name = "raw")]
        public bool Raw { get; set; }

        [DataMember(Name = "scaled")]
        public bool Scaled { get; set; }

        [DataMember(Name = "timing")]
        public bool Timing { get; set; }

        [DataMember(Name = "split24")]
        public bool Split24 { get; set; }

        [DataMember(Name = "pps")]
        public bool Pps { get; set; }

        public string GetCommand()
        {
            return $"?WATCH={JsonConvert.SerializeObject(this)}";
        }
    }
}
