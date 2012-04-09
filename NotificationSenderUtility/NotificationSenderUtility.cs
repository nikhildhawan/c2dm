// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace WindowsPhone.PushNotificationManager
{
    #region NotificationType Enum
    public enum NotificationType
    {
        Token = 1,
        Toast = 2,
        Raw = 3
    }
    #endregion

    #region Event & Event Handler Definition
    public class CallbackArgs
    {
        public CallbackArgs(NotificationType notificationType, HttpWebResponse response)
        {
            this.Timestamp = DateTimeOffset.Now;
            this.MessageId = response.Headers[NotificationSenderUtility.MESSAGE_ID_HEADER];
            this.ChannelUri = response.ResponseUri.ToString();
            this.NotificationType = notificationType;
            this.StatusCode = response.StatusCode;
            this.NotificationStatus = response.Headers[NotificationSenderUtility.NOTIFICATION_STATUS_HEADER];
            this.DeviceConnectionStatus = response.Headers[NotificationSenderUtility.DEVICE_CONNECTION_STATUS_HEADER];
            this.SubscriptionStatus = response.Headers[NotificationSenderUtility.SUBSCRIPTION_STATUS_HEADER];
        }

        public DateTimeOffset Timestamp { get; private set; }
        public string MessageId { get; private set; }
        public string ChannelUri { get; private set; }
        public NotificationType NotificationType { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public string NotificationStatus { get; private set; }
        public string DeviceConnectionStatus { get; private set; }
        public string SubscriptionStatus { get; private set; }
    }
    #endregion

    public class NotificationSenderUtility
    {
        #region Local constants
        public const string MESSAGE_ID_HEADER = "X-MessageID";
        public const string NOTIFICATION_CLASS_HEADER = "X-NotificationClass";
        public const string NOTIFICATION_STATUS_HEADER = "X-NotificationStatus";
        public const string DEVICE_CONNECTION_STATUS_HEADER = "X-DeviceConnectionStatus";
        public const string SUBSCRIPTION_STATUS_HEADER = "X-SubscriptionStatus";
        public const string WINDOWSPHONE_TARGET_HEADER = "X-WindowsPhone-Target";
        public const int MAX_PAYLOAD_LENGTH = 1024;
        #endregion

        #region Notification Callback Delegate Definition
        public delegate void SendNotificationToMPNSCompleted(CallbackArgs response);
        #endregion

        #region Callback call
        protected void OnNotified(NotificationType notificationType, HttpWebResponse response, SendNotificationToMPNSCompleted callback)
        {
            CallbackArgs args = new CallbackArgs(notificationType, response);
            if (null != callback)
                callback(args);
        }
        #endregion

        #region SendXXXNotification functionality
        public void SendRawNotification(List<Uri> Uris, byte[] Payload, SendNotificationToMPNSCompleted callback)
        {
            foreach (var uri in Uris)
                SendNotificationByType(uri, Payload, NotificationType.Raw, callback);
        }

        public void SendToastNotification(List<Uri> Uris, string message1, string message2, SendNotificationToMPNSCompleted callback)
        {
            byte[] payload = prepareToastPayload(message1, message2);

            foreach (var uri in Uris)
                SendNotificationByType(uri, payload, NotificationType.Toast, callback);
        }

        public void SendTileNotification(List<Uri> Uris, string TokenID, string BackgroundImageUri, int Count, string Title, SendNotificationToMPNSCompleted callback)
        {
            byte[] payload = prepareTilePayload(TokenID, BackgroundImageUri, Count, Title);

            foreach (var uri in Uris)
                SendNotificationByType(uri, payload, NotificationType.Token, callback);
        }
        #endregion

        #region SendNotificatioByType Logic
        private void SendNotificationByType(Uri channelUri, byte[] payload, NotificationType notificationType, SendNotificationToMPNSCompleted callback)
        {
            try
            {
                SendMessage(channelUri, payload, notificationType, callback);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion

        #region Send Message to Microsoft Push Service
        private void SendMessage(Uri channelUri, byte[] payload, NotificationType notificationType, SendNotificationToMPNSCompleted callback)
        {
       
            if (payload.Length > MAX_PAYLOAD_LENGTH)
                throw new ArgumentOutOfRangeException("Payload is too long. Maximum payload size shouldn't exceed " + MAX_PAYLOAD_LENGTH.ToString() + " bytes");

            try
            {
          
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(channelUri);
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "text/xml; charset=utf-8";
                request.ContentLength = payload.Length;
                request.Headers[MESSAGE_ID_HEADER] = Guid.NewGuid().ToString();
                request.Headers[NOTIFICATION_CLASS_HEADER] = ((int)notificationType).ToString();

                if (notificationType == NotificationType.Toast)
                    request.Headers[WINDOWSPHONE_TARGET_HEADER] = "toast";
                else if (notificationType == NotificationType.Token)
                    request.Headers[WINDOWSPHONE_TARGET_HEADER] = "token";

                request.BeginGetRequestStream((ar) =>
                {
               
                    Stream requestStream = request.EndGetRequestStream(ar);

                    requestStream.BeginWrite(payload, 0, payload.Length, (iar) =>
                    {
                        requestStream.EndWrite(iar);
                        requestStream.Close();

                        request.BeginGetResponse((iarr) =>
                        {
                            using (WebResponse response = request.EndGetResponse(iarr))
                            {
                                OnNotified(notificationType, (HttpWebResponse)response, callback);
                            }
                        },
                        null);
                    },
                    null);
                },
                null);
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    OnNotified(notificationType, (HttpWebResponse)ex.Response, callback);
                }

                throw;
            }
        }
        #endregion

        #region Prepare Payloads
        private static byte[] prepareToastPayload(string text1, string text2)
        {
            MemoryStream stream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 };
            XmlWriter writer = XmlWriter.Create(stream, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("wp", "Notification", "WPNotification");
            writer.WriteStartElement("wp", "Toast", "WPNotification");
            writer.WriteStartElement("wp", "Text1", "WPNotification");
            writer.WriteValue(text1);
            writer.WriteEndElement();
            writer.WriteStartElement("wp", "Text2", "WPNotification");
            writer.WriteValue(text2);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            byte[] payload = stream.ToArray();
            return payload;
        }

        private static byte[] prepareTilePayload(string tokenId, string backgroundImageUri, int count, string title)
        {
            MemoryStream stream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 };
            XmlWriter writer = XmlWriter.Create(stream, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("wp", "Notification", "WPNotification");
            writer.WriteStartElement("wp", "Tile", "WPNotification");
            writer.WriteStartElement("wp", "BackgroundImage", "WPNotification");
            writer.WriteValue(backgroundImageUri);
            writer.WriteEndElement();
            writer.WriteStartElement("wp", "Count", "WPNotification");
            writer.WriteValue(count.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("wp", "Title", "WPNotification");
            writer.WriteValue(title);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Close();

            byte[] payload = stream.ToArray();
            return payload;
        }

        #endregion

    }
}