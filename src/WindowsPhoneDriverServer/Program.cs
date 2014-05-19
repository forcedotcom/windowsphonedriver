// <copyright file="Program.cs" company="Salesforce.com">
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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using WindowsPhoneDriver;

namespace WindowsPhoneDriverServer
{
    /// <summary>
    /// Class containing the main entry point of the application.
    /// </summary>
    internal class Program
    {
        private static RemoteServer httpServer;
        private static ConsoleLogger logger = null;
        private static string userName = string.Empty;
        private static string password = string.Empty;
        private static bool continueRunning = true;
        private static bool ignoreRemoteShutdown;

        /// <summary>
        /// The main entry point into the application.
        /// </summary>
        /// <param name="args">The set of arguments passed in on the command line.</param>
        public static void Main(string[] args)
        {
            Options commandLineOptions = new Options(args);
            if (commandLineOptions.HelpRequested)
            {
                PrintUsageMessage();
                return;
            }

            if (commandLineOptions.VersionRequested)
            {
                Console.WriteLine(commandLineOptions.ServerVersion);
                return;
            }

            logger = new ConsoleLogger(commandLineOptions.LoggingLevel);
            if (commandLineOptions.ReserveUrl)
            {
                bool urlReserved = ReserveUrl(commandLineOptions.UrlToReserve, true);
                if (!urlReserved)
                {
                    Environment.ExitCode = 1;
                }
            }
            else
            {
                LogVersionDetails(commandLineOptions);
                userName = commandLineOptions.UserName;
                password = commandLineOptions.Password;
                ignoreRemoteShutdown = commandLineOptions.IgnoreRemoteShutdown;
                if (string.IsNullOrEmpty(commandLineOptions.DeviceName))
                {
                    httpServer = new RemoteServer(commandLineOptions.Port, commandLineOptions.UrlPath, logger);
                }
                else
                {
                    httpServer = new RemoteServer(commandLineOptions.Port, commandLineOptions.UrlPath, commandLineOptions.DeviceName, commandLineOptions.DeviceControllerKind, logger);
                }

                httpServer.ShutdownRequested += new EventHandler(OnRemoteServerShutdownRequested);
                bool urlReservationExists = CheckForUrlReservation(commandLineOptions);
                if (urlReservationExists)
                {
                    if (!string.IsNullOrEmpty(commandLineOptions.HubLocation))
                    {
                        RegisterWithHub(commandLineOptions.HubLocation);
                    }

                    try
                    {
                        httpServer.StartListening();
                        logger.Log(string.Format(CultureInfo.InvariantCulture, "Server started. RemoteWebDriver instances connect to {0}", httpServer.ListenerPrefix), LogLevel.Info);
                        Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
                    }
                    catch (WindowsPhoneDriverException ex)
                    {
                        logger.Log(string.Format(CultureInfo.InvariantCulture, "Server could not be started. Reported error: {0}", ex.Message), LogLevel.Error);
                        continueRunning = false;
                    }

                    while (continueRunning)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        private static void PrintUsageMessage()
        {
            Console.WriteLine("Launches the WebDriver server for the Windows Phone driver");
            Console.WriteLine();
            Console.WriteLine("WindowsPhoneDriverServer.exe [/port=<port>] [/urlpath=<url path>]");
            Console.WriteLine("                             [/username=<user name>] [/password=<password>]");
            Console.WriteLine("                             [/devicename=<device name> [/usedevice]]");
            Console.WriteLine("                             [/loglevel=<level>");
            Console.WriteLine("  /port=<port>  Specifies the port on which the server will listen for");
            Console.WriteLine("                commands. Defaults to 7332 if not specified.");
            Console.WriteLine("  /urlpath=<url path>");
            Console.WriteLine("                Specifies the URL prefix on which the server will listen for");
            Console.WriteLine("                commands (e.g., 'wd/hub').");
            Console.WriteLine("  /username=<user name>");
            Console.WriteLine("                Specifies the Windows user name to use for elevation, should");
            Console.WriteLine("                elevation be required to reserve the URL for the driver.");
            Console.WriteLine("  /password=<password>");
            Console.WriteLine("                Specifies the password for the Windows user account to use for");
            Console.WriteLine("                elevation, should elevation be required to reserve the URL for");
            Console.WriteLine("                the driver.");
            Console.WriteLine("  /devicename=<device name>");
            Console.WriteLine("                Specifies the name of the Windows Phone device the driver will");
            Console.WriteLine("                connect to. This can be a real device or an emulated device.");
            Console.WriteLine("                the driver.");
            Console.WriteLine("  /usedevice    Specifies that the device named with the devicename switch");
            Console.WriteLine("                is a physical device, not an emulated one.");
        }

        private static void RegisterWithHub(string hubLocation)
        {
            // TODO: Make the server aware of Selenium Grid hubs.
            throw new NotImplementedException("Configuration as node of a Grid hub is not yet implemented: " + hubLocation);
        }

        private static void OnRemoteServerShutdownRequested(object sender, EventArgs e)
        {
            logger.Log("Remote server shutdown requested...", LogLevel.Info);
            if (ignoreRemoteShutdown)
            {
                logger.Log("Remote server shutdown request ignored", LogLevel.Info);
            }

            ShutdownServer();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            logger.Log("Shutting down server...", LogLevel.Info);
            ShutdownServer();
            e.Cancel = true;
        }

        private static void ShutdownServer()
        {
            httpServer.Dispose();
            continueRunning = false;
        }

        private static void LogVersionDetails(Options commandLineOptions)
        {
            string serverVersion = commandLineOptions.ServerVersion;
            string operatingSystemVersion = commandLineOptions.OSVersion;
            logger.Log(serverVersion);
            logger.Log(".NET runtime version: " + Environment.Version.ToString());
            logger.Log("OS version: " + operatingSystemVersion);
        }

        private static bool CheckForUrlReservation(Options commandLineOptions)
        {
            bool urlReservationExists = true;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                urlReservationExists = false;
                ReadOnlyCollection<string> urlReservations = HttpApi.GetReservedUrlList();
                foreach (string reservation in urlReservations)
                {
                    if (reservation == httpServer.ListenerPrefix)
                    {
                        urlReservationExists = true;
                        break;
                    }
                }

                if (!urlReservationExists)
                {
                    logger.Log(string.Format(CultureInfo.InvariantCulture, "URL reservation for '{0}' does not exist. Reserving URL.", httpServer.ListenerPrefix));
                    urlReservationExists = ReserveUrl(httpServer.ListenerPrefix, commandLineOptions.CurrentUserIsAdmin);
                }
            }

            return urlReservationExists;
        }

        private static bool ReserveUrl(string reservePath, bool isAdmin)
        {
            bool urlReserved = true;
            if (!isAdmin)
            {
                using (Process reserverProcess = new Process())
                {
                    ProcessStartInfo reserveInfo = new ProcessStartInfo();
                    string fileName = Assembly.GetExecutingAssembly().Location;
                    reserveInfo.WorkingDirectory = Environment.CurrentDirectory;
                    reserveInfo.FileName = fileName;
                    reserveInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "/reserve:{0}", reservePath);
                    if (Environment.OSVersion.Version.Major >= 6)
                    {
                        logger.Log("Current user is not an administrator. Requesting elevation.");
                        reserveInfo.Verb = "runas";
                        reserveInfo.ErrorDialog = true;
                        reserveInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    }
                    else
                    {
                        logger.Log("Current user is not an administrator. Attempting login.");
                        reserveInfo.UseShellExecute = false;
                        reserveInfo.CreateNoWindow = true;
                        reserveInfo.UserName = userName;
                        using (SecureString securePassword = new SecureString())
                        {
                            foreach (char passwordChar in password.ToCharArray())
                            {
                                securePassword.AppendChar(passwordChar);
                            }

                            reserveInfo.Password = securePassword;
                        }
                    }

                    reserverProcess.StartInfo = reserveInfo;
                    try
                    {
                        reserverProcess.Start();
                        reserverProcess.WaitForExit();
                        if (reserverProcess.ExitCode != 0)
                        {
                            urlReserved = false;
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        logger.Log("Error reserving URL: " + ex.Message, LogLevel.Error);
                        urlReserved = false;
                    }
                }
            }
            else
            {
                try
                {
                    HttpApi.AddReservation(reservePath, "BUILTIN\\Users");
                }
                catch (Win32Exception)
                {
                    urlReserved = false;
                }
            }

            return urlReserved;
        }
    }
}
