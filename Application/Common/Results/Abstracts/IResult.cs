using Application.Common.Results.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Abstracts
{
    public interface IResult
    {
        public string? Message { get; }
        public string[]? Messages { get; }
        public bool Success { get; }
        public AppError? ErrorType { get; }
    }
}
