using PushSharp.Core;

namespace PushSharp.Apple
{
    public class AppleHttpPushChannelSettings : IPushChannelSettings
    {
        public const string BundleId = "com.pof.mobileapp.iphone";

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string TeamId { get; private set; }
        public string KeyId { get; private set; }
        public string PrivateKey { get; private set; }

        public AppleHttpPushChannelSettings(string host, int port, string teamId, string keyId, string privateKey)
        {
            Host = host;
            Port = port;
            TeamId = teamId;
            KeyId = keyId;
            PrivateKey = privateKey;

            Initialize();
        }

        private void Initialize()
        {
        }
    }
}