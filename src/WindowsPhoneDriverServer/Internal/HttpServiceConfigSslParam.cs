// <copyright file="HttpServiceConfigSslParam.cs" company="Salesforce.com">
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsPhoneDriverServer.Internal
{
    /// <summary>
    /// Represents the struct used to configure SSL parameters through the HTTP service.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct HttpServiceConfigSslParam
    {
        /// <summary>
        /// The hash length for the SSL encryption.
        /// </summary>
        public int SslHashLength;

        /// <summary>
        /// A pointer to the hash of the SSL encryption.
        /// </summary>
        public IntPtr SslHash;

        /// <summary>
        /// The ID of the application.
        /// </summary>
        public Guid AppId;

        /// <summary>
        /// The name of the SSL certificate store.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string SslCertStoreName;

        /// <summary>
        /// The default check mode for certificates.
        /// </summary>
        public uint DefaultCertCheckMode;

        /// <summary>
        /// The default revocation time for the certificate.
        /// </summary>
        public int DefaultRevocationFreshnessTime;

        /// <summary>
        /// The default revocation timeout for URL retrieval.
        /// </summary>
        public int DefaultRevocationUrlRetrievalTimeout;

        /// <summary>
        /// The default SSL control identifier.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DefaultSslCtlIdentifier;

        /// <summary>
        /// The default SSL control store name.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DefaultSslCtlStoreName;

        /// <summary>
        /// The default flags for the SSL configuration.
        /// </summary>
        public uint DefaultFlags;
    }
}
