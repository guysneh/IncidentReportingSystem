using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Comments.DTOs;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.Comments.Handlers
{
    /// <summary>
    /// Creates a comment after asserting incident and author existence.
    /// </summary>
    public sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
    {
        private readonly IIncidentCommentsRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IUserRepository _users; 

        public CreateCommentCommandHandler(
            IIncidentCommentsRepository repo,
            IUnitOfWork uow,
            IUserRepository users) 
        {
            _repo = repo;
            _uow = uow;
            _users = users;
        }

        /// <inheritdoc />
        public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken ct)
        {
            if (!await _repo.IncidentExistsAsync(request.IncidentId, ct))
                throw new KeyNotFoundException($"Incident {request.IncidentId} not found.");

            // NEW: ensure author exists
            if (!await _users.ExistsByIdAsync(request.AuthorId, ct))
                throw new KeyNotFoundException($"User {request.AuthorId} not found.");

            var entity = new IncidentComment
            {
                Id = Guid.NewGuid(),
                IncidentId = request.IncidentId,
                UserId = request.AuthorId,
                Text = request.Text.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return new CommentDto
            {
                Id = entity.Id,
                IncidentId = entity.IncidentId,
                UserId = entity.UserId,
                Text = entity.Text,
                CreatedAtUtc = entity.CreatedAtUtc
            };
        }
    }
}
