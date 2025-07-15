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
        Task<IDataResult<ConfirmEmailDto>> GenerateEmailConfirmationToken(Guid userId);
        Task<IResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto); 
    }
}
