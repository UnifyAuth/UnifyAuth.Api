using Application.Common.Results.Abstracts;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<IResult> UserExistByEmail(string email);
        Task<IDataResult<User>> GetUserByEmailAsync(string email);
        Task<IDataResult<User>> CreateUserAsync(User user, string password);
        Task<IDataResult<User>> CheckRegisteredUserAsync(string email, string password);
    }
}
