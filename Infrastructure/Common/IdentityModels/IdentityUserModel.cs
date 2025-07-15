using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Common.IdentityModels
{
    public class IdentityUserModel: IdentityUser<Guid>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public AuthenticationProviderType Preferred2FAProvider { get; set; }
        public string? ExternalProvider { get; set; }
        public string? ExternalProviderId { get; set; }
    }
}
