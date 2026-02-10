using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;

namespace MyOrg.CurrencyConverter.API.Core.Validators
{
    public class GetLatestRatesRequestValidator : AbstractValidator<GetLatestRatesRequest>
    {
        public GetLatestRatesRequestValidator()
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty()
                .WithMessage("Base currency is required")
                .Length(3)
                .WithMessage("Base currency must be a 3-letter code")
                .Matches("^[A-Z]{3}$")
                .WithMessage("Base currency must contain only uppercase letters");
        }
    }
}
