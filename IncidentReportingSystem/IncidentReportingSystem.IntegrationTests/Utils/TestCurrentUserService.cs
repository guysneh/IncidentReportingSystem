using IncidentReportingSystem.Application.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    /// <summary>
    /// Test implementation of ICurrentUserService for integration tests.
    /// Always authenticated, returns a fixed Guid user id, and grants all permissions.
    /// </summary>
    public sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        /// <summary>
        /// Creates a test user service with a fixed Guid user id.
        /// </summary>
        /// <param name="userId">
        /// Optional explicit user id. If not provided, a deterministic test Guid is used.
        /// </param>
        public TestCurrentUserService(Guid? userId = null)
        {
            _userId = userId ?? new Guid("00000000-0000-0000-0000-000000000001");
        }

        // ====== match your interface members ======
        // If your interface exposes Guid? UserId — this fits.
        // If it's Guid (non-nullable), just change the property type to Guid and return _userId.
        /// <summary>Gets the current authenticated user id (test value).</summary>
        public Guid? UserId => _userId;

        /// <summary>Returns the current user id or throws in real impls; here always returns the fixed Guid.</summary>
        public Guid UserIdOrThrow() => _userId;

        /// <summary>In tests we are always authenticated.</summary>
        public bool IsAuthenticated => true;

        /// <summary>Grants any permission in tests.</summary>
        public bool HasPermission(string permission) => true;

        /// <summary>Roles exposed for completeness; adjust/remove if your interface doesn't have it.</summary>
        public IEnumerable<string> Roles => new[] { "Admin" };
        // ==========================================
    }
}
