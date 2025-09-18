using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class ErrorDataResult<T> : DataResult<T>
    {
        public ErrorDataResult(string message, AppError errorType) : base(default, message, false, errorType)
        {
        }
        public ErrorDataResult(string[] messages, AppError errorType) : base(default, messages, false, errorType)
        {
        }
    }
}
