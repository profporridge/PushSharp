using PushSharp.Core;

namespace PushSharp.Apple
{
    public class AppleHttpPushChannelSettings : IPushChannelSettings
    {
        public string BundleId { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string TeamId { get; private set; }
        public string KeyId { get; private set; }
        public string PrivateKey { get; private set; }
        public bool EnableDeleteToken { get; private set; }

        public AppleHttpPushChannelSettings(string host, int port, string teamId, string keyId, string privateKey, string bundleId, bool enableDeleteToken)
        {
            Host = host;
            Port = port;
            TeamId = teamId;
            KeyId = keyId;
            PrivateKey = privateKey;
            BundleId = bundleId;
            EnableDeleteToken = enableDeleteToken;

            Initialize();
        }

        private void Initialize()
        {
        }
    }
}