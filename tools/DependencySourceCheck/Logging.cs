namespace Colossal.Logging;
internal interface ILog
{
    ILog SetShowsErrorsInUI(bool value);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
internal static class LogManager
{
    internal static ILog GetLogger(string name) => new FixtureLog();
    private sealed class FixtureLog : ILog
    {
        public ILog SetShowsErrorsInUI(bool value) => this;
        public void Info(string message) => BridgeBuilder.Mod.Log.Info(message);
        public void Warn(string message) { }
        public void Error(string message) { }
    }
}
