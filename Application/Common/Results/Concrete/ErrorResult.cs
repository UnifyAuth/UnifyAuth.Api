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
        public ErrorResult(string[] messages, string errorType) : base(false)
        {
            Messages = messages;
            ErrorType = errorType;
        }
        public string ErrorType { get; }
        public string[] Messages { get; set; }
    }
}
