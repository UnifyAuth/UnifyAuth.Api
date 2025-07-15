using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class ErrorResult : Result
    {
        public ErrorResult(string message) : base(message, false)
        {
        }
        public ErrorResult(string message, string errorType) : base(message, false)
        {
            ErrorType = errorType;
        }
        public ErrorResult(string message, IDictionary<string, string[]> errors, string errorType) : base(message, false)
        {
            Errors = errors;
            ErrorType = errorType;
        }
        public ErrorResult(IDictionary<string, string[]> errors, string errorType) : base(false)
        {
            Errors = errors;
            ErrorType = errorType;
        }
        public IDictionary<string, string[]> Errors { get; }
        public string ErrorType { get; }
    }
}
