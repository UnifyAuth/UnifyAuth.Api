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
        public DataResult(T data, bool succcess) : base(succcess)
        {
            Data = data;
        }
        public DataResult(T data, string message, bool success) : base(message, success)
        {
            Data = data;
        }
        public T Data { get; }
    }
}
