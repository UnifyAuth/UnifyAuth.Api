using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<IdentityUserModel> _userManager;
        private readonly IMapper _mapper;

        public UserRepository(UserManager<IdentityUserModel> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<IDataResult<User>> CreateUserAsync(User user, string password)
        {
            try
            {
                var identityUser = _mapper.Map<IdentityUserModel>(user);
                var identityResult = await _userManager.CreateAsync(identityUser, password);

                if (!identityResult.Succeeded)
                {
                    var filteredErrors = identityResult.Errors
                        .Where(e => !string.Equals(e.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (filteredErrors.Count() == 1)
                    {
                        var identityError = filteredErrors.Select(e => e.Description).ToArray().FirstOrDefault();
                        return new ErrorDataResult<User>(identityError, "BadRequest");

                    }
                    return new ErrorDataResult<User>(filteredErrors.Select(e => e.Description).ToArray(), "BadRequest");
                }
                user.Id = identityUser.Id;
                return new SuccessDataResult<User>(user, "User Created Successfully");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<User>($"Error creating user: {ex.Message}", "SystemError");
            }
        }

        public Task<IDataResult<User>> GetUserByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<IResult> UserExistByEmail(string email)
        {
            throw new NotImplementedException();
        }
    }
}
