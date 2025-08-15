using Application.Common.Results.Abstracts;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IPasswordService
    {
       Task<IResult> ResetPassword(User user, string token, string newPassword);
    }
}
