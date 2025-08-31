using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class TwoFactorConfigurationDto
    {
        public AuthenticationProviderType Provider { get; set; }
        public string SharedKey { get; set; }
        public string QrCodeUri { get; set; }
    }
}
