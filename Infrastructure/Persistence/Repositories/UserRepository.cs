using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(UserManager<IdentityUserModel> userManager, IMapper mapper, ILogger<UserRepository> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IDataResult<User>> CreateUserAsync(User user, string password)
        {
            //Debugging log
            _logger.LogDebug("Creating user with email: {Email}", user.Email);

            var identityUser = _mapper.Map<IdentityUserModel>(user);
            var identityResult = await _userManager.CreateAsync(identityUser, password);

            //Debugging log
            _logger.LogDebug("Identity result for user creation: {Succeeded}, Errors: {Errors}", 
                user.Email,
                identityResult.Succeeded, 
                identityResult.Errors.Select(e => e.Description).ToArray());

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
