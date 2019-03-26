using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoAuctions.ViewModels
{
    public class ChangeUserPasswordViewModel
    {        
        public string Email { get; set; }
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
        public string newPasswordConfirmation { get; set; }
    }
}
