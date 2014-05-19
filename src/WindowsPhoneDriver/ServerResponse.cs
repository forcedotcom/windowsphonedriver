// <copyright file="ServerResponse.cs" company="Salesforce.com">
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
using System.Net;
using System.Text;
using OpenQA.Selenium.Remote;

namespace WindowsPhoneDriver
{
    /// <summary>
    /// Represents the information the server should send to the remote client in its response.
    /// </summary>
    internal class ServerResponse
    {
        #region Private members
        private Response returnedResponse = new Response();
        private HttpStatusCode statusCode = HttpStatusCode.OK;
        private string contentType = "application/json;charset=UTF-8";
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerResponse"/> class.
        /// </summary>
        /// <param name="responseToReturn">A <see cref="Response"/> object to be sent to the remote client.</param>
        /// <param name="status">An <see cref="HttpStatusCode"/> value to set the status code of the HTTP response.</param>
        internal ServerResponse(Response responseToReturn, HttpStatusCode status)
        {
            this.returnedResponse = responseToReturn;
            this.statusCode = status;
            if (this.statusCode >= HttpStatusCode.BadRequest && this.statusCode < HttpStatusCode.InternalServerError)
            {
                this.contentType = "text/plain";
            }
        }

        /// <summary>
        /// Gets the <see cref="Response"/> object to be sent to the remote client.
        /// </summary>
        internal Response ReturnedResponse
        {
            get { return this.returnedResponse; }
        }

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/> value to set the status code of the HTTP response.
        /// </summary>
        internal HttpStatusCode StatusCode
        {
            get { return this.statusCode; }
        }

        /// <summary>
        /// Gets the value to which to set the Content-Type header of the HTTP response.
        /// </summary>
        internal string ContentType
        {
            get { return this.contentType; }
        }
    }
}
