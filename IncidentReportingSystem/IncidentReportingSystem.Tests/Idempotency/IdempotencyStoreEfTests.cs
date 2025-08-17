using System.Text.Json;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Services.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Tests.Idempotency
{
    public class IdempotencyStoreEfTests
    {
        private static ApplicationDbContext NewDb()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(opts);
        }

        // Strongly-typed response shape for (de)serialization in tests
        private sealed class TestResponse
        {
            public int Updated { get; set; }
            public Guid[] NotFound { get; set; } = Array.Empty<Guid>();
            public string IdempotencyKey { get; set; } = string.Empty;
        }

        [Fact]
        public async Task PutIfAbsent_Then_TryGet_Returns_Same_Response()
        {
            using var db = NewDb();
            var store = new IdempotencyStoreEf(db);
            var key = "K1";
            var payload = new { Ids = new[] { Guid.NewGuid() }, Status = "Closed" };
            var response = new TestResponse { Updated = 1, NotFound = Array.Empty<Guid>(), IdempotencyKey = key };

            // TResponse inferred as TestResponse
            var stored = await store.PutIfAbsentAsync(key, payload, response, TimeSpan.FromHours(24), default);
            Assert.Equal(JsonSerializer.Serialize(response), JsonSerializer.Serialize(stored));

            // Deserialize back into the same strong type (not dynamic)
            var fetched = await store.TryGetAsync<object, TestResponse>(key, payload, default);
            Assert.NotNull(fetched);
            Assert.Equal(response.Updated, fetched!.Updated);
            Assert.Equal(response.IdempotencyKey, fetched.IdempotencyKey);
        }

        [Fact]
        public async Task SameKey_DifferentPayload_FirstWriteWins()
        {
            using var db = NewDb();
            var store = new IdempotencyStoreEf(db);
            var key = "K2";

            var payloadA = new { Ids = new[] { Guid.NewGuid() }, Status = "Closed" };
            var respA = new TestResponse { Updated = 1, NotFound = Array.Empty<Guid>(), IdempotencyKey = key };
            await store.PutIfAbsentAsync(key, payloadA, respA, TimeSpan.FromHours(24), default);

            var payloadB = new { Ids = new[] { Guid.NewGuid(), Guid.NewGuid() }, Status = "Open" };
            var respB = new TestResponse { Updated = 2, NotFound = Array.Empty<Guid>(), IdempotencyKey = key };
            var returned = await store.PutIfAbsentAsync(key, payloadB, respB, TimeSpan.FromHours(24), default);

            // First-Write-Wins => original response is returned
            Assert.Equal(respA.Updated, returned.Updated);
            Assert.Equal(respA.IdempotencyKey, returned.IdempotencyKey);
        }
    }
}
