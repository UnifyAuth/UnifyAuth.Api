using Application.Common.Security;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ITokenService
    {
        Task<AccessToken> GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}
