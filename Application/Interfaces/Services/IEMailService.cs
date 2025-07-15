using Application.Common.Results.Abstracts;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IEMailService
    {
        Task<IResult> SendAsync(string to, string subject, string htmlContent);
    }
}
