// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.


using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.ServiceModel;
using Notification_ServerService.Service;
using System.ServiceModel.Description;

namespace Notification_ServerService
{
    public partial class App : Application
    {
        ServiceHost host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
       
                host = new ServiceHost(typeof(RegistrationService));
                host.Open();
            }
            catch (TimeoutException timeoutException)
            {
                MessageBox.Show(String.Format("The service operation timed out. {0}", timeoutException.Message));
            }
            catch (CommunicationException communicationException)
            {
                MessageBox.Show(String.Format("Could not start service host. {0}", communicationException.Message));
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (host != null)
            {
                try
                {
                    host.Close();
                }
                catch (TimeoutException)
                {
                    host.Abort();
                }
                catch (CommunicationException)
                {
                    host.Abort();
                }
            }
            base.OnExit(e);
        }
    }
}