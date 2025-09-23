using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public sealed record VerifyTwoFactorAuthenticationDto
    {
        public string UserId { get; init; }
        public string Provider { get; init; }
        public string Key { get; init; }
    }
}
