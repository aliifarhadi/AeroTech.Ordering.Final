using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.Ota
{
    public sealed class OtaAddRemarkCommandHandler : IRequestHandler<OtaAddRemarkCommand, AddRemarkResult>
    {
        private readonly IAddRemarkService _service;
        private readonly CallerOwnedOrderAuthorization _authorization;

        public OtaAddRemarkCommandHandler(IAddRemarkService service, CallerOwnedOrderAuthorization authorization)
        {
            _service = service;
            _authorization = authorization;
        }

        public Task<AddRemarkResult> Handle(OtaAddRemarkCommand command, CancellationToken cancellationToken)
        {
            var remark = AddRemarkArgsMapper.ToArgs(command.Remark);

            return command.SupersedesRemarkId is { } supersededId
                ? _service.ReviseAsync(command.OrderId, supersededId, remark, _authorization, cancellationToken)
                : _service.AddAsync(command.OrderId, remark, _authorization, cancellationToken);
        }
    }
}
