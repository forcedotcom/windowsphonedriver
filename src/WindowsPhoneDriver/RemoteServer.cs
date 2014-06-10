// <copyright file="RemoteServer.cs" company="Salesforce.com">
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace WindowsPhoneDriver
{
    /// <summary>
    /// Handles requests from remote clients to perform automated tests.
    /// </summary>
    public class RemoteServer : IDisposable
    {
        #region Constants
        private const int AccessDenied = 5;
        private const int SharingViolation = 32;
        private const string ShutdownUrlFragment = "SHUTDOWN";
        #endregion

        #region Private members
        private HttpListener listener = new HttpListener();
        private UriTemplateTable getDispatcherTable;
        private UriTemplateTable postDispatcherTable;
        private UriTemplateTable deleteDispatcherTable;
        private WindowsPhoneCommandExecutor executor;
        private Logger serverLogger;
        private string listenerPrefix;
        private string listenerPath;
        private int listenerPort;
        private bool listenerStopping;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteServer"/> class using the specified port and relative path.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="path">The relative path to connect to.</param>
        public RemoteServer(int port, string path)
            : this(port, path, new ConsoleLogger(LogLevel.Info))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteServer"/> class using the specified port, relative path, and logger.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="path">The relative path to connect to.</param>
        /// <param name="log">A <see cref="Logger"/> object describing how to log information about commands executed.</param>
        public RemoteServer(int port, string path, Logger log)
            : this(port, path, new WindowsPhoneCommandExecutor(), log)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteServer"/> class using the specified port, relative path, 
        /// device name, device type, and logger.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="path">The relative path to connect to.</param>
        /// <param name="deviceName">The name of the device to which to bind the controller.</param>
        /// <param name="kind">The <see cref="ControllerKind"/> value describing the kind of controller to create.</param>
        /// <param name="log">A <see cref="Logger"/> object describing how to log information about commands executed.</param>
        public RemoteServer(int port, string path, string deviceName, ControllerKind kind, Logger log)
            : this(port, path, new WindowsPhoneCommandExecutor(kind, deviceName), log)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteServer"/> class using the specified port and relative path.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="path">The relative path to connect to.</param>
        /// <param name="executor">A <see cref="WindowsPhoneCommandExecutor"/> used to execute commands.</param>
        /// <param name="log">A <see cref="Logger"/> used to log information in the server.</param>
        private RemoteServer(int port, string path, WindowsPhoneCommandExecutor executor, Logger log)
            : this("*", port, path, executor, log)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteServer"/> class using the specified port, relative path, and logger.
        /// </summary>
        /// <param name="basePath">The base path of the server to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="path">The relative path to connect to.</param>
        /// <param name="executor">A <see cref="WindowsPhoneCommandExecutor"/> used to execute commands.</param>
        /// <param name="log">A <see cref="Logger"/> object describing how to log information about commands executed.</param>
        private RemoteServer(string basePath, int port, string path, WindowsPhoneCommandExecutor executor, Logger log)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path", "path cannot be null");
            }

            this.executor = executor;

            this.listenerPort = port;

            if (!path.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                path = path + "/";
            }

            if (!path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                path = "/" + path;
            }

            this.listenerPath = path;
            this.listenerPrefix = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}{2}", basePath, this.listenerPort, this.listenerPath);

            this.serverLogger = log;
        }
        #endregion

        /// <summary>
        /// Event raised when a shutdown of the server is requested via the shutdown URL.
        /// </summary>
        public event EventHandler ShutdownRequested;

        #region Properties
        /// <summary>
        /// Gets the full base URL prefix on which all requests are matched.
        /// </summary>
        public string ListenerPrefix
        {
            get { return this.listenerPrefix; }
        }

        /// <summary>
        /// Gets the logger used for this remote server.
        /// </summary>
        protected Logger ServerLogger
        {
            get { return this.serverLogger; }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Starts the remote server listening for requests.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Application is not localized. String literals are expressly permitted.")]
        public void StartListening()
        {
            try
            {
                this.executor.Start(this.serverLogger);
                this.listenerStopping = false;
                this.ConstructDispatcherTables(this.listenerPrefix);
                this.listener.Prefixes.Add(this.listenerPrefix);
                this.listener.Start();
                this.listener.BeginGetContext(this.OnClientConnect, this.listener);
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == AccessDenied)
                {
                    this.serverLogger.Log("Access was denied to listen on added prefixes. Attempt elevation.", LogLevel.Error);
                }
                else if (ex.ErrorCode == SharingViolation)
                {
                    this.serverLogger.Log(string.Format(CultureInfo.InvariantCulture, "Another application is already listening on port {0}", this.listenerPort), LogLevel.Error);
                }
                else
                {
                    this.serverLogger.Log(string.Format(CultureInfo.InvariantCulture, "An unexpected error with error code {0} occurred.", ex.ErrorCode), LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Stops the remote server listening for requests.
        /// </summary>
        public void StopListening()
        {
            this.listenerStopping = true;
            if (this.listener.IsListening)
            {
                this.listener.Stop();
            }
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Releases all resources associated with this <see cref="RemoteServer"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all managed and unmanaged resources associated with this <see cref="RemoteServer"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose of managed and 
        /// unmanaged resources; <see langword="false"/> to dispose of only unmanaged 
        /// resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StopListening();
                this.listener.Close();
            }
        }
        #endregion

        /// <summary>
        /// Raises the ShutdownRequested event.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs"/> object describing the event.</param>
        protected void OnShutdownRequested(EventArgs e)
        {
            if (this.ShutdownRequested != null)
            {
                this.ShutdownRequested(this, e);
            }
        }

        #region Private support methods
        private void ConstructDispatcherTables(string prefix)
        {
            this.getDispatcherTable = new UriTemplateTable(new Uri(prefix.Replace("*", "localhost")));
            this.postDispatcherTable = new UriTemplateTable(new Uri(prefix.Replace("*", "localhost")));
            this.deleteDispatcherTable = new UriTemplateTable(new Uri(prefix.Replace("*", "localhost")));

            // DriverCommand is a static class with static fields containing the command names.
            // Since this is initialization code only, we'll take the perf hit in using reflection.
            FieldInfo[] fields = typeof(DriverCommand).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                string commandName = field.GetValue(null).ToString();
                CommandInfo commandInformation = CommandInfoRepository.Instance.GetCommandInfo(commandName);
                UriTemplate commandUriTemplate = new UriTemplate(commandInformation.ResourcePath);
                UriTemplateTable templateTable = this.FindDispatcherTable(commandInformation.Method);
                templateTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(commandUriTemplate, commandName));
            }

            this.getDispatcherTable.MakeReadOnly(false);
            this.postDispatcherTable.MakeReadOnly(false);
            this.deleteDispatcherTable.MakeReadOnly(false);
        }

        private UriTemplateTable FindDispatcherTable(string httpMethod)
        {
            UriTemplateTable tableToReturn = null;
            switch (httpMethod)
            {
                case CommandInfo.GetCommand:
                    tableToReturn = this.getDispatcherTable;
                    break;

                case CommandInfo.PostCommand:
                    tableToReturn = this.postDispatcherTable;
                    break;

                case CommandInfo.DeleteCommand:
                    tableToReturn = this.deleteDispatcherTable;
                    break;
            }

            return tableToReturn;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Application is not localized. String literals are expressly permitted.")]
        private void OnClientConnect(IAsyncResult result)
        {
            try
            {
                HttpListener localListener = (HttpListener)result.AsyncState;

                // Call EndGetContext to complete the asynchronous operation.
                HttpListenerContext context = localListener.EndGetContext(result);
                this.listener.BeginGetContext(new AsyncCallback(this.OnClientConnect), localListener);

                this.ProcessContext(context);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                this.serverLogger.Log("SocketException:" + se.Message, LogLevel.Error);
            }
            catch (HttpListenerException hle)
            {
                // When we shut the HttpListener down, there will always still be
                // a thread pending listening for a request. If there is no client
                // connected, we may have a real problem here.
                if (!this.listenerStopping)
                {
                    Console.WriteLine("HttpListenerException:" + hle.Message);
                }
            }
        }

        private void ProcessContext(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            string httpMethod = request.HttpMethod;
            string requestBody = string.Empty;
            if (string.Compare(httpMethod, "POST", StringComparison.OrdinalIgnoreCase) == 0)
            {
                int totalBytesRead = 0;
                byte[] bodyData = new byte[request.ContentLength64];
                while (totalBytesRead < request.ContentLength64)
                {
                    totalBytesRead += request.InputStream.Read(bodyData, totalBytesRead, (int)request.ContentLength64 - totalBytesRead);
                }

                requestBody = Encoding.UTF8.GetString(bodyData);
            }

            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            ServerResponse result = this.DispatchRequest(request.Url, httpMethod, requestBody);
            if (result.StatusCode == HttpStatusCode.SeeOther)
            {
                response.AddHeader("Location", request.Url.AbsoluteUri + "/" + result.ReturnedResponse.Value.ToString());
                result.ReturnedResponse.Value = string.Empty;
            }
            
            string responseString = result.ReturnedResponse.ToJson();
            
            // Construct a response.
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = result.ContentType;
            response.StatusCode = (int)result.StatusCode;
            response.StatusDescription = result.StatusCode.ToString();

            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            
            // We must close the output stream.
            output.Close();

            if (request.Url.AbsolutePath.ToUpperInvariant().Contains(ShutdownUrlFragment))
            {
                this.OnShutdownRequested(new EventArgs());
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Application is not localized. String literals are expressly permitted.")]
        private ServerResponse DispatchRequest(Uri resourcePath, string httpMethod, string requestBody)
        {
            HttpStatusCode codeToReturn = HttpStatusCode.OK;
            Response commandResponse = new Response();
            UriTemplateTable templateTable = this.FindDispatcherTable(httpMethod);
            UriTemplateMatch match = templateTable.MatchSingle(resourcePath);
            if (resourcePath.AbsolutePath.ToUpperInvariant().Contains(ShutdownUrlFragment))
            {
                this.serverLogger.Log("Executing: [Shutdown] at URL: " + resourcePath.AbsolutePath);
            }
            else if (match == null)
            {
                codeToReturn = HttpStatusCode.NotFound;
                commandResponse.Value = "No command associated with " + resourcePath.AbsolutePath;
            }
            else
            {
                string relativeUrl = match.RequestUri.AbsoluteUri.Substring(match.RequestUri.AbsoluteUri.IndexOf(this.listenerPath, StringComparison.OrdinalIgnoreCase) + this.listenerPath.Length - 1);

                string commandName = (string)match.Data;

                Command commandToExecute = new Command(commandName, requestBody);
                foreach (string key in match.BoundVariables.Keys)
                {
                    object value = match.BoundVariables[key];
                    if (relativeUrl.Contains("/element/") && key == "ID")
                    {
                        // So that we are consistent in sending element references over the
                        // wire to the phone, if we find an element reference in the URL,
                        // convert it to an element reference dictionary before sending it.
                        Dictionary<string, object> element = new Dictionary<string, object>();
                        element["ELEMENT"] = match.BoundVariables[key];
                        value = element;
                    }

                    commandToExecute.Parameters.Add(key, value);
                }

                commandResponse = this.executor.Execute(commandToExecute);
                if (commandResponse.Status != WebDriverResult.Success)
                {
                    codeToReturn = HttpStatusCode.InternalServerError;
                }

                if (commandToExecute.Name != DriverCommand.Status)
                {
                    this.serverLogger.Log("Done: " + relativeUrl);
                }
            }

            return new ServerResponse(commandResponse, codeToReturn);
        }
        #endregion
    }
}
