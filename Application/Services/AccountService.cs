using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccountService> _logger;
        private readonly IMapper _mapper;

        public AccountService(IUserRepository userRepository, ILogger<AccountService> logger, IMapper mapper)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IDataResult<UserDto>> GetUserInfos(string userId)
        {
            var result = await _userRepository.GetUserByIdAsync(userId);
            if(result is ErrorDataResult<User> errorDataResult)
            {
                _logger.LogInformation("Error retrieving user info for userId {UserId}: {Error}", userId, errorDataResult.Message);
                return new ErrorDataResult<UserDto>(errorDataResult.Message, errorDataResult.ErrorType);
            }

            UserDto userDto = _mapper.Map<UserDto>(result.Data);
            return new SuccessDataResult<UserDto>(userDto, "User information retrieved successfully");
        }

    }
}
