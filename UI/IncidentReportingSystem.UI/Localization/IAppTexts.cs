namespace IncidentReportingSystem.UI.Localization
{
    public interface IAppTexts
    {
        string this[string key] { get; }
        string this[string key, params object[] args] { get; }
    }
}
