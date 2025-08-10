using Application.Common.Results.Abstracts;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<IDataResult<UserDto>> GetUserInfos(string userId);
    }
}
