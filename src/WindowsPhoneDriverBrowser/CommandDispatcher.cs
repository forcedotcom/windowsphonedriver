// <copyright file="CommandDispatcher.cs" company="Salesforce.com">
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
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using WindowsPhoneDriverBrowser.CommandHandlers;

namespace WindowsPhoneDriverBrowser
{
    /// <summary>
    /// Dispatches received commands to the correct handlers.
    /// </summary>
    internal class CommandDispatcher
    {
        private StreamSocketListener listener;
        private CommandEnvironment environment;
        private int displayScale;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
        /// </summary>
        /// <param name="browser">The <see cref="WebBrowser"/> to use in executing the commands.</param>
        /// <param name="displayScale">The zoom level of the display.</param>
        public CommandDispatcher(WebBrowser browser, int displayScale)
        {
            this.environment = new CommandEnvironment(browser);
            this.displayScale = displayScale;
        }

        /// <summary>
        /// Event triggered when the address information of the dispatcher is updated.
        /// </summary>
        public event EventHandler<TextEventArgs> AddressInfoUpdated;

        /// <summary>
        /// Event triggered when data is received over the wire.
        /// </summary>
        public event EventHandler<TextEventArgs> DataReceived;

        /// <summary>
        /// Dispatches a command and returns its response.
        /// </summary>
        /// <param name="serializedCommand">A JSON serialized representation of a <see cref="Command"/> object.</param>
        /// <returns>A JSON value serializing the response for the command.</returns>
        public string DispatchCommand(string serializedCommand)
        {
            Command command = Command.FromJson(serializedCommand);
            Response response = command.Execute(this.environment);
            return response.ToJson();
        }

        /// <summary>
        /// Starts listening for incoming commands.
        /// </summary>
        public async void Start()
        {
            string address = GetIPAddress();
            this.listener = new StreamSocketListener();
            this.listener.Control.QualityOfService = SocketQualityOfService.Normal;
            this.listener.ConnectionReceived += this.ConnectionReceivedEventHandler;
            await this.listener.BindServiceNameAsync(string.Empty);
            string port = this.listener.Information.LocalPort;

            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (var stream = storage.CreateFile("networkInfo.txt"))
            {
                string networkInfo = string.Format("{0}:{1}:{2}", address, port, this.displayScale);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(networkInfo);
                stream.Write(buffer, 0, buffer.Length);
            }

            var ls = storage.GetFileNames();
            this.OnAddressInfoUpdated(new TextEventArgs(address + ":" + port));
        }

        /// <summary>
        /// Executed when the address information is updated.
        /// </summary>
        /// <param name="e">The <see cref="TextEventArgs"/> containing information about the event.</param>
        protected void OnAddressInfoUpdated(TextEventArgs e)
        {
            if (this.AddressInfoUpdated != null)
            {
                this.AddressInfoUpdated(this, e);
            }
        }

        /// <summary>
        /// Executed when data is received over the wire.
        /// </summary>
        /// <param name="e">The <see cref="TextEventArgs"/> containing information about the event.</param>
        protected void OnDataReceived(TextEventArgs e)
        {
            if (this.DataReceived != null)
            {
                this.DataReceived(this, e);
            }
        }

        private static string GetIPAddress()
        {
            string address = string.Empty;
            List<string> addresses = new List<string>();
            var hostNames = NetworkInformation.GetHostNames();
            foreach (var hostName in hostNames)
            {
                if (hostName.IPInformation != null && (hostName.IPInformation.NetworkAdapter.IanaInterfaceType == 71 || hostName.IPInformation.NetworkAdapter.IanaInterfaceType == 6))
                {
                    string hostDisplayName = hostName.DisplayName;
                    addresses.Add(hostDisplayName);
                }
            }

            if (addresses.Count > 0)
            {
                address = addresses[addresses.Count - 1];
            }

            return address;
        }

        private async void ConnectionReceivedEventHandler(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Run(() => { this.HandleRequest(args.Socket); });
        }

        private async void HandleRequest(StreamSocket socket)
        {
            DataReader reader = new DataReader(socket.InputStream);
            DataWriter writer = new DataWriter(socket.OutputStream);
            writer.UnicodeEncoding = UnicodeEncoding.Utf8;

            string serializedRequest = await this.ReadData(reader);
            this.OnDataReceived(new TextEventArgs(serializedRequest));

            string commandResponse = this.DispatchCommand(serializedRequest);
            int length = System.Text.Encoding.UTF8.GetByteCount(commandResponse);
            string serializedResponse = string.Format("{0}:{1}", length, commandResponse);
            writer.WriteString(serializedResponse);
            await writer.StoreAsync();
            socket.Dispose();
        }

        private async Task<string> ReadData(DataReader reader)
        {
            string length = string.Empty;
            bool lengthFound = false;
            while (!lengthFound)
            {
                await reader.LoadAsync(1);
                byte character = reader.ReadByte();
                if (character == ':')
                {
                    lengthFound = true;
                }
                else
                {
                    length += Convert.ToChar(character);
                }
            }

            if (string.IsNullOrEmpty(length))
            {
                return string.Empty;
            }
            
            int dataLength = int.Parse(length);
            byte[] dataBuffer = new byte[dataLength];
            await reader.LoadAsync(Convert.ToUInt32(dataLength));
            reader.ReadBytes(dataBuffer);
            return System.Text.Encoding.UTF8.GetString(dataBuffer, 0, dataLength);
        }
    }
}
