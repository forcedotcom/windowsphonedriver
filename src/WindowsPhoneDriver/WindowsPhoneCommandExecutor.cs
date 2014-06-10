// <copyright file="WindowsPhoneCommandExecutor.cs" company="Salesforce.com">
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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using OpenQA.Selenium.Remote;

namespace WindowsPhoneDriver
{
    /// <summary>
    /// Handles execution of commands received by the server via HTTP.
    /// </summary>
    public class WindowsPhoneCommandExecutor : ICommandExecutor
    {
        private DeviceController controller;
        private Logger log;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPhoneCommandExecutor"/> class.
        /// </summary>
        public WindowsPhoneCommandExecutor()
        {
            this.controller = new DeviceController();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPhoneCommandExecutor"/> class
        /// for the given kind of controller and device name.
        /// </summary>
        /// <param name="controllerKind">The <see cref="ControllerKind"/> value representing
        /// the kind of controller to create.</param>
        /// <param name="deviceName">The device name for which to search when connecting.</param>
        public WindowsPhoneCommandExecutor(ControllerKind controllerKind, string deviceName)
        {
            this.controller = new DeviceController(controllerKind, deviceName);
        }

        /// <summary>
        /// Starts the command executor.
        /// </summary>
        /// <param name="logProvider"><see cref="Logger"/> object providing logging capabilities to the executor.</param>
        public void Start(Logger logProvider)
        {
            this.log = logProvider;
            this.controller.ConnectionStatusUpdated += this.ControllerConnectionStatusUpdated;
            this.controller.Start();
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="commandToExecute">The command to execute.</param>
        /// <returns>The response of the command.</returns>
        public Response Execute(Command commandToExecute)
        {
            if (commandToExecute == null)
            {
                throw new ArgumentNullException("commandToExecute");
            }

            if (commandToExecute.Name == DriverCommand.Status)
            {
                Dictionary<string, object> build = new Dictionary<string, object>();
                build["version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                Dictionary<string, object> os = new Dictionary<string, object>();
                string platformName = "windows";
                if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    platformName = "linux";
                }

                os["name"] = platformName;
                os["version"] = Environment.OSVersion.Version.ToString();

                Dictionary<string, object> status = new Dictionary<string, object>();
                status["build"] = build;
                status["os"] = os;
                Response statusResponse = new Response();
                statusResponse.Value = status;
                return statusResponse;
            }

            if (commandToExecute.Name == DriverCommand.NewSession)
            {
                this.controller.StartSession();
            }

            if (!this.controller.HasSession)
            {
                Response noSessionResponse = new Response();
                if (commandToExecute.Name != DriverCommand.Quit && commandToExecute.Name != DriverCommand.Close)
                {
                    noSessionResponse.Status = OpenQA.Selenium.WebDriverResult.NoSuchDriver;
                    noSessionResponse.Value = "Driver does not have an active session.";
                }

                return noSessionResponse;
            }

            string serializedCommand = "{\"name\":\"" + commandToExecute.Name + "\",\"parameters\":" + commandToExecute.ParametersAsJsonString + "}";
            string serializedResponse = this.SendMessage(serializedCommand);

            if (commandToExecute.Name == DriverCommand.Quit || commandToExecute.Name == DriverCommand.Close)
            {
                this.controller.StopSession();
            }

            Response result = Response.FromJson(serializedResponse);
            return result;
        }

        private string SendMessage(string serializedCommand)
        {
            return this.SendMessage(this.controller.Address, this.controller.Port, serializedCommand);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Application is not localized. String literals are expressly permitted.")]
        private string SendMessage(string address, string port, string message)
        {
            string receivedMessage = string.Empty;
            Console.WriteLine("Attempting to connect to {0}:{1}", address, port);
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(address, int.Parse(port, CultureInfo.InvariantCulture));
                using (NetworkStream sendStream = new NetworkStream(socket, false))
                {
                    int length = Encoding.UTF8.GetByteCount(message);
                    string datagram = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", length, message);
                    this.log.Log(string.Format(CultureInfo.InvariantCulture, ">>> {0}", datagram));
                    sendStream.Write(Encoding.UTF8.GetBytes(datagram), 0, Encoding.UTF8.GetByteCount(datagram));
                }

                using (NetworkStream receiveStream = new NetworkStream(socket, false))
                {
                    DateTime initialReadTimeout = DateTime.Now.AddSeconds(15);
                    int byteValue = receiveStream.ReadByte();
                    while (byteValue == 0 && DateTime.Now < initialReadTimeout)
                    {
                        System.Threading.Thread.Sleep(50);
                        this.log.Log("Waiting for data to be available");
                        byteValue = receiveStream.ReadByte();
                    }

                    StringBuilder dataLengthBuilder = new StringBuilder();
                    char currentChar = Convert.ToChar(byteValue);
                    while (currentChar != ':')
                    {
                        dataLengthBuilder.Append(currentChar);
                        byteValue = receiveStream.ReadByte();
                        currentChar = Convert.ToChar(byteValue);
                    }

                    int dataLength = int.Parse(dataLengthBuilder.ToString(), CultureInfo.InvariantCulture);
                    this.log.Log(string.Format(CultureInfo.InvariantCulture, "Waiting to receive {0} bytes", dataLength));
                    byte[] buffer = new byte[dataLength];
                    int received = 0;
                    while (received < dataLength)
                    {
                        received += receiveStream.Read(buffer, received, dataLength - received);
                    }

                    receivedMessage = Encoding.UTF8.GetString(buffer, 0, received);
                    this.log.Log(string.Format(CultureInfo.InvariantCulture, "<<< {0}", receivedMessage));
                }
            }

            return receivedMessage;
        }

        private void ControllerConnectionStatusUpdated(object sender, DeviceControllerConnectionStatusUpdatedEventArgs e)
        {
            this.log.Log(e.StatusUpdateText);
        }
    }
}
