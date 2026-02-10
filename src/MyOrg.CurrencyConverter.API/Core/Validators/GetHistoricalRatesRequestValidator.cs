using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.Models.Requests;

namespace MyOrg.CurrencyConverter.API.Core.Validators
{
    public class GetHistoricalRatesRequestValidator : AbstractValidator<GetHistoricalRatesRequest>
    {
        public GetHistoricalRatesRequestValidator()
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty()
                .WithMessage("Base currency is required")
                .Length(3)
                .WithMessage("Base currency must be a 3-letter code")
                .Matches("^[A-Z]{3}$")
                .WithMessage("Base currency must contain only uppercase letters");

            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate)
                .WithMessage("Start date must be before end date");

            RuleFor(x => x.EndDate)
                .LessThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("End date cannot be in the future");
        }
    }
}
