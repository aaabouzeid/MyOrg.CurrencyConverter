using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;

namespace MyOrg.CurrencyConverter.API.Core.Validators
{
    public class ConvertCurrencyRequestValidator : AbstractValidator<ConvertCurrencyRequest>
    {
        private readonly string[] _restrictedCurrencies;

        public ConvertCurrencyRequestValidator(string[] restrictedCurrencies)
        {
            _restrictedCurrencies = restrictedCurrencies ?? Array.Empty<string>();

            RuleFor(x => x.From)
                .NotEmpty()
                .WithMessage("Source currency is required")
                .Length(3)
                .WithMessage("Source currency must be a 3-letter code")
                .Matches("^[A-Z]{3}$")
                .WithMessage("Source currency must contain only uppercase letters")
                .Must(currency => !_restrictedCurrencies.Contains(currency))
                .WithMessage(x => $"Currency conversion is not supported for {x.From}. Restricted currencies: {string.Join(", ", _restrictedCurrencies)}");

            RuleFor(x => x.To)
                .NotEmpty()
                .WithMessage("Target currency is required")
                .Length(3)
                .WithMessage("Target currency must be a 3-letter code")
                .Matches("^[A-Z]{3}$")
                .WithMessage("Target currency must contain only uppercase letters")
                .Must(currency => !_restrictedCurrencies.Contains(currency))
                .WithMessage(x => $"Currency conversion is not supported for {x.To}. Restricted currencies: {string.Join(", ", _restrictedCurrencies)}");

            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Amount must be greater than or equal to zero");
        }
    }
}
