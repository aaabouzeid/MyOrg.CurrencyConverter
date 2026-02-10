using FluentValidation;
using MyOrg.CurrencyConverter.API.Core.DTOs.Requests;

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

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page number must be greater than or equal to 1");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page size must be greater than or equal to 1");
        }
    }
}
