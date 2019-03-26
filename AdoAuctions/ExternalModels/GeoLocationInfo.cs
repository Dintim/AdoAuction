using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoAuctions.ExternalModels
{
    public class GeoLocationInfo
    {
        public string ip { get; set; }
        public string country_name { get; set; }
        public string city { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
    }
}
