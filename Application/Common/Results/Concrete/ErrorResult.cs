using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class ErrorResult : Result
    {
        public ErrorResult(string message, AppError errorType) : base(message, false, errorType)
        {
        }
        public ErrorResult(string[] messages, AppError errorType) : base(messages,false, errorType)
        {
        }
    }
}
