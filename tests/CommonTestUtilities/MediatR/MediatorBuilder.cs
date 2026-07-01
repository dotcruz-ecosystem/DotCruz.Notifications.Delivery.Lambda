using Moq;
using Mediator;

namespace CommonTestUtilities.MediatR;

public class MediatorBuilder
{
    private readonly Mock<IMediator> _mediator;

    public MediatorBuilder()
    {
        _mediator = new Mock<IMediator>();
    }

    public IMediator Build() => _mediator.Object;
    public Mock<IMediator> GetMock() => _mediator;
}
