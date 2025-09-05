using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ConfirmEmailTokenDto
    {
        public Guid UserId { get; set; }
        public string Token { get; set; }
    }
}
