using Application.Common.Results.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class DataResult<T> : Result, IDataResult<T>
    {
        public DataResult(T? data, bool success) : base(success)
        {
            Data = data;
        }
        public DataResult(T? data, string? message, bool success, AppError? errorType) : base(message, success, errorType)
        {
            Data = data;
        }
        public DataResult(T? data, string[]? messages, bool success, AppError? errorType) : base(messages, success, errorType)
        {
            Data = data;
        }
        public T? Data { get; }
    }
}
