using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ResetPasswordDto
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmedPassword { get; set; }
    }
}
