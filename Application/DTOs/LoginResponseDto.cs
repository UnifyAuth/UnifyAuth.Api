using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public sealed record LoginResponseDto
    {
        public string UserId { get; init; }
        public bool IsTowFactorRequired { get; init; }
        public string Provider { get; init; }
        public TokenResultDto? TokenResultDto { get; init; }
    }
}
