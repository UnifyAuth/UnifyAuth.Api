using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum AuthenticationProviderType
    {
        None = 0,
        Email = 1,
        Phone = 2,
        Authenticator = 3,
    }
}
