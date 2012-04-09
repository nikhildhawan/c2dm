// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using System.Xml;
using WindowsPhone.PushNotificationManager;
using Notification_ServerService.Service;

namespace Notification_ServerService
{
   
    public partial class MainWindow : Window
    {
        #region Private variables
        private ObservableCollection<CallbackArgs> trace = new ObservableCollection<CallbackArgs>();
        private NotificationSenderUtility notifier = new NotificationSenderUtility();
        private string[] lastSent = null;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializeWeather();
            InitializeLocations();

            Log.ItemsSource = trace;
            RegistrationService.Subscribed += new EventHandler<RegistrationService.SubscriptionEventArgs>(RegistrationService_Subscribed);
        }

        #region Initializations
        private void InitializeLocations()
        {
          List<string> locations = new List<string>();
          locations.Add("Algorithm");
          locations.Add("Data Modeling");
          locations.Add("OS");
          locations.Add("Image Processing");
          locations.Add("OOAD");
          locations.Add("Networking");

          cmbLocation.ItemsSource = locations;
          cmbLocation.SelectedIndex = 0;
        }

        private void InitializeWeather()
        {
          Dictionary<string, string> weather = new Dictionary<string, string>();
          weather.Add("Prof. Prasanna", "Prof. Prasanna");
          weather.Add("Prof. Dinesha", "Prof. Dinesha");
          weather.Add("Prof. Shrisha Rao", "Prof. Shrisha Rao");
          weather.Add("Prof. R.Chandrashekhar", "Prof. R.Chandrashekhar");
          weather.Add("Prof. R.Jyotsna Bapat", "Prof. R.Jyotsna Bapat");
          weather.Add("Prof. Jaya Nair", "Prof.Jaya Nair");
          weather.Add("Prof. Das", "Prof. Das");
          weather.Add("Prof. Niladri", "Prof. Niladri");

          cmbWeather.ItemsSource = weather;
          cmbWeather.DisplayMemberPath = "Value";
          cmbWeather.SelectedValuePath = "Key";
          cmbWeather.SelectedIndex = 0;
        }
        #endregion

        #region Event Handlers
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
          if ((bool)rbnTile.IsChecked) sendTile();
          else if ((bool)rbnHttp.IsChecked) sendHttp();
          else if ((bool)rbnToast.IsChecked) sendToast();
        }

        private void sendToast()
        {
            string msg = txtToastMessage.Text;
            txtToastMessage.Text = "";
            List<Uri> subscribers = RegistrationService.GetSubscribers();
            ThreadPool.QueueUserWorkItem((unused) => notifier.SendToastNotification(subscribers,
                "ALERT", msg, OnMessageSent));
        }

        private void sendTile()
        {
            string weatherType = cmbWeather.SelectedValue as string;
            int temperature = (int)sld.Value;
            string location = cmbLocation.SelectedValue as string;
            List<Uri> subscribers = RegistrationService.GetSubscribers();
            ThreadPool.QueueUserWorkItem((unused) => notifier.SendTileNotification(subscribers, "PushNotificationsToken", "/Images/" + weatherType + ".png", temperature, location, OnMessageSent));
        }

        private void sendHttp()
        {
          
            List<Uri> subscribers = RegistrationService.GetSubscribers();
          
            byte[] payload = prepareRAWPayload(
                cmbLocation.SelectedValue as string,
                 sld.Value.ToString("F1"),
                cmbWeather.SelectedValue as string);
          
            ThreadPool.QueueUserWorkItem((unused) => notifier.SendRawNotification(subscribers,
                                    payload,
                                    OnMessageSent)
                );

            
            lastSent = new string[3];
            lastSent[0] = cmbLocation.SelectedValue as string;
            lastSent[1] = sld.Value.ToString("F1");
            lastSent[2] = cmbWeather.SelectedValue as string;
        }

        void RegistrationService_Subscribed(object sender, RegistrationService.SubscriptionEventArgs e)
        {
          
            if (null != lastSent)
            {
                string location = lastSent[0];
                string temperature = lastSent[1];
                string weatherType = lastSent[2];
                List<Uri> subscribers = new List<Uri>();
                subscribers.Add(e.ChannelUri);
                byte[] payload = prepareRAWPayload(location, temperature, weatherType);

                ThreadPool.QueueUserWorkItem((unused) => notifier.SendRawNotification(subscribers, payload, OnMessageSent));
            }

            Dispatcher.BeginInvoke((Action)(() =>
            { UpdateStatus(); })
            );
        }

        private void OnMessageSent(CallbackArgs response)
        {
            Dispatcher.BeginInvoke((Action)(() => { trace.Add(response); }));
        }

        #endregion

        #region Private functionality
        private void UpdateStatus()
        {
            int activeSubscribers = RegistrationService.GetSubscribers().Count;
            bool isReady = (activeSubscribers > 0);
            txtActiveConnections.Text = activeSubscribers.ToString();
            txtStatus.Text = isReady ? "Ready" : "Waiting for connection...";
        }

        private static byte[] prepareRAWPayload(string location, string temperature, string weatherType)
        {
            MemoryStream stream = new MemoryStream();

            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 };
            XmlWriter writer = XmlTextWriter.Create(stream, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("WeatherUpdate");

            writer.WriteStartElement("Location");
            writer.WriteValue(location);
            writer.WriteEndElement();

            writer.WriteStartElement("Temperature");
            writer.WriteValue(temperature);
            writer.WriteEndElement();

            writer.WriteStartElement("WeatherType");
            writer.WriteValue(weatherType);
            writer.WriteEndElement();

            writer.WriteStartElement("LastUpdated");
            writer.WriteValue(DateTime.Now.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            byte[] payload = stream.ToArray();
            return payload;
        }
        #endregion
    }
}