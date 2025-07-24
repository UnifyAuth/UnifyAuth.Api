using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class ErrorDataResult<T> : DataResult<T>
    {
        public ErrorDataResult(T data, string message) : base(data, message, false)
        {
        }
        public ErrorDataResult(string message) : base(default, message, false) { }
        public ErrorDataResult(T data, string message, string errorType) : base(data, message, false)
        {
            ErrorType = errorType;
        }
        public ErrorDataResult(string message, string errorType) : base(default, message, false)
        {
            ErrorType = errorType;
        }
        public ErrorDataResult(string[] messages, string errorType) : base(default, default, false)
        {
            Messages = messages;
            ErrorType = errorType;
        }

        public string ErrorType { get; }
        public string[] Messages { get; set; }
    }
}
