// <copyright file="DeviceController.cs" company="Salesforce.com">
//
// Copyright (c) 2014 Salesforce.com, Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
//
//    Redistributions of source code must retain the above copyright notice, this list of conditions and the following
//    disclaimer.
//
//    Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the
//    following disclaimer in the documentation and/or other materials provided with the distribution.
//
//    Neither the name of Salesforce.com nor the names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using Microsoft.SmartDevice.Connectivity;

namespace WindowsPhoneDriver
{
    /// <summary>
    /// Determines the kind of controller.
    /// </summary>
    public enum ControllerKind
    {
        /// <summary>
        /// Controller controls an actual device.
        /// </summary>
        Device,

        /// <summary>
        /// Controller controls an emulator.
        /// </summary>
        Emulator
    }

    /// <summary>
    /// Provides control of a Windows Phone device or emulator.
    /// </summary>
    public class DeviceController
    {
        private ControllerKind kind = ControllerKind.Emulator;
        private string deviceName = "Emulator WVGA";
        private string address = string.Empty;
        private string port = string.Empty;
        private int displayScale = 100;
        private bool hasSession;
        private RemoteApplication browserApplication;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class.
        /// </summary>
        public DeviceController()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class connecting to the
        /// specified IP address and port.
        /// </summary>
        /// <param name="address">The IP address of the device to connect to.</param>
        /// <param name="port">The port of the device to connect to.</param>
        public DeviceController(string address, string port)
        {
            this.address = address;
            this.port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceController"/> class connecting to the
        /// specified kind of device with the specified name.
        /// </summary>
        /// <param name="kind">The <see cref="ControllerKind"/> of the controller.</param>
        /// <param name="deviceName">The name of the device to connect to.</param>
        public DeviceController(ControllerKind kind, string deviceName)
        {
            this.kind = kind;
            this.deviceName = deviceName;
        }

        /// <summary>
        /// Event raised when the status of the connection to the device changes.
        /// </summary>
        public event EventHandler<DeviceControllerConnectionStatusUpdatedEventArgs> ConnectionStatusUpdated;

        /// <summary>
        /// Gets the IP of the address of the device being controlled.
        /// </summary>
        public string Address
        {
            get { return this.address; }
        }

        /// <summary>
        /// Gets the port of the device being controlled.
        /// </summary>
        public string Port
        {
            get { return this.port; }
        }

        /// <summary>
        /// Gets the scale of the display.
        /// </summary>
        public int DisplayScaleFactor
        {
            get { return this.displayScale; }
        }

        /// <summary>
        /// Gets a value indicating whether a session has been created or not.
        /// </summary>
        public bool HasSession
        {
            get { return this.hasSession; }
        }

        /// <summary>
        /// Starts the controller.
        /// </summary>
        public void Start()
        {
            if (!string.IsNullOrEmpty(this.address) && !string.IsNullOrEmpty(this.port))
            {
                return;
            }

            Device device = this.FindDevice();

            if (device == null)
            {
                throw new WindowsPhoneDriverException(string.Format("Found no matching devices for name '{0}'", this.deviceName));
            }
            else
            {
                this.SendStatusUpdate("Connecting to device {0}.", device.Name);
                string assemblyDirectory = Path.GetDirectoryName(this.GetType().Assembly.Location);
                string xapPath = GetPackagePath(assemblyDirectory);
                XapInfo appInfo = XapInfo.ReadApplicationInfo(xapPath);
                Guid applicationId = appInfo.ApplicationId.Value;
                string iconPath = appInfo.ExtractIconFile();

                bool isConnectedToDevice = false;
                try
                {
                    device.Connect();
                    isConnectedToDevice = device.IsConnected();
                }
                catch (SmartDeviceException ex)
                {
                    this.SendStatusUpdate("WARNING! Exception encountered when connecting to device. HRESULT: {0:X}, message: {1}", ex.HResult, ex.Message);
                    System.Threading.Thread.Sleep(500);
                }

                if (!isConnectedToDevice)
                {
                    // TODO: Create connection mitigation routine.
                    this.SendStatusUpdate("WARNING! Was unable to connect to device!");
                }
                else
                {
                    if (!device.IsApplicationInstalled(applicationId))
                    {
                        this.SendStatusUpdate("Installing application {0}.", xapPath);
                        this.browserApplication = device.InstallApplication(applicationId, applicationId, "WindowsPhoneDriverBrowser", iconPath, xapPath);
                    }
                    else
                    {
                        this.SendStatusUpdate("Application already installed.");
                        this.browserApplication = device.GetApplication(applicationId);
                    }
                }

                File.Delete(iconPath);
            }
        }

        /// <summary>
        /// Starts a session with the specified device.
        /// </summary>
        public void StartSession()
        {
            if (!string.IsNullOrEmpty(this.address) && !string.IsNullOrEmpty(this.port))
            {
                return;
            }

            this.SendStatusUpdate("Launching application.");
            this.browserApplication.Launch();
            string localFile = this.RetrieveNetworkInfoFile();

            string networkInfo = File.ReadAllText(localFile);
            this.SendStatusUpdate("Contents of network info file: \"{0}\"", networkInfo);
            string[] parts = networkInfo.Split(':');
            this.address = parts[0];
            this.port = parts[1];
            this.displayScale = int.Parse(parts[2]);
            this.hasSession = true;
            File.Delete(localFile);
        }

        /// <summary>
        /// Stops a session of the WindowsPhoneDriverBrowser application on the device.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Stopping a session must not throw. Catching all exceptions is correct.")]
        public void StopSession()
        {
            try
            {
                this.browserApplication.TerminateRunningInstances();
            }
            catch (Exception)
            {
            }

            this.address = null;
            this.port = null;
            this.hasSession = false;
        }

        /// <summary>
        /// Raises the <see cref="ConnectionStatusUpdated"/> event.
        /// </summary>
        /// <param name="e">A <see cref="DeviceControllerConnectionStatusUpdatedEventArgs"/> that contains the event data.</param>
        protected void OnConnectionStatusUpdated(DeviceControllerConnectionStatusUpdatedEventArgs e)
        {
            if (this.ConnectionStatusUpdated != null)
            {
                this.ConnectionStatusUpdated(this, e);
            }
        }

        private static string GetPackagePath(string directory)
        {
            List<string> fileNames = new List<string>() { "WindowsPhoneDriverBrowser.xap" };
            foreach (string fileName in fileNames)
            {
                string fullCandidatePath = Path.Combine(directory, fileName);
                if (File.Exists(fullCandidatePath))
                {
                    return fullCandidatePath;
                }
            }

            return string.Empty;
        }

        private Device FindDevice()
        {
            DatastoreManager manager = new DatastoreManager(1033);
            Collection<Platform> platforms = manager.GetPlatforms();
            if (platforms.Count == 0)
            {
                throw new WindowsPhoneDriverException("Found no platforms");
            }

            Platform platform = platforms.FirstOrDefault((p) => { return p.Name.StartsWith("Windows Phone "); });
            this.SendStatusUpdate("Found platform {0}.", platform.Name);
            Collection<Device> devices = platform.GetDevices();
            if (devices.Count == 0)
            {
                throw new WindowsPhoneDriverException("Found no devices");
            }

            this.SendStatusUpdate("Searching for {1} device with name '{0}'.", this.deviceName, this.kind == ControllerKind.Emulator ? "emulated" : "physical");
            Device device = devices.FirstOrDefault((d) => { return d.Name == this.deviceName && d.IsEmulator() == (this.kind == ControllerKind.Emulator); });
            if (device != null)
            {
                this.SendStatusUpdate("Found device {0}.", device.Name);
            }
            else
            {
                this.SendStatusUpdate("No device found with name exactly matching '{0}'; looking for device with name contains '{0}'.", this.deviceName);
                device = devices.FirstOrDefault((d) => { return d.Name.Contains(this.deviceName) && d.IsEmulator() == (this.kind == ControllerKind.Emulator); });
            }

            if (device == null)
            {
                StringBuilder errorBuilder = new StringBuilder();
                errorBuilder.AppendFormat("No {1} device found for name matching or containing '{0}'. Found devices:", this.deviceName, this.kind == ControllerKind.Emulator ? "emulated" : "physical");
                foreach (Device currentDevice in devices)
                {
                    errorBuilder.AppendLine();
                    errorBuilder.AppendFormat("    {0} ({1} device)", currentDevice.Name, currentDevice.IsEmulator() ? "emulated" : "physical");
                }

                this.SendStatusUpdate(errorBuilder.ToString());
            }

            return device;
        }

        private string RetrieveNetworkInfoFile()
        {
            this.SendStatusUpdate("Waiting for network information file to be written to device.");
            RemoteIsolatedStorageFile storage = null;
            int retryCount = 0;
            string remoteFileName = string.Empty;
            while (string.IsNullOrEmpty(remoteFileName) && retryCount < 4)
            {
                // Need sleep here to allow application to launch.
                System.Threading.Thread.Sleep(1000);
                storage = this.browserApplication.GetIsolatedStore();
                List<RemoteFileInfo> files = storage.GetDirectoryListing(string.Empty);
                DateTime findTimeout = DateTime.Now.Add(TimeSpan.FromSeconds(15));
                while (DateTime.Now < findTimeout)
                {
                    foreach (RemoteFileInfo info in files)
                    {
                        if (info.Name.Contains("networkInfo.txt"))
                        {
                            remoteFileName = info.Name;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(remoteFileName))
                    {
                        break;
                    }

                    System.Threading.Thread.Sleep(500);
                    storage = this.browserApplication.GetIsolatedStore();
                    files = storage.GetDirectoryListing(string.Empty);
                }

                retryCount++;
            }

            if (string.IsNullOrEmpty(remoteFileName))
            {
                throw new WindowsPhoneDriverException("Application was installed and launched, but did not write network info file");
            }

            this.SendStatusUpdate("Retrieving network information file from device.");
            string localFile = Path.Combine(Path.GetTempPath(), "networkInfo.txt");
            storage.ReceiveFile("networkInfo.txt", localFile, true);
            storage.DeleteFile("networkInfo.txt");
            return localFile;
        }

        private void SendStatusUpdate(string format, params object[] args)
        {
            this.SendStatusUpdate(string.Format(format, args));
        }

        private void SendStatusUpdate(string statusUpdate)
        {
            this.OnConnectionStatusUpdated(new DeviceControllerConnectionStatusUpdatedEventArgs(statusUpdate));
        }
    }
}
