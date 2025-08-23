using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus;
using IncidentReportingSystem.Domain.Enums;
using Moq;

namespace IncidentReportingSystem.Tests.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus
{
    public class BulkUpdateIncidentStatusHandlerTests
    {
        // === Fake store to avoid Moq generic gymnastics (It.IsAnyType) ===
        private sealed class FakeIdempotencyStore : IIdempotencyStore
        {
            public object? CachedResponse { get; set; }

            public (string key, object payload, object response, TimeSpan ttl, CancellationToken ct)?
                LastPutArgs
            { get; private set; }

            public Task<TResponse?> TryGetAsync<TPayload, TResponse>(string key, TPayload payload, CancellationToken ct)
                => Task.FromResult((TResponse?)CachedResponse);

            public Task<TResponse> PutIfAbsentAsync<TPayload, TResponse>(string key, TPayload payload, TResponse response, TimeSpan ttl, CancellationToken ct)
            {
                LastPutArgs = (key, payload!, response!, ttl, ct);
                return Task.FromResult(response);
            }
        }

        private readonly Mock<IIncidentReportRepository> _repo = new();
        private readonly FakeIdempotencyStore _store = new();
        private readonly BulkUpdateIncidentStatusHandler _sut;

        public BulkUpdateIncidentStatusHandlerTests()
        {
            _sut = new BulkUpdateIncidentStatusHandler(_repo.Object, _store);
        }

        [Fact]
        public async Task Returns_Cached_Response_When_TryGet_Hits_Store()
        {
            // Arrange
            var key = "k1";
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var cached = new BulkStatusUpdateResultDto
            {
                Updated = 2,
                NotFound = Array.Empty<Guid>(),
                IdempotencyKey = key
            };
            _store.CachedResponse = cached; // cache hit

            var cmd = new BulkUpdateIncidentStatusCommand(key, ids, IncidentStatus.Closed);

            // Act
            var result = await _sut.Handle(cmd, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(cached);
            _repo.Verify(r => r.BulkUpdateStatusAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<IncidentStatus>(), It.IsAny<CancellationToken>()), Times.Never);
            _store.LastPutArgs.Should().BeNull(); 
        }

        [Fact]
        public async Task Miss_Then_Updates_Repo_And_Persists_For_24h()
        {
            // Arrange
            var key = "k2";
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            _store.CachedResponse = null; // cache miss

            var repoResult = (UpdatedCount: 2, NotFound: new List<Guid> { ids[2] });
            _repo
                .Setup(r => r.BulkUpdateStatusAsync(ids, IncidentStatus.InProgress, It.IsAny<CancellationToken>()))
                .ReturnsAsync(repoResult);

            var cmd = new BulkUpdateIncidentStatusCommand(key, ids, IncidentStatus.InProgress);

            // Act
            var result = await _sut.Handle(cmd, CancellationToken.None);

            // Assert 
            result.Updated.Should().Be(2);
            result.NotFound.Should().ContainSingle().Which.Should().Be(ids[2]);
            result.IdempotencyKey.Should().Be(key);

            // Assert 
            _store.LastPutArgs.Should().NotBeNull();
            var put = _store.LastPutArgs!.Value;
            put.key.Should().Be(key);
            put.response.Should().BeEquivalentTo(result);
            put.ttl.TotalHours.Should().BeApproximately(24d, precision: 0.1);

            _repo.VerifyAll();
        }

        [Fact]
        public async Task Throws_When_Ids_Empty()
        {
            var cmd = new BulkUpdateIncidentStatusCommand("k3", Array.Empty<Guid>(), IncidentStatus.Closed);
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.Handle(cmd, CancellationToken.None));
        }
    }
}
