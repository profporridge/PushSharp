using System;
using PushSharp.Core;

namespace PushSharp.Apple
{
	public class ApplePushChannelFactory : IPushChannelFactory
	{
		public IPushChannel CreateChannel(IPushChannelSettings channelSettings)
		{
			if (channelSettings is ApplePushChannelSettings)
				return new ApplePushChannel(channelSettings as ApplePushChannelSettings);

			if (channelSettings is AppleHttpPushChannelSettings)
				return new AppleHttpPushChannel(channelSettings as AppleHttpPushChannelSettings);

			throw new ArgumentException("Channel Settings not supported.");
		}
	}
}