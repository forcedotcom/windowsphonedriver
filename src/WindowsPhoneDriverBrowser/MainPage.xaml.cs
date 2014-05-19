// <copyright file="MainPage.xaml.cs" company="Salesforce.com">
//
// Copyright 2014 Salesforce.com
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using WindowsPhoneDriverBrowser.Resources;

namespace WindowsPhoneDriverBrowser
{
    /// <summary>
    /// Contains the code behind the main page of the application.
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        private CommandDispatcher dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            int displayScale = App.Current.Host.Content.ScaleFactor;
            this.dispatcher = new CommandDispatcher(this.browser, displayScale);
            this.dispatcher.AddressInfoUpdated += this.DispatcherAddressInfoUpdated;
            this.dispatcher.DataReceived += this.DispatcherDataReceived;
            this.dispatcher.Start();
        }

        private void DispatcherDataReceived(object sender, TextEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.receivedData.Text = e.Text;
                string memoryUsage = string.Format("Mem: {0}/{1}", FormatMemoryValue(DeviceStatus.ApplicationPeakMemoryUsage), FormatMemoryValue(DeviceStatus.ApplicationMemoryUsageLimit));
                this.addressInfo.Text = memoryUsage;
            });
        }

        private void DispatcherAddressInfoUpdated(object sender, TextEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.addressInfo.Text = e.Text;
            });
        }

        private string FormatMemoryValue(long valueInBytes)
        {
            string valueString = string.Empty;
            if (valueInBytes >= 1024 * 1024)
            {
                double valueInMb = valueInBytes / (1024.0 * 1024.0);
                valueString = string.Format("{0:F2} MB", valueInMb);
            }
            else
            {
                double valueInKb = valueInBytes / 1024.0;
                valueString = string.Format("{0:F2} KB", valueInKb);
            }

            return valueString;
        }
    }
}