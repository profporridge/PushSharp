using PushSharp.Core;
using System;
using System.Net.Http;
using System.Text;

namespace PushSharp.Apple
{
    public class AppleHttpPushChannel : IPushChannel
    {
        private readonly Guid _channelInstanceId = Guid.NewGuid();
        private readonly ApplePushChannelSettings _appleSettings;
        private readonly HttpClient _httpClient;

        public AppleHttpPushChannel(ApplePushChannelSettings channelSettings)
        {
            Log.Debug("Creating ApplePushChannel instance " + _channelInstanceId);

            _appleSettings = channelSettings;

            var requestHandler = new Http2CustomHandler();
            requestHandler.ClientCertificates.Add(_appleSettings.Certificate);
            _httpClient = new HttpClient(requestHandler, true)
            {
                BaseAddress = new Uri($"https://{_appleSettings.Host}:{_appleSettings.Port}")
            };
        }

        public void SendNotification(INotification notification, SendNotificationCallbackDelegate callback)
        {
            var appleNotification = notification as AppleNotification;
            if (appleNotification == null)
            {
                throw new ArgumentException("Notification was not an AppleNotification", "notification");
            }

            try
            {
                var message = new HttpRequestMessage(HttpMethod.Post, $"/3/device/{appleNotification.DeviceToken}");
                message.Headers.Add("apns-id", appleNotification.Identifier.ToString());
                message.Headers.Add("apns-expiration", appleNotification.ExpirationEpochSeconds.ToString());
                message.Headers.Add("apns-push-type", Enum.GetName(typeof(AppleNotificationType), appleNotification.Type).ToLower());
                //message.Headers.Add("apns-priority", ""); // default 10, send immediately
                //message.Headers.Add("apns-topic", "");
                //message.Headers.Add("apns-collapse-id", "");

                message.Content = new StringContent(appleNotification.Payload.ToString(), Encoding.UTF8, "application/json");

                var response = _httpClient.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    if (callback != null)
                        callback(this, new SendNotificationResult(notification));
                }
                else
                {

                }





                // TODO error handling/retries?
                // TODO feedservice logic (need to find the response status and call delegate)




            }
            catch (Exception ex)
            {
                Log.Error("Exception during APNS Send with channel {2}: {0} -> {1}",
                    appleNotification.Identifier,
                    ex,
                    _channelInstanceId);

                if (callback != null)
                {
                    // will requeue the notification
                    callback(this, new SendNotificationResult(notification, true, ex));
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
