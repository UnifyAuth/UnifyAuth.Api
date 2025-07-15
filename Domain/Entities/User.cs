using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public AuthenticationProviderType Preferred2FAProvider { get; set; }
        public string? ExternalProvider { get; set; }
        public string? ExternalProviderId { get; set; }
    }
}
