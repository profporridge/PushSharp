using System;
using System.Threading;
using PushSharp.Core;

namespace PushSharp.Apple
{
    public class ApplePushService : PushServiceBase
    {
        FeedbackService feedbackService;
        CancellationTokenSource cancelTokenSource;
        Timer timerFeedback = null;


        public ApplePushService(IPushChannelSettings channelSettings)
            : this(default(IPushChannelFactory), channelSettings, default(IPushServiceSettings))
        {
        }

        public ApplePushService(IPushChannelSettings channelSettings, IPushServiceSettings serviceSettings)
            : this(default(IPushChannelFactory), channelSettings, serviceSettings)
        {
        }

        public ApplePushService(IPushChannelFactory pushChannelFactory, IPushChannelSettings channelSettings)
            : this(pushChannelFactory, channelSettings, default(IPushServiceSettings))
        {
        }

        public ApplePushService(IPushChannelFactory pushChannelFactory, IPushChannelSettings channelSettings, IPushServiceSettings serviceSettings)
            : base(pushChannelFactory ?? new ApplePushChannelFactory(), channelSettings, serviceSettings)
        {
            var appleChannelSettings = channelSettings as ApplePushChannelSettings;
            if (appleChannelSettings != null)
            {
                cancelTokenSource = new CancellationTokenSource();

                //allow control over feedback call interval, if set to zero, don't make feedback calls automatically
                if (appleChannelSettings.FeedbackIntervalMinutes > 0)
                {
                    feedbackService = new FeedbackService();
                    feedbackService.OnFeedbackReceived += feedbackService_OnFeedbackReceived;
                    feedbackService.OnFeedbackException += (Exception ex) => this.RaiseServiceException(ex);

                    if (timerFeedback == null)
                    {
                        timerFeedback = new Timer(new TimerCallback((state) =>
                        {
                            try { feedbackService.Run(channelSettings as ApplePushChannelSettings, this.cancelTokenSource.Token); }
                            catch (Exception ex) { base.RaiseServiceException(ex); }

                            //Timer will run first after 10 seconds, then every 10 minutes to get feedback!
                        }), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(appleChannelSettings.FeedbackIntervalMinutes));
                    }
                }
            }

            // Apple has documented that they only want us to use 20 connections to them
            base.ServiceSettings.MaxAutoScaleChannels = 20;
        }

        /// fired exclusively by the feedback service. 
        void feedbackService_OnFeedbackReceived(string deviceToken, DateTime timestamp)
        {
            base.RaiseSubscriptionExpired(deviceToken, timestamp.ToUniversalTime(), null);
        }

        public override bool BlockOnMessageResult
        {
            get { return false; }
        }
    }
}
