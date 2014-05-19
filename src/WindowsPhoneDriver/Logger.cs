// <copyright file="Logger.cs" company="Salesforce.com">
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
using System.Text;

namespace WindowsPhoneDriver
{
    /// <summary>
    /// Provides the services required to log messages.
    /// </summary>
    public abstract class Logger
    {
        private LogLevel currentLogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="level">A <see cref="LogLevel"/> value specifying the level of messages to log.</param>
        protected Logger(LogLevel level)
        {
            this.currentLogLevel = level;
        }

        /// <summary>
        /// Writes an informational message to the log.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        public void Log(string message)
        {
            this.Log(message, LogLevel.Info);
        }

        /// <summary>
        /// Writes a message to the log with a given logging level.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        /// <param name="level">The logging level of the message.</param>
        public void Log(string message, LogLevel level)
        {
            if (level >= this.currentLogLevel)
            {
                this.WriteMessage(FormatLogMessage(message, level));
            }
        }

        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        protected abstract void WriteMessage(string message);

        private static string FormatLogMessage(string message, LogLevel level)
        {
            string logTime = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string formattedMessage = string.Format(CultureInfo.InvariantCulture, "{0} {1} - {2}", logTime, level.ToString().ToUpperInvariant(), message);
            return formattedMessage;
        }
    }
}
