using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoAuctions.Models
{
    public class ApplicationUserPasswordHistories
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public DateTime SetupDate { get; set; }
        public DateTime InvalidatedDate { get; set; }
        public string PasswordHash { get; set; }
    }
}
