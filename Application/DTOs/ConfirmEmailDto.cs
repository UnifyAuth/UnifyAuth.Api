﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ConfirmEmailDto
    {
        public Guid UserId { get; set; }
        public string Token { get; set; }
    }
}
