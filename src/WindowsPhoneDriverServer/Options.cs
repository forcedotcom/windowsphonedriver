// <copyright file="Options.cs" company="Salesforce.com">
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using WindowsPhoneDriver;
using WindowsPhoneDriverServer.Internal;

namespace WindowsPhoneDriverServer
{
    /// <summary>
    /// Represents the options available from the command line.
    /// </summary>
    internal class Options
    {
        private const string PortCommandLineOption = "PORT";
        private const string UserNameCommandLineOption = "USERNAME";
        private const string PasswordCommandLineOption = "PASSWORD";
        private const string ReserveUrlCommandLineOption = "RESERVE";
        private const string RemoteShutdownCommandLineOption = "REMOTESHUTDOWN";
        private const string LogLevelCommandLineOption = "LOGLEVEL";
        private const string HubCommandLineOption = "HUB";
        private const string UrlPathCommandLineOption = "URLPATH";
        private const string UseDeviceCommandLineOption = "USEDEVICE";
        private const string DeviceNameCommandLineOption = "DEVICENAME";
        private const string VersionCommandLineOption = "VERSION";
        private const string VersionShortcutCommandLineOption = "V";
        private const string HelpCommandLineOption = "HELP";
        private const string HelpShortcutCommandLineOption = "?";

        private int port = 7332;
        private string userName = string.Empty;
        private string password = string.Empty;
        private string urlToReserve = string.Empty;
        private string operatingSystemVersion = string.Empty;
        private string serverVersion = string.Empty;
        private string hubLocation = string.Empty;
        private string urlPath = string.Empty;
        private string deviceName = string.Empty;
        private ControllerKind controllerKind = ControllerKind.Emulator;
        private bool reserveUrl;
        private bool currentUserIsAdmin;
        private bool ignoreRemoteShutdown;
        private bool helpRequested;
        private bool versionRequested;
        private LogLevel logLevel = LogLevel.Debug;

        /// <summary>
        /// Initializes a new instance of the <see cref="Options"/> class.
        /// </summary>
        /// <param name="commandLineArguments">The array of arguments passed in on the command line.</param>
        internal Options(string[] commandLineArguments)
        {
            this.GetServerVersion();
            this.GetOSVersion();
            this.GetCurrentUserAdminStatus();
            foreach (string arg in commandLineArguments)
            {
                string[] argumentValues = arg.Split(new string[] { ":", "=" }, 2, StringSplitOptions.None);
                string argName = argumentValues[0];
                string argValue = "ignored";
                if (argumentValues.Length > 1)
                {
                    argValue = argumentValues[1];
                }

                this.SetOption(argName, argValue);
            }
        }

        /// <summary>
        /// Gets the relative path to append to the URL for client connections.
        /// </summary>
        internal string UrlPath
        {
            get { return this.urlPath; }
        }

        /// <summary>
        /// Gets the version of the operating system.
        /// </summary>
        internal string OSVersion
        {
            get { return this.operatingSystemVersion; }
        }

        /// <summary>
        /// Gets the version of the server.
        /// </summary>
        internal string ServerVersion
        {
            get { return this.serverVersion; }
        }

        /// <summary>
        /// Gets a value indicating whether to reserve a URL for use with the HTTP API.
        /// </summary>
        internal bool ReserveUrl
        {
            get { return this.reserveUrl; }
        }

        /// <summary>
        /// Gets a the URL to reserve for use with the HTTP API.
        /// </summary>
        internal string UrlToReserve
        {
            get { return this.urlToReserve; }
        }

        /// <summary>
        /// Gets the port on which the server should listen.
        /// </summary>
        internal int Port
        {
            get { return this.port; }
        }

        /// <summary>
        /// Gets the user name with which the server should authenticate.
        /// </summary>
        internal string UserName
        {
            get { return this.userName; }
        }

        /// <summary>
        /// Gets the password with which the server should authenticate.
        /// </summary>
        internal string Password
        {
            get { return this.password; }
        }

        /// <summary>
        /// Gets a value indicating whether the current user is an administrator.
        /// </summary>
        internal bool CurrentUserIsAdmin
        {
            get { return this.currentUserIsAdmin; }
        }

        /// <summary>
        /// Gets a value indicating whether the server should ignore remote shutdown requests.
        /// </summary>
        internal bool IgnoreRemoteShutdown
        {
            get { return this.ignoreRemoteShutdown; }
        }

        /// <summary>
        /// Gets the location of the hub to which this server should be registered.
        /// </summary>
        internal string HubLocation
        {
            get { return this.hubLocation; }
        }

        /// <summary>
        /// Gets the logging level of the server.
        /// </summary>
        internal LogLevel LoggingLevel
        {
            get { return this.logLevel; }
        }
        
        /// <summary>
        /// Gets a value indicating the kind of device controller (emulator or physical device) to create.
        /// </summary>
        internal ControllerKind DeviceControllerKind
        {
            get { return this.controllerKind; }
        }

        /// <summary>
        /// Gets the name of the device to control.
        /// </summary>
        internal string DeviceName
        {
            get { return this.deviceName; }
        }

        /// <summary>
        /// Gets a value indicating whether the user has asked for usage instructions.
        /// </summary>
        internal bool HelpRequested
        {
            get { return this.helpRequested; }
        }

        /// <summary>
        /// Gets a value indicating whether the user has asked for the version to be displayed.
        /// </summary>
        internal bool VersionRequested
        {
            get { return this.versionRequested; }
        }

