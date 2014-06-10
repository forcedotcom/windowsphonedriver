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
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsPhoneDriver;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string localIPAddress = GetLocalIPAddress();
            Console.WriteLine("Local IP Address: {0}", localIPAddress);

            RunRepl();

            Console.WriteLine("Completed. Press <Enter> to exit.");
            Console.ReadLine();
        }

        private static void RunRepl()
        {
            bool isStart = false;
            bool isQuit = false;

            Stopwatch watcher = new Stopwatch();
            string connectionInfo = string.Empty;
            DeviceController controller = null;
            if (string.IsNullOrEmpty(connectionInfo))
            {
                controller = new DeviceController();
            }
            else
            {
                string[] parts = connectionInfo.Split(':');
                controller = new DeviceController(parts[0], parts[1]);
            }

            string message = string.Empty;
            while (message != "exit")
            {
                Console.WriteLine("Type your message:");
                message = Console.ReadLine();
                if (message == "exit")
                {
                    break;
                }
                else if (message.StartsWith("start"))
                {
                    string[] values = message.Split(' ');
                    string[] parts = values[1].Split(':');
                    controller.Address = parts[0];
                    controller.Port = parts[1];
                    message = "{ \"name\" : \"newSession\", \"parameters\" : {} }";
                    isStart = true;
                }
                else if (message == "quit")
                {
                    message = "{ \"name\" : \"quit\", \"parameters\" : {} }";
                    isQuit = true;
                }
                else if (message.StartsWith("go"))
                {
                    string[] values = message.Split(' ');
                    message = string.Format("{{ \"name\" : \"get\", \"parameters\": {{ \"url\": \"{0}\" }} }}", values[1]);
                }
                else if (message.StartsWith("title"))
                {
                    message = "{ \"name\" : \"getTitle\", \"parameters\": {} }";
                }
                else if (message.StartsWith("url"))
                {
                    message = "{ \"name\" : \"getCurrentUrl\", \"parameters\": {} }";
                }
                else if (message.StartsWith("source"))
                {
                    message = "{ \"name\" : \"getPageSource\", \"parameters\": {} }";
                }
                else if (message.StartsWith("frame"))
                {
                    string[] values = message.Split(' ');
                    string frameId = values[1];
                    if (frameId.StartsWith(":"))
                    {
                        frameId = string.Format("{{ \"ELEMENT\": \"{0}\" }}", values[1]);
                    }
                    else
                    {
                        int index = 0;
                        if (int.TryParse(values[1], out index))
                        {
                            frameId = values[1];
                        }
                        else
                        {
                            if (values[1] == "top" || values[1] == "default")
                            {
                                frameId = "null";
                            }
                            else
                            {
                                frameId = string.Format("\"{0}\"", values[1]);
                            }
                        }
                    }

                    message = string.Format("{{ \"name\" : \"switchToFrame\", \"parameters\": {{ \"id\": {0} }} }}", frameId);
                }
                else if (message.StartsWith("findall"))
                {
                    string[] values = message.Split(' ');
                    message = string.Format("{{ \"name\" : \"findElements\", \"parameters\": {{ \"using\": \"{0}\", \"value\": \"{1}\" }} }}", values[1], values[2]);
                }
                else if (message.StartsWith("find"))
                {
                    string[] values = message.Split(' ');
                    message = string.Format("{{ \"name\" : \"findElement\", \"parameters\": {{ \"using\": \"{0}\", \"value\": \"{1}\" }} }}", values[1], values[2]);
                }
                else if (message.StartsWith("children"))
                {
                    string[] values = message.Split(' ');
                    message = string.Format("{{ \"name\" : \"findChildElements\", \"parameters\": {{ \"using\": \"{0}\", \"value\": \"{1}\", \"ID\": {{ \"ELEMENT\": \"{2}\" }} }} }}", values[1], values[2], values[3]);
                }
                else if (message.StartsWith("child"))
                {
                    string[] values = message.Split(' ');
                    message = string.Format("{{ \"name\" : \"findChildElement\", \"parameters\": {{ \"using\": \"{0}\", \"value\": \"{1}\", \"ID\": {{ \"ELEMENT\": \"{2}\" }} }} }}", values[1], values[2], values[3]);
                }
                else if (message.StartsWith("script"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    string[] scriptParts = values[1].Split(new char[] { '|' });
                    string scriptSource = scriptParts[0].Trim();
                    StringBuilder argsBuilder = new StringBuilder();
                    for (int i = 1; i < scriptParts.Length; i++)
                    {
                        string arg = scriptParts[i].Trim();
                        if (argsBuilder.Length > 0)
                        {
                            argsBuilder.Append(", ");
                        }

                        if (arg == bool.TrueString || arg == bool.FalseString || arg == "null")
                        {
                            argsBuilder.Append(arg);
                        }
                        else if (arg.StartsWith(":"))
                        {
                            argsBuilder.AppendFormat("{{ \"ELEMENT\": \"{0}\" }}", arg);
                        }
                        else
                        {
                            double numericValue;
                            if (double.TryParse(arg, out numericValue))
                            {
                                argsBuilder.Append(arg);
                            }
                            else
                            {
                                argsBuilder.AppendFormat("\"{0}\"", arg);
                            }
                        }
                    }
                    message = string.Format("{{ \"name\" : \"executeScript\", \"parameters\": {{ \"script\": \"{0}\", \"args\": [{1}] }} }}", scriptSource, argsBuilder.ToString());
                }
                else if (message.StartsWith("async"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    string[] scriptParts = values[1].Split(new char[] { '|' });
                    string scriptSource = scriptParts[0].Trim();
                    StringBuilder argsBuilder = new StringBuilder();
                    for (int i = 1; i < scriptParts.Length; i++)
                    {
                        string arg = scriptParts[i].Trim();
                        if (argsBuilder.Length > 0)
                        {
                            argsBuilder.Append(", ");
                        }

                        if (arg == bool.TrueString || arg == bool.FalseString || arg == "null")
                        {
                            argsBuilder.Append(arg);
                        }
                        else if (arg.StartsWith(":"))
                        {
                            argsBuilder.AppendFormat("{{ \"ELEMENT\": \"{0}\" }}", arg);
                        }
                        else
                        {
                            double numericValue;
                            if (double.TryParse(arg, out numericValue))
                            {
                                argsBuilder.Append(arg);
                            }
                            else
                            {
                                argsBuilder.AppendFormat("\"{0}\"", arg);
                            }
                        }
                    }
                    message = string.Format("{{ \"name\" : \"executeAsyncScript\", \"parameters\": {{ \"script\": \"{0}\", \"args\": [{1}] }} }}", scriptSource, argsBuilder.ToString());
                }
                else if (message.StartsWith("type"))
                {
                    string keys = string.Empty;
                    string[] values = message.Split(new char[] { ' ' }, 3);
                    char[] individualKeys = values[2].ToCharArray();
                    foreach (char key in individualKeys)
                    {
                        if (keys.Length > 0)
                        {
                            keys += ",";
                        }

                        keys += "\"" + key.ToString() + "\"";
                    }

                    message = string.Format("{{ \"name\" : \"sendKeysToElement\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }}, \"value\": [{1}] }} }}", values[1], keys);
                }
                else if (message.StartsWith("click"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"clickElement\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("clear"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"clearElement\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("submit"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"submitElement\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("location"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"getElementLocation\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("size"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"getElementSize\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("text"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"getElementText\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("displayed"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"isElementDisplayed\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("enabled"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"isElementEnabled\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("selected"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"isElementSelected\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("tag"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    message = string.Format("{{ \"name\" : \"getElementTagName\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }} }} }}", values[1]);
                }
                else if (message.StartsWith("attribute"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 3);
                    message = string.Format("{{ \"name\" : \"getElementAttribute\", \"parameters\": {{ \"ID\": {{ \"ELEMENT\": \"{0}\" }}, \"NAME\": \"{1}\" }} }}", values[1], values[2]);
                }
                else if (message.StartsWith("cookie"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 3);
                    if (values.Length < 2)
                    {
                        message = "{ \"name\" : \"getCookies\", \"parameters\": {} }";
                    }
                    else if (values[1].StartsWith("deleteall"))
                    {
                        message = "{ \"name\" : \"deleteAllCookies\", \"parameters\": { } }";
                    }
                    else if (values[1].StartsWith("delete"))
                    {
                        message = string.Format("{{ \"name\" : \"deleteCookie\", \"parameters\": {{ \"NAME\": \"{0}\" }} }}", values[2]);
                    }
                    else
                    {
                        message = string.Format("{{ \"name\" : \"addCookie\", \"parameters\": {{ \"cookie\": {0} }} }}", values[2]);
                    }
                }
                else if (message.StartsWith("back"))
                {
                    message = "{ \"name\" : \"goBack\", \"parameters\": {} }";
                }
                else if (message.StartsWith("forward"))
                {
                    message = "{ \"name\" : \"goForward\", \"parameters\": {} }";
                }
                else if (message.StartsWith("refresh"))
                {
                    message = "{ \"name\" : \"refresh\", \"parameters\": {} }";
                }
                else if (message.StartsWith("orientation"))
                {
                    message = "{ \"name\" : \"getOrientation\", \"parameters\": {} }";
                }
                else if (message.StartsWith("timeout"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 3);
                    if (values[1].StartsWith("implicit"))
                    {
                        message = string.Format("{{ \"name\" : \"implicitlyWait\", \"parameters\" : {{ \"ms\" : {0} }} }}", values[2]);
                    }
                    else if (values[1].StartsWith("script"))
                    {
                        message = string.Format("{{ \"name\" : \"setScriptTimeout\", \"parameters\" : {{ \"ms\" : {0} }} }}", values[2]);
                    }
                    else if (values[1].StartsWith("page"))
                    {

                    }
                }
                else if (message.StartsWith("window"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    if (values[1].StartsWith("size"))
                    {
                        message = "{ \"name\" : \"getWindowSize\", \"parameters\" : {} }";
                    }
                    else if (values[1].StartsWith("position"))
                    {
                        message = "{ \"name\" : \"getWindowPosition\", \"parameters\" : {} }";
                    }
                }
                else if (message.StartsWith("keys"))
                {
                    string keys = string.Empty;
                    string[] values = message.Split(new char[] { ' ' }, 2);
                    char[] individualKeys = values[1].ToCharArray();
                    foreach (char key in individualKeys)
                    {
                        if (keys.Length > 0)
                        {
                            keys += ",";
                        }

                        keys += "\"" + key.ToString() + "\"";
                    }

                    message = string.Format("{{ \"name\" : \"sendKeysToActiveElement\", \"parameters\": {{ \"value\": [{0}] }} }}", keys);
                }
                else if (message.StartsWith("mouse"))
                {
                    string[] values = message.Split(new char[] { ' ' }, 5);
                    if (values[1] == "click")
                    {
                        message = string.Format("{{ \"name\" : \"mouseClick\", \"parameters\": {{ \"button\": 0 }} }}");
                    }
                    else if (values[1] == "down")
                    {
                        message = string.Format("{{ \"name\" : \"mouseDown\", \"parameters\": {{ \"button\": 0 }} }}");
                    }
                    else if (values[1] == "up")
                    {
                        message = string.Format("{{ \"name\" : \"mouseUp\", \"parameters\": {{ \"button\": 0 }} }}");
                    }
                    else if (values[1] == "move")
                    {
                        if (values.Length == 3)
                        {
                            string elementId = values[2];
                            if (values[2] != "null")
                            {
                                elementId = "\"" + values[2] + "\"";
                            }

                            message = string.Format("{{ \"name\" : \"mouseMoveTo\", \"parameters\": {{ \"element\": {0} }} }}", elementId);
                        }
                        else if (values.Length == 4)
                        {
                            message = string.Format("{{ \"name\" : \"mouseMoveTo\", \"parameters\": {{ \"xoffset\": {0}, \"yoffset\": {1} }} }}", values[2], values[3]);
                        }
                        else if (values.Length == 5)
                        {
                            string elementId = values[2];
                            if (values[2] != "null")
                            {
                                elementId = "\"" + values[2] + "\"";
                            }

                            message = string.Format("{{ \"name\" : \"mouseMoveTo\", \"parameters\": {{ \"element\": {0}, \"xoffset\": {1}, \"yoffset\": {2} }} }}", elementId, values[3], values[4]);
                        }
                        else if (values[1].StartsWith("double") || values[1].StartsWith("dbl"))
                        {
                            message = string.Format("{{ \"name\" : \"mouseDoubleClick\", \"parameters\": {{ \"button\": 0 }} }}");
                        }
                    }
                }
                else if (message.StartsWith("screenshot"))
                {
                    message = "{ \"name\" : \"screenshot\", \"parameters\": { } }";
                }
                else if (message.StartsWith("alert"))
                {
                    // Reinstate when alert handling is stable.
                    //string[] values = message.Split(new char[] { ' ' }, 3);
                    //if (values.Length < 2)
                    //{
                    //    message = "{ \"name\" : \"getAlertText\", \"parameters\": {} }";
                    //}
                    //else if (values[1].StartsWith("dismiss"))
                    //{
                    //    ClickButton(controller, 1, true);
                    //    message = "{ \"name\" : \"dismissAlert\", \"parameters\": {} }";
                    //}
                    //else if (values[1].StartsWith("accept"))
                    //{
                    //    ClickButton(controller, 0, true);
                    //    message = "{ \"name\" : \"acceptAlert\", \"parameters\": {} }";
                    //}
                    //else
                    //{
                    //    message = string.Format("{{ \"name\" : \"setAlertValue\", \"parameters\": {{ \"text\": {0} }} }}", values[2]);
                    //}
                    continue;
                }
                else
                {
                    Console.WriteLine("Unknown command: {0}", message);
                    continue;
                }

                Console.WriteLine("Sending message {0}", message);
                watcher.Reset();
                watcher.Start();
                if (isStart)
                {
                    controller.Start();
                    controller.StartSession();
                    isStart = false;
                }

                string response = SendMessage(controller.Address, controller.Port, message);
                if (isQuit)
                {
                    controller.StopSession();
                    isQuit = false;
                }

                watcher.Stop();

                Console.WriteLine("Command duration: {0} ms", watcher.ElapsedMilliseconds);
                Console.WriteLine("Received response {0}", response);
            }
        }

        private static void ClickButton(DeviceController controller, int buttonIndex, bool isPortrait)
        {
            int buttonYOffset = 184;
            int buttonHeight = 42;
            int yCoordinate = buttonYOffset + (buttonHeight / 2);

            int buttonWidth = 198;
            int buttonXOffset = 27;
            int buttonLandscapeOffset = 72;

            int xCoordinate = buttonXOffset + (buttonWidth / 2);
            if (!isPortrait)
            {
                xCoordinate += buttonLandscapeOffset;
            }

            int buttonSpaceOffset = 32;
            xCoordinate += (buttonIndex * buttonSpaceOffset);
            // controller.SynthesizeClick(buttonIndex, xCoordinate, yCoordinate);
        }

        private static string ExtractResponseValue(string response, string valueName)
        {
            string pattern = string.Format("\\\"{0}\\\"\\s*:\\s*([0-9a-zA-Z]*)", valueName);
            string match = Regex.Match(response, pattern).Groups[1].ToString();
            return match;
        }

        private static string SendMessage(string address, string port, string message)
        {
            string receivedMessage = string.Empty;
            Console.WriteLine("Attempting to connect to {0}:{1}", address, port);
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(address, int.Parse(port));
                using (NetworkStream sendStream = new NetworkStream(socket, false))
                {
                    int length = Encoding.UTF8.GetByteCount(message);
                    string datagram = string.Format("{0}:{1}", length, message);
                    sendStream.Write(Encoding.UTF8.GetBytes(datagram), 0, Encoding.UTF8.GetByteCount(datagram));
                }

                using (NetworkStream receiveStream = new NetworkStream(socket, false))
                {
                    StringBuilder dataLengthBuilder = new StringBuilder();
                    int byteValue = receiveStream.ReadByte();
                    char currentChar = Convert.ToChar(byteValue);
                    while (currentChar != ':')
                    {
                        dataLengthBuilder.Append(currentChar);
                        byteValue = receiveStream.ReadByte();
                        currentChar = Convert.ToChar(byteValue);
                    }

                    int dataLength = int.Parse(dataLengthBuilder.ToString());
                    byte[] buffer = new byte[dataLength];
                    int received = receiveStream.Read(buffer, 0, dataLength);
                    receivedMessage = Encoding.UTF8.GetString(buffer, 0, received);
                }
            }

            return receivedMessage;
        }

        private static string GetLocalIPAddress()
        {
            NetworkInterface foundInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet && !i.Name.ToLowerInvariant().Contains("switch"));
            if (foundInterface != null)
            {
                UnicastIPAddressInformation addressInfo = foundInterface.GetIPProperties().UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (addressInfo != null)
                {
                    return addressInfo.Address.ToString();
                }
            }

            return string.Empty;
        }
    }
}
