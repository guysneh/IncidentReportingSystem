//using Bunit;
//using IncidentReportingSystem.UI.Core.Options;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Xunit;
//
//namespace IncidentReportingSystem.UI.Tests
//{
//    /// <summary>
//    /// Smoke tests for the Home page to validate rendering and configuration banner logic.
//    /// </summary>
//    public sealed class HomeSmokeTest : UiTestBase
//    {
//        [Fact]
//        public void Home_Renders_And_Shows_Alive_When_ApiUrl_Present()
//        {
//            Services.Configure<ApiOptions>(o => o.BaseUrl = "https://example.test/api/v1/");
//
//            var cut = RenderComponent<IncidentReportingSystem.UI.Pages>();
//
//            Assert.Contains("App is alive", cut.Markup);
//            Assert.DoesNotContain("Api:BaseUrl is missing", cut.Markup);
//        }
//
//        [Fact]
//        public void Home_Shows_Banner_When_ApiUrl_Missing()
//        {
//            Services.Configure<ApiOptions>(o => o.BaseUrl = "");
//
//            var cut = RenderComponent<IncidentReportingSystem.UI.Pages>();
//
//            Assert.Contains("Api:BaseUrl is missing", cut.Markup);
//        }
//    }
//}
