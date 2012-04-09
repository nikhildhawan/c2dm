// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Notification_ServerService.Service
{
    public class RegistrationService : IRegistrationService
    {
        public static event EventHandler<SubscriptionEventArgs> Subscribed;

        private static List<Uri> subscribers = new List<Uri>();
        private static object obj = new object();

        public void Register(string uri)
        {
            Uri channelUri = new Uri(uri, UriKind.Absolute);
            Subscribe(channelUri);
        }

        public void Unregister(string uri)
        {
            Uri channelUri = new Uri(uri, UriKind.Absolute);
            Unsubscribe(channelUri);
        }

        #region Subscription/Unsubscribing logic
        private void Subscribe(Uri channelUri)
        {
            lock (obj)
            {
                if (!subscribers.Exists((u) => u == channelUri))
                {
                    subscribers.Add(channelUri);
                }
            }
            OnSubscribed(channelUri, true);
        }

        public static void Unsubscribe(Uri channelUri)
        {
            lock (obj)
            {
                subscribers.Remove(channelUri);
            }
            OnSubscribed(channelUri, false);
        }
        #endregion

        #region Helper private functionality
        private static void OnSubscribed(Uri channelUri, bool isActive)
        {
            EventHandler<SubscriptionEventArgs> handler = Subscribed;
            if (handler != null)
            {
                handler(null,
                  new SubscriptionEventArgs(channelUri, isActive));
            }
        }
        #endregion

        #region Internal SubscriptionEventArgs class definition
        public class SubscriptionEventArgs : EventArgs
        {
            public SubscriptionEventArgs(Uri channelUri, bool isActive)
            {
                this.ChannelUri = channelUri;
                this.IsActive = isActive;
            }

            public Uri ChannelUri { get; private set; }
            public bool IsActive { get; private set; }
        }
        #endregion

        #region Helper public functionality
        public static List<Uri> GetSubscribers()
        {
            return subscribers;
        }
        #endregion
    }
}