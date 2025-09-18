using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.IdentityModels;
using Infrastructure.Persistence.Context;
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
        private readonly UnifyAuthContext _context;
        private readonly UserManager<IdentityUserModel> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(UserManager<IdentityUserModel> userManager, IMapper mapper, ILogger<UserRepository> logger, UnifyAuthContext context)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _context = context;
        }

        public async Task<IDataResult<User>> CreateUserAsync(User user, string password)
        {
            var identityUser = _mapper.Map<IdentityUserModel>(user);
            var identityResult = await _userManager.CreateAsync(identityUser, password);

            if (!identityResult.Succeeded)
            {
                var filteredErrors = identityResult.Errors
                    .Where(e => !string.Equals(e.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                    .ToList(); // Exclude duplicate username error because username same with email. This error handled duplicate email error.

                if (filteredErrors.Count() == 1)
                {
                    var identityError = filteredErrors.Select(e => e.Description).ToArray().FirstOrDefault();
                    return new ErrorDataResult<User>(identityError!, AppError.Validation());
                }
                return new ErrorDataResult<User>(filteredErrors.Select(e => e.Description).ToArray(), AppError.Validation());
            }
            user.Id = identityUser.Id;
            return new SuccessDataResult<User>(user);
        }

        public async Task<IDataResult<User>> GetUserByEmailAsync(string email)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                _logger.LogInformation("User not found with Email: {Email}", email);
                return new ErrorDataResult<User>("User not found", AppError.NotFound());
            }

            var user = _mapper.Map<User>(identityUser);
            return new SuccessDataResult<User>(user);
        }

        public async Task<IDataResult<User>> CheckRegisteredUserAsync(string email, string password)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                _logger.LogInformation("User not found with Email: {Email}", email);
                return new ErrorDataResult<User>("Email or password incorrect", AppError.BadRequest());
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, password);
            if (!isPasswordValid)
            {
                _logger.LogInformation("Invalid password attempt for Email: {Email}", email);
                return new ErrorDataResult<User>("Email or password incorrect", AppError.BadRequest());
            }

            User user = _mapper.Map<User>(identityUser);
            return new SuccessDataResult<User>(user);
        }
        public async Task<IResult> UserExistByEmail(string email)
        {
            var identityUser = await _userManager.Users.AnyAsync(u => u.Email == email);
            if (!identityUser) return new ErrorResult("User not found", AppError.BadRequest());

            return new SuccessResult("User exists");
        }

        public async Task<IDataResult<User>> GetUserByIdAsync(string userId)
        {
            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                _logger.LogWarning("User not found for update with Id: {UserId}", userId);
                return new ErrorDataResult<User>("User not found", AppError.NotFound());
            }

            User user = _mapper.Map<User>(identityUser);
            return new SuccessDataResult<User>(user);
        }

        public async Task<IResult> UpdateUserAsync(UserUpdateDto userUpdateDto)
        {
            var identityUser = await _userManager.FindByIdAsync(userUpdateDto.Id.ToString());

            identityUser!.FirstName = userUpdateDto.FirstName;
            identityUser.LastName = userUpdateDto.LastName;
            identityUser.PhoneNumber = userUpdateDto.PhoneNumber;

            var identityResult = await _userManager.UpdateAsync(identityUser);
            if (!identityResult.Succeeded)
            {
                var filteredErrors = identityResult.Errors
                    .Where(e => !string.Equals(e.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                    .ToList(); // Exclude duplicate username errors because username same with email. This error handled duplicate email error.

                if (filteredErrors.Count() == 1)
                {
                    var identityError = filteredErrors.Select(e => e.Description).FirstOrDefault();
                    _logger.LogInformation("Failed to update user with Email: {Email}. Error: {Error}", identityUser.Email, identityError);
                    return new ErrorResult(identityError!, AppError.Validation());
                }
                _logger.LogInformation("Failed to update user with Email: {Email}. Errors: {Errors}", identityUser.Email, string.Join(", ", filteredErrors.Select(e => e.Description)));
                return new ErrorResult(filteredErrors.Select(e => e.Description).ToArray(), AppError.Validation());
            }

            _logger.LogInformation("User updated successfully with Email: {Email}", identityUser.Email);
            return new SuccessResult("User updated successfully");
        }
    }
}
