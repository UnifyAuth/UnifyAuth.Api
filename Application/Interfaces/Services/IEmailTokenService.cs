using Application.Common.Results.Abstracts;
using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IEmailTokenService
    {
        Task<IDataResult<ConfirmEmailTokenDto>> GenerateEmailConfirmationToken(User user);
        Task<IResult> ConfirmEmail(ConfirmEmailTokenDto confirmEmailDto);
        Task<IDataResult<ConfirmEmailTokenDto>> GenerateChangeEmailToken(User user, string newEmail);
        Task<IResult> VerifyChangeEmailToken(ChangeEmailTokenDto changeEmailTokenDto);
    }
}
