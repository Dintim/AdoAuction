using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoAuctions.Models
{
    public class ApplicationUserSignInHistories
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public DateTime SignInTime { get; set; }
        public string MachineIp { get; set; }
        public string IpToGeoCountryCode { get; set; }
        public string IpToGeoCityName { get; set; }
        public decimal IpToGeoLatitude { get; set; }
        public decimal IpToGeoLongitude { get; set; }
    }
}
