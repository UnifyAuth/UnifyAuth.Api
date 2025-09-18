using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Results.Concrete
{
    public sealed record AppError(string Code)
    {
        public static AppError NotFound() => new("NotFound");
        public static AppError Validation() => new("Validation");
        public static AppError BadRequest() => new("BadRequest");
        public static AppError ConcurrencyFailure() => new("ConcurrencyFailure");
        public static AppError Unauthorized() => new("Unauthorized");
        public static AppError InternalServerError() => new("InternalServerError");
    }
}
