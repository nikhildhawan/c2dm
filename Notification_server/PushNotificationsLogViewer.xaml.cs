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
using System.Collections;

namespace Notification_ServerService
{
   
    public partial class PushNotificationsLogViewer : UserControl
    {
        public PushNotificationsLogViewer()
        {
            InitializeComponent();
            tracelog.LostFocus += (sender, e) => { tracelog.SelectedIndex = -1; };
        }

        public IEnumerable ItemsSource
        {
            get
            {
                return this.tracelog.ItemsSource;
            }

            set
            {
                this.tracelog.ItemsSource = value;
            }
        }
    }
}