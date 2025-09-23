using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class VerifyTwoFactorConfigurationDto
    {
        public AuthenticationProviderType Provider { get; set; }
        public string Key { get; set; }
    }
}
