using Application.Common.Results.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class Result : IResult
    {

        public Result(string? message, bool success, AppError? errorType) : this(success)
        {
            Message = message;
            ErrorType = errorType;
        }
        public Result(string[]? messages, bool success, AppError? errorType) : this(success)
        {
            Messages = messages;
            ErrorType = errorType;
        }
        public Result(bool success)
        {
            Success = success;
        }

        public string? Message { get; }
        public string[]? Messages { get; }
        public bool Success { get; }
        public AppError? ErrorType { get; }
    }
}
