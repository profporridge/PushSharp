﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PushSharp.Apple
{
    public class FeedbackService
    {
        public delegate void FeedbackReceivedDelegate(string deviceToken, DateTime timestamp);

        public event FeedbackReceivedDelegate OnFeedbackReceived;

        public delegate void FeedbackExceptionDelegate(Exception ex);

        public event FeedbackExceptionDelegate OnFeedbackException;

        public void RaiseFeedbackReceived(string deviceToken, DateTime timestamp)
        {
            var evt = this.OnFeedbackReceived;
            if (evt != null)
                evt(deviceToken, timestamp);
        }

        public void RaiseFeedbackException(Exception ex)
        {
            var evt = this.OnFeedbackException;
            if (evt != null)
                evt(ex);
        }

        public void Run(ApplePushChannelSettings settings)
        {
            try
            {
                Run(settings, (new CancellationTokenSource()).Token);
            }
            catch (Exception ex)
            {
                this.RaiseFeedbackException(ex);
            }
        }

        public void Run(ApplePushChannelSettings settings, CancellationToken cancelToken)
        {
            var encoding = Encoding.ASCII;

            var certificate = settings.Certificate;

            var certificates = new X509CertificateCollection();
            certificates.Add(certificate);


            var client = new TcpClient(settings.FeedbackHost, settings.FeedbackPort);

            var stream = new SslStream(client.GetStream(), true,
                (sender, cert, chain, sslErrs) => { return true; },
                (sender, targetHost, localCerts, remoteCert, acceptableIssuers) => { return certificate; });

            stream.AuthenticateAsClient(settings.FeedbackHost, certificates, SslProtocols.Tls11 | SslProtocols.Tls12, false);


            //Set up
            byte[] buffer = new byte[1482];
            int bufferIndex = 0;
            int bufferLevel = 0;
            int completePacketSize = 4 + 2 + 32;
            int recd = 0;
            DateTime minTimestamp = DateTime.Now.AddYears(-1);

            //Get the first feedback
            recd = stream.Read(buffer, 0, buffer.Length);

            //Continue while we have results and are not disposing
            while (recd > 0 && !cancelToken.IsCancellationRequested)
            {
                //Update how much data is in the buffer, and reset the position to the beginning
                bufferLevel += recd;
                bufferIndex = 0;

                try
                {
                    //Process each complete notification "packet" available in the buffer
                    while (bufferLevel - bufferIndex >= completePacketSize)
                    {
                        //Get our seconds since 1970 ?
                        byte[] bSeconds = new byte[4];
                        byte[] bDeviceToken = new byte[32];

                        Array.Copy(buffer, bufferIndex, bSeconds, 0, 4);

                        //Check endianness
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bSeconds);

                        int tSeconds = BitConverter.ToInt32(bSeconds, 0);

                        //Add seconds since 1970 to that date, in UTC
                        var timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(tSeconds);

                        //flag to allow feedback times in UTC or local, but default is local
                        if (!settings.FeedbackTimeIsUTC)
                            timestamp = timestamp.ToLocalTime();

                        //Now copy out the device token
                        Array.Copy(buffer, bufferIndex + 6, bDeviceToken, 0, 32);

                        var deviceToken = BitConverter.ToString(bDeviceToken).Replace("-", "").ToLower().Trim();

                        //Make sure we have a good feedback tuple
                        if (deviceToken.Length == 64
                            && timestamp > minTimestamp)
                        {
                            //Raise event
                            try
                            {
                                RaiseFeedbackReceived(deviceToken, timestamp);
                            }
                            catch { }
                        }

                        //Keep track of where we are in the received data buffer
                        bufferIndex += completePacketSize;
                    }
                }
                catch { }

                //Figure out how much data we have left over in the buffer still
                bufferLevel -= bufferIndex;

                //Copy any leftover data in the buffer to the start of the buffer
                if (bufferLevel > 0)
                    Array.Copy(buffer, bufferIndex, buffer, 0, bufferLevel);

                //Read the next feedback
                recd = stream.Read(buffer, bufferLevel, buffer.Length - bufferLevel);
            }

            try
            {
                stream.Close();
                stream.Dispose();
            }
            catch { }

            try
            {
                client.Client.Shutdown(SocketShutdown.Both);
                client.Client.Dispose();
            }
            catch { }

            try { client.Close(); }
            catch { }

        }
    }
}