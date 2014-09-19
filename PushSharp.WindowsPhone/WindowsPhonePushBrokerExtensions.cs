using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PushSharp.WindowsPhone;
using PushSharp.Core;

namespace PushSharp
{
	public static class WindowsPhonePushBrokerExtensions
	{
		public static void RegisterWindowsPhoneService(this PushBroker broker, string applicationId, IPushServiceSettings serviceSettings = null)
		{
			RegisterWindowsPhoneService (broker, null, applicationId, serviceSettings);
		}

		public static void RegisterWindowsPhoneService(this PushBroker broker, IPushServiceSettings serviceSettings = null)
		{
			RegisterWindowsPhoneService (broker, null, null, serviceSettings);
		}

		public static void RegisterWindowsPhoneService(this PushBroker broker, WindowsPhonePushChannelSettings channelSettings, IPushServiceSettings serviceSettings = null)
		{
			RegisterWindowsPhoneService (broker, channelSettings, null, serviceSettings);
		}

		public static void RegisterWindowsPhoneService(this PushBroker broker, WindowsPhonePushChannelSettings channelSettings, string applicationId, IPushServiceSettings serviceSettings = null)
		{
			var service = new WindowsPhonePushService(new WindowsPhonePushChannelFactory(), channelSettings, serviceSettings);

			broker.RegisterService<WindowsPhoneCycleTileNotification>(service, applicationId, registerEvents:true);
            broker.RegisterService<WindowsPhoneFlipTileNotification>(service, applicationId, registerEvents: false);
            broker.RegisterService<WindowsPhoneIconicTileNotification>(service, applicationId, registerEvents: false);
            broker.RegisterService<WindowsPhoneTileNotification>(service, applicationId, registerEvents: false);
            broker.RegisterService<WindowsPhoneToastNotification>(service, applicationId, registerEvents: false);
            broker.RegisterService<WindowsPhoneRawNotification>(service, applicationId, registerEvents: false);
		}

		public static WindowsPhoneCycleTileNotification WindowsPhoneCycleTileNotification(this PushBroker broker)
		{
			return new WindowsPhoneCycleTileNotification();
		}

		public static WindowsPhoneFlipTileNotification WindowsPhoneFlipTileNotification(this PushBroker broker)
		{
			return new WindowsPhoneFlipTileNotification();
		}

		public static WindowsPhoneIconicTileNotification WindowsPhoneIconicTileNotification(this PushBroker broker)
		{
			return new WindowsPhoneIconicTileNotification();
		}

		public static WindowsPhoneTileNotification WindowsPhoneTileNotification(this PushBroker broker)
		{
			return new WindowsPhoneTileNotification();
		}

		public static WindowsPhoneToastNotification WindowsPhoneToastNotification(this PushBroker broker)
		{
			return new WindowsPhoneToastNotification();
		}

		public static WindowsPhoneRawNotification WindowsPhoneRawNotification(this PushBroker broker)
		{
			return new WindowsPhoneRawNotification();
		}
	}
}
