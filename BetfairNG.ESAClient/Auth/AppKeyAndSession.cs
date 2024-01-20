using System;

namespace Betfair.ESAClient.Auth
{
    /// <summary>
    /// Wraps an appkey & it's current session
    /// </summary>
    public class AppKeyAndSession(string appkey, string session)
    {
        public string AppKey { get; private set; } = appkey;

        public DateTime CreateTime { get; private set; } = DateTime.UtcNow;

        public string Session { get; private set; } = session;
    }
}