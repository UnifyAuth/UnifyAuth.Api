using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public class SuccessResult : Result
    {
        public SuccessResult(bool success) : base(success)
        {
        }

        public SuccessResult(string message) : base(message, true)
        {
        }
    }
}
