using Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Validators
{
    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(dto => dto.NewPassword)
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^a-zA-Z0-9\s]").WithMessage("Password must contain at least one special character.");
            RuleFor(dto => dto.ConfirmedPassword)
                .NotEmpty().WithMessage("Confirmed password is required.")
                .Equal(x => x.NewPassword).WithMessage("Confirmed password must match the password.");
        }
    }
}