        private void SetOption(string name, string value)
        {
            string argumentName = name.Substring(1).ToUpperInvariant();
            switch (argumentName)
            {
                case PortCommandLineOption:
                    this.port = int.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case UserNameCommandLineOption:
                    this.userName = value;
                    break;

                case PasswordCommandLineOption:
                    this.password = value;
                    break;

                case ReserveUrlCommandLineOption:
                    this.reserveUrl = true;
                    this.urlToReserve = value;
                    break;

                case RemoteShutdownCommandLineOption:
                    this.ignoreRemoteShutdown = value.ToUpperInvariant() == "IGNORE";
                    break;

                case HubCommandLineOption:
                    this.hubLocation = value;
                    break;

                case UrlPathCommandLineOption:
                    this.urlPath = value;
                    break;

                case LogLevelCommandLineOption:
                    LogLevel level = LogLevel.Info;
                    if (Enum.TryParse<LogLevel>(value, true, out level))
                    {
                        this.logLevel = level;
                    }

                    break;

                case UseDeviceCommandLineOption:
                    this.controllerKind = ControllerKind.Device;
                    break;

                case DeviceNameCommandLineOption:
                    this.deviceName = value;
                    break;

                case HelpCommandLineOption:
                case HelpShortcutCommandLineOption:
                    this.helpRequested = true;
                    break;

                case VersionCommandLineOption:
                case VersionShortcutCommandLineOption:
                    this.versionRequested = true;
                    break;
            }
        }

        private void GetOSVersion()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                NativeMethods.OSVersionInfoEx versionInfo = new NativeMethods.OSVersionInfoEx();
                versionInfo.dwOSVersionInfoSize = Marshal.SizeOf(versionInfo);
                NativeMethods.GetVersionEx(ref versionInfo);
               
                NativeMethods.SystemInfo sysInfo = new NativeMethods.SystemInfo();
                NativeMethods.GetSystemInfo(out sysInfo);
                NativeMethods.VersionNT versionType = (NativeMethods.VersionNT)versionInfo.wProductType;
                NativeMethods.ProcessorArchitecture architecture = (NativeMethods.ProcessorArchitecture)sysInfo.processorArchitecture;
                if (versionInfo.dwMajorVersion == 5)
                {
                    if (versionInfo.dwMinorVersion == 1)
                    {
                        this.operatingSystemVersion = "Windows XP";
                    }
                    else if (versionInfo.dwMinorVersion == 2)
                    {
                        int isServerR2 = NativeMethods.GetSystemMetrics((int)NativeMethods.SystemMetrics.ServerR2);
                        if (versionType == NativeMethods.VersionNT.Workstation && architecture == NativeMethods.ProcessorArchitecture.AMD64)
                        {
                            this.operatingSystemVersion = "Windows XP x64 Edition";
                        }
                        else if (versionType != NativeMethods.VersionNT.Workstation && isServerR2 == 0)
                        {
                            this.operatingSystemVersion = "Windows Server 2003";
                        }
                        else if (versionType == NativeMethods.VersionNT.Workstation && isServerR2 != 0)
                        {
                            this.operatingSystemVersion = "Windows Server 2003 R2";
                        }
                    }
                    else
                    {
                        this.operatingSystemVersion = "Windows 2000";
                    }
                }
                else if (versionInfo.dwMajorVersion == 6)
                {
                    if (versionInfo.dwMinorVersion == 0)
                    {
                        if (versionType == NativeMethods.VersionNT.Workstation)
                        {
                            this.operatingSystemVersion = "Windows Vista";
                        }
                        else
                        {
                            this.operatingSystemVersion = "Windows Server 2008";
                        }
                    }
                    else if (versionInfo.dwMinorVersion == 1)
                    {
                        if (versionType == NativeMethods.VersionNT.Workstation)
                        {
                            this.operatingSystemVersion = "Windows 7";
                        }
                        else
                        {
                            this.operatingSystemVersion = "Windows Server 2008 R2";
                        }
                    }
                    else
                    {
                        if (versionType == NativeMethods.VersionNT.Workstation)
                        {
                            this.operatingSystemVersion = "Windows 8";
                        }
                        else
                        {
                            this.operatingSystemVersion = "Windows Server 2012";
                        }
                    }
                }
                else
                {
                    this.operatingSystemVersion = "Unsupported Windows NT version";
                }

                if (Environment.OSVersion.ServicePack.Length > 0)
                {
                    this.operatingSystemVersion = this.operatingSystemVersion + " " + Environment.OSVersion.ServicePack;
                }

                this.operatingSystemVersion += " " + string.Format(CultureInfo.InvariantCulture, "({0}.{1}.{2})", versionInfo.dwMajorVersion, versionInfo.dwMinorVersion, versionInfo.dwBuildNumber);
                this.operatingSystemVersion += " " + architecture.ToString().ToLowerInvariant();
            }
        }

        private void GetServerVersion()
        {
            StringBuilder versionBuilder = new StringBuilder();
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            AssemblyDescriptionAttribute description = executingAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
            if (description != null)
            {
                versionBuilder.Append(description.Description);
                versionBuilder.Append(" ");
                versionBuilder.Append(executingAssembly.GetName().Version.ToString());
            }

            this.serverVersion = versionBuilder.ToString();
        }

        private void GetCurrentUserAdminStatus()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                this.currentUserIsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                this.currentUserIsAdmin = true;
            }
        }
    }
}
