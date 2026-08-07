using eCommerce.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using ValidationException = eCommerce.Application.Common.Exceptions.ValidationException;

namespace eCommerce.Application.UnitTests.Common.Behaviours;

public class ValidationBehaviourTests
{
    public sealed record SampleRequest(string Name, int Quantity) : IRequest<string>;

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(r => r.Name).NotEmpty();
            RuleFor(r => r.Quantity).GreaterThan(0);
        }
    }

    private static readonly RequestHandlerDelegate<string> Next = _ => Task.FromResult("handled");

    [Fact]
    public async Task Handle_InvokesTheHandlerWhenThereAreNoValidators()
    {
        var behaviour = new ValidationBehaviour<SampleRequest, string>([]);

        var result = await behaviour.Handle(new SampleRequest("", -1), Next, CancellationToken.None);

        result.Should().Be("handled");
    }

    [Fact]
    public async Task Handle_InvokesTheHandlerWhenTheRequestIsValid()
    {
        var behaviour = new ValidationBehaviour<SampleRequest, string>([new SampleValidator()]);

        var result = await behaviour.Handle(new SampleRequest("ok", 1), Next, CancellationToken.None);

        result.Should().Be("handled");
    }

    [Fact]
    public async Task Handle_ThrowsWithEveryFailureGroupedByProperty()
    {
        var behaviour = new ValidationBehaviour<SampleRequest, string>([new SampleValidator()]);
        var handlerWasCalled = false;

        var act = () => behaviour.Handle(
            new SampleRequest("", 0),
            token =>
            {
                handlerWasCalled = true;
                return Next(token);
            },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();

        exception.Which.Errors.Should().ContainKeys(
            nameof(SampleRequest.Name),
            nameof(SampleRequest.Quantity));

        handlerWasCalled.Should().BeFalse();
    }
}
