using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.Backoffice
{
    public sealed class BackofficeAddRemarkCommandHandler : IRequestHandler<BackofficeAddRemarkCommand, AddRemarkResult>
    {
        private readonly IAddRemarkService _service;

        public BackofficeAddRemarkCommandHandler(IAddRemarkService service) => _service = service;

        public Task<AddRemarkResult> Handle(BackofficeAddRemarkCommand command, CancellationToken cancellationToken)
        {
            var remark = AddRemarkArgsMapper.ToArgs(command.Remark);

            return command.SupersedesRemarkId is { } supersededId
                ? _service.ReviseAsync(command.OrderId, supersededId, remark, UnrestrictedOrderAuthorization.Instance, cancellationToken)
                : _service.AddAsync(command.OrderId, remark, UnrestrictedOrderAuthorization.Instance, cancellationToken);
        }
    }
}
