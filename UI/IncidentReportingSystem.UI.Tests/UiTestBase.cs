using Bunit;
using IncidentReportingSystem.UI.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

public abstract class UiTestBase : TestContext
{
    protected UiTestBase()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
