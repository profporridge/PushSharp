using PushSharp.Apple;
using PushSharp.Core;

namespace PushSharp
{
    public static class ApplePushBrokerExtensions
	{
		public static void RegisterAppleService(this PushBroker broker, IPushChannelSettings channelSettings, IPushServiceSettings serviceSettings = null)
		{
			RegisterAppleService (broker, channelSettings, null, serviceSettings);
		}

		public static void RegisterAppleService(this PushBroker broker, IPushChannelSettings channelSettings, string applicationId, IPushServiceSettings serviceSettings = null)
		{
			broker.RegisterService<AppleNotification>(new ApplePushService(channelSettings, serviceSettings), applicationId);
		}

		public static AppleNotification AppleNotification(this PushBroker broker)
		{
			return new AppleNotification();
		}
	}
}