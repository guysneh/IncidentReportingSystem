using Microsoft.Extensions.Localization;

namespace IncidentReportingSystem.UI.Localization
{
    public sealed class AppTexts : IAppTexts
    {
        private readonly IStringLocalizer<AppTexts> _loc;
        public AppTexts(IStringLocalizer<AppTexts> loc) => _loc = loc;
        public string this[string key] => _loc[key];
        public string this[string key, params object[] args] => _loc[key, args];
    }
}
