using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;

namespace MyOrg.CurrencyConverter.API.Core.Validators
{
    public class GetExchangeRateRequestValidator : AbstractValidator<GetExchangeRateRequest>
    {
        public GetExchangeRateRequestValidator()
        {
            RuleFor(x => x.From)
                .NotEmpty()
                .WithMessage("Source currency is required")
                .Length(3)
                .WithMessage("Source currency must be a 3-letter code")
                .Matches("^[A-Z]{3}$")
                .WithMessage("Source currency must contain only uppercase letters");

            RuleFor(x => x.To)
                .NotEmpty()
                .WithMessage("Target currency is required")
                .Length(3)
                .WithMessage("Target currency must be a 3-letter code")
                .Matches("^[A-Z]{3}$")
                .WithMessage("Target currency must contain only uppercase letters");
        }
    }
}
