﻿using Newtonsoft.Json;
using PushSharp.Core;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace PushSharp.Apple
{
    public class AppleHttpPushChannel : IPushChannel
    {
        private readonly Guid _channelInstanceId = Guid.NewGuid();
        private readonly AppleHttpPushChannelSettings _appleSettings;
        private readonly HttpClient _httpClient;
        private readonly CngKey _privateKey;

        private string _currentJWT;
        private DateTime? _JWTExpiryDate;

        private object _lock = new object();

        public AppleHttpPushChannel(AppleHttpPushChannelSettings channelSettings)
        {
            Log.Debug("Creating ApplePushChannel instance " + _channelInstanceId);

            _appleSettings = channelSettings;
            _privateKey = CngKey.Import(Convert.FromBase64String(channelSettings.PrivateKey), CngKeyBlobFormat.Pkcs8PrivateBlob);

            var requestHandler = new WinHttpHandler() { SslProtocols = SslProtocols.Tls12 };
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
                HttpResponseMessage response;

                var path = $"/3/device/{appleNotification.DeviceToken}";
                using (var message = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    message.Version = new Version(2, 0);
                    message.Headers.TryAddWithoutValidation(":method", "POST");
                    message.Headers.TryAddWithoutValidation(":path", path);
                    message.Headers.Add("apns-expiration", appleNotification.ExpirationEpochSeconds.ToString());
                    message.Headers.Add("apns-push-type", Enum.GetName(typeof(AppleNotificationType), appleNotification.Type).ToLower());
                    message.Headers.Add("authorization", $"bearer {GetJwtToken()}");
                    message.Headers.Add("apns-topic", appleNotification.Type == AppleNotificationType.Voip ? $"{_appleSettings.BundleId}.voip" : _appleSettings.BundleId);
                    //message.Headers.Add("apns-id", ); -> apple creates one if not provided
                    //message.Headers.Add("apns-priority", ""); -> apple default 10, send immediately
                    //message.Headers.Add("apns-collapse-id", ""); -> not used

                    message.Content = new StringContent(appleNotification.Payload.ToString());
                    response = _httpClient.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                if (response.IsSuccessStatusCode)
                {
                    if (callback != null)
                        callback(this, new SendNotificationResult(notification));
                }
                else
                {
                    var responseBody = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    Log.Error("Error during APNS Send with channel {0}: {1} -> Code {2} - {3} with Device token {4}.",
                                        _channelInstanceId,
                                        appleNotification.Identifier,
                                        response.StatusCode,
                                        string.IsNullOrEmpty(responseBody) ? "No response body" : responseBody,
                                        appleNotification.DeviceToken);

                    if (response.StatusCode == HttpStatusCode.Gone && callback != null)
                    {
                        var result = new SendNotificationResult(notification, false, new Exception("Device token no longer available on APNs."))
                        {
                            IsSubscriptionExpired = true,
                            OldSubscriptionId = appleNotification.DeviceToken,
                            SubscriptionExpiryUtc = DateTime.UtcNow
                        };
                        callback(this, result);
                        return;
                    }

                    if (callback != null)
                    {
                        callback(this, new SendNotificationResult(notification, false,
                                    new Exception("Error during APNS Send.", new Exception(string.Format("Error during APNS Send with channel {0}: {1} -> Code {2} - {3} with Device token {4}.",
                                        _channelInstanceId,
                                        appleNotification.Identifier,
                                        response.StatusCode,
                                        string.IsNullOrEmpty(responseBody) ? "No response body" : responseBody,
                                        appleNotification.DeviceToken)))));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception during APNS Send with channel {2}: {0} -> {1} with Device token {2}.",
                    appleNotification.Identifier,
                    ex,
                    _channelInstanceId,
                    appleNotification.DeviceToken);

                if (callback != null)
                {
                    // will requeue the notification
                    callback(this, new SendNotificationResult(notification, true, ex));
                }
            }
        }

        private string GetJwtToken()
        {
            if (!string.IsNullOrEmpty(_currentJWT) && _JWTExpiryDate.HasValue && DateTime.UtcNow <= _JWTExpiryDate.Value)
            {
                return _currentJWT;
            }

            return CreateJWTToken();
        }

        private string CreateJWTToken()
        {
            lock (_lock)
            {
                if (_JWTExpiryDate.HasValue && DateTime.UtcNow <= _JWTExpiryDate.Value)
                {
                    return _currentJWT;
                }

                var dateTimeUtcNow = DateTime.UtcNow;
                var header = JsonConvert.SerializeObject(new { alg = "ES256", kid = _appleSettings.KeyId });
                var payload = JsonConvert.SerializeObject(new { iss = _appleSettings.TeamId, iat = ToEpoch(dateTimeUtcNow) });

                using (var dsa = new ECDsaCng(_privateKey))
                {
                    dsa.HashAlgorithm = CngAlgorithm.Sha256;
                    var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(header));
                    var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
                    var unsignedJwtData = $"{headerBase64}.{payloadBase64}";
                    var signature = dsa.SignData(Encoding.UTF8.GetBytes(unsignedJwtData));
                    _JWTExpiryDate = dateTimeUtcNow.AddMinutes(50); // adds expiration of 50m for the token - APNs expects the token to have a TTL between 20m and 60m
                    _currentJWT = $"{unsignedJwtData}.{Convert.ToBase64String(signature)}";
                    return _currentJWT;
                }
            }
        }

        /*
         * This would be the JWT generation based on a ECDsa key, uses Microsoft JWT package

            private static string CreateJwt(ECDsa key, string keyId, string teamId)
            {
                var signingCredentials = new SigningCredentials(new ECDsaSecurityKey(key), SecurityAlgorithms.EcdsaSha256);

                var now = DateTime.UtcNow;

                var claims = new List<Claim>
                {
                    new Claim(ClaimConstants.Issuer, teamId),
                    new Claim(ClaimConstants.IssuedAt, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
                };

                var tokenJWT = new JwtSecurityToken(
                    issuer: teamId,
                    claims: claims,
                    signingCredentials: signingCredentials
                );

                tokenJWT.Header.Add(ClaimConstants.KeyID, keyId);
                JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
                return _tokenHandler.WriteToken(tokenJWT);
            }
         */

        private static int ToEpoch(DateTime date)
        {
            var span = date - new DateTime(1970, 1, 1);
            return Convert.ToInt32(span.TotalSeconds);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
