using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class AuthenticatorAppDto
    {
        public string SharedKey { get; set; }
        public string QrCodeUri { get; set; }
    }
}
