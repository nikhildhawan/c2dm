// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using System.Xml.Linq;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;

namespace PushNotifications
{
    public partial class MainPage : PhoneApplicationPage
    {
        private HttpNotificationChannel httpChannel;
        const string channelName = "NotificationChannel";
        const string fileName = "PushNotificationsSettings.dat";
        const int pushConnectTimeout = 30;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            if (!TryFindChannel())
                DoConnect();
        }

        #region Tracing and Status Updates
        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
        }

        private void Trace(string message)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
        }
        #endregion

        #region Misc logic
        private void DoConnect()
        {
            try
            {
               
                httpChannel = HttpNotificationChannel.Find(channelName);

                if (null != httpChannel)
                {
                 
                    SubscribeToChannelEvents();

                  
                    SubscribeToService();

                  
                    SubscribeToNotifications();

                    Dispatcher.BeginInvoke(() => UpdateStatus("Channel recovered"));
                }
                else
                {
                   
                  
                    httpChannel = new HttpNotificationChannel(channelName, "HOLWeatherService");
                   

                    SubscribeToChannelEvents();

                  
                    httpChannel.Open();
                    Dispatcher.BeginInvoke(() => UpdateStatus("Channel open requested"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(() => UpdateStatus("Channel error: " + ex.Message));
            }
        }

        private void ParseRAWPayload(Stream e, out string weather, out string location, out string temperature)
        {
            XDocument document;

            using (var reader = new StreamReader(e))
            {
                string payload = reader.ReadToEnd().Replace('\0',
                  ' ');
                document = XDocument.Parse(payload);
            }

            location = (from c in document.Descendants("WeatherUpdate")
                        select c.Element("Location").Value).FirstOrDefault();
            Trace("Got location: " + location);

            temperature = (from c in document.Descendants("WeatherUpdate")
                           select c.Element("Temperature").Value).FirstOrDefault();
            Trace("Got temperature: " + temperature);

            weather = (from c in document.Descendants("WeatherUpdate")
                       select c.Element("WeatherType").Value).FirstOrDefault();
        }
        #endregion

        #region Subscriptions
        private void SubscribeToChannelEvents()
        {
          
            httpChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(httpChannel_ChannelUriUpdated);

          
            httpChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(httpChannel_HttpNotificationReceived);

          
            httpChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(httpChannel_ExceptionOccurred);

          
            httpChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(httpChannel_ShellToastNotificationReceived);
        }

        private void SubscribeToService()
        {
          
            string baseUri = "http://192.16.11.100:8000/RegirstatorService/Register?uri={0}";
            string theUri = String.Format(baseUri, httpChannel.ChannelUri.ToString());
            WebClient client = new WebClient();
            client.DownloadStringCompleted += (s, e) =>
            {
                if (null == e.Error)
                    Dispatcher.BeginInvoke(() => UpdateStatus("Registration succeeded"));
                else
                    Dispatcher.BeginInvoke(() => UpdateStatus("Registration failed: " + e.Error.Message));
            };
            client.DownloadStringAsync(new Uri(theUri));
        }
        private void SubscribeToNotifications()
        {
            try
            {
                if (httpChannel.IsShellToastBound == false)
                {
                    httpChannel.BindToShellToast();
                }
               
            }
            catch (Exception ex)
            {
                
            }

             try
            {
                if (httpChannel.IsShellTileBound == false)
                {
                    Collection<Uri> uris = new Collection<Uri>();
                    uris.Add(new Uri("http://jquery.andreaseberhard.de/pngFix/pngtest.png"));

                    httpChannel.BindToShellTile(uris);
                }
              
            }
            catch (Exception ex)
            {
            
            }
        }

        #endregion

        #region Channel event handlers
        void httpChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
         
            Dispatcher.BeginInvoke(() => SaveChannelInfo());

           
            SubscribeToService();
            SubscribeToNotifications();

            Dispatcher.BeginInvoke(() => UpdateStatus("Channel created successfully"));
        }

        void httpChannel_ExceptionOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(() => UpdateStatus(e.ErrorType + " occurred: " + e.Message));
        }

        void httpChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {

          
            string weather, location, temperature;
            ParseRAWPayload(e.Notification.Body, out weather, out location, out temperature);

            Dispatcher.BeginInvoke(() => this.textBlockListTitle.Text = location);
            Dispatcher.BeginInvoke(() => this.txtTemperature.Text = temperature);
            Dispatcher.BeginInvoke(() => this.imgWeatherConditions.Source = new BitmapImage(new Uri(@"Images/" + weather + ".png", UriKind.Relative)));
          
        }

        void httpChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
          
            foreach (var key in e.Collection.Keys)
            {
                string msg = e.Collection[key];

             
                Dispatcher.BeginInvoke(() => UpdateStatus("Toast/Tile message: " + msg));
            }

          
        }
        #endregion

        #region Loading/Saving Channel Info
        private bool TryFindChannel()
        {
            bool bRes = false;

          
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
          
                if (isf.FileExists(fileName))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, FileMode.Open, isf))
                    {
                        using (StreamReader sr = new StreamReader(isfs))
                        {
                            string uri = sr.ReadLine();
                            httpChannel = HttpNotificationChannel.Find(channelName);

                            if (null != httpChannel)
                            {
                                if (httpChannel.ChannelUri.ToString() == uri)
                                {
                                    Dispatcher.BeginInvoke(() => UpdateStatus("Channel retrieved"));
                                    SubscribeToChannelEvents();
                                    SubscribeToService();
                                    bRes = true;
                                }

                                sr.Close();
                            }
                        }
                    }
                }
              
            
            }

            return bRes;
        }

        private void SaveChannelInfo()
        {
          
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
           
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, FileMode.Create, isf))
                {
                    using (StreamWriter sw = new StreamWriter(isfs))
                    {
                     
                        sw.WriteLine(httpChannel.ChannelUri.ToString());
                        sw.Close();
                      
                    }
                }
            }
        }
        #endregion
    }
}