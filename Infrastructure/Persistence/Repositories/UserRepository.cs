using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogDebug("Identity result for user creation: {Succeeded},Email: {Email}, Errors: {Errors}", 
                identityResult.Succeeded, 
                user.Email,
                identityResult.Errors.Select(e => e.Description).ToArray());

            if (!identityResult.Succeeded)
            {
                var filteredErrors = identityResult.Errors
                    .Where(e => !string.Equals(e.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase)) 
                    .ToList(); // Exclude duplicate username errors because username same with email. This error handled duplicate email error.

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

        public async Task<IDataResult<User>> GetUserByEmailAsync(string email)
        {
            //Debugging log
            _logger.LogDebug("Retrieving user by email: {Email}", email);
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null) return new ErrorDataResult<User>("User not found", "NotFound");            

            var user = _mapper.Map<User>(identityUser);
            return new SuccessDataResult<User>(user, "User retrieved successfully");
        }

        public async Task<IDataResult<User>> CheckRegisteredUserAsync(string email, string password)
        {
            // Debugging log
            _logger.LogDebug("Checking registered user with email: {Email}", email);

            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null) return new ErrorDataResult<User>("User not found", "NotFound");

            var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, password);
            if(!isPasswordValid) return new ErrorDataResult<User>("Invalid password", "Unauthorized");

            User user = _mapper.Map<User>(identityUser);
            return new SuccessDataResult<User>(user);
        }
        public Task<IResult> UserExistByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<IDataResult<User>> GetUserByIdAsync(string userId)
        {
            //Debugging log
            _logger.LogDebug("Retrieving user by ID: {UserId}", userId);

            var identityUser = await _userManager.FindByIdAsync(userId);
            if(identityUser == null) return new ErrorDataResult<User>("User not found", "NotFound");

            User user = _mapper.Map<User>(identityUser);
            return new SuccessDataResult<User>(user, "User retrieved successfully");
        }
    }
}
