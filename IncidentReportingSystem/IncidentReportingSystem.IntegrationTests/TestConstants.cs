namespace IncidentReportingSystem.IntegrationTests
{
    public static class TestConstants
    {
        public const string ApiVersion = "v1";

        // JWT test settings (used both by token generator and API config in tests)
        public const string JwtIssuer = "irs-tests-issuer";
        public const string JwtAudience = "irs-tests-audience";
        public const string JwtSigningKey = "irs-tests-signing-key-32-bytes-min!!"; // ≥ 32 chars
    }
}
