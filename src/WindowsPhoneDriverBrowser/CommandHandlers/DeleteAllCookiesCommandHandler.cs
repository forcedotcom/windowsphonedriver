// <copyright file="DeleteAllCookiesCommandHandler.cs" company="Salesforce.com">
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;

namespace WindowsPhoneDriverBrowser.CommandHandlers
{
    /// <summary>
    /// Provides handling for the get all cookies command.
    /// </summary>
    internal class DeleteAllCookiesCommandHandler : CommandHandler
    {
        private const string DeleteCookieScript = @"function(name){
  var trim = function(str) { return str.replace(/^\\s*/, '').replace(/\\s*$/, ''); }
  var getCookieByName = function(cookieName, doc) {
    var ck = document.cookie;
    if (!ck) return null;
    var ckPairs = ck.split(/;/);
    for (var i = 0; i < ckPairs.length; i++) {
      var ckPair = trim(ckPairs[i]);
      var ckNameValue = ckPair.split(/=/);
      var ckName = decodeURIComponent(ckNameValue[0]);
      if (ckName === cookieName) { 
        return decodeURIComponent(ckNameValue[1]);
      }
    }
    return null;
  };
  var deleteCookie = function(cookieName, domain, path, doc) {
    var expireDateInMilliseconds = new Date(1).toGMTString();
    var cookie = cookieName + '=deleted; ';
    if (path) {
      cookie += 'path=' + path + '; ';
    }
    if (domain) {
      cookie += 'domain=' + domain + '; ';
    }
    cookie += 'expires=' + expireDateInMilliseconds;
    document.cookie = cookie;
  };
  var _maybeDeleteCookie = function(cookieName, domain, path, doc) {
    deleteCookie(cookieName, domain, path, doc);
    return false;
  };
  var _recursivelyDeleteCookieDomains = function(cookieName, domain, path, doc) {
    var deleted = _maybeDeleteCookie(cookieName, domain, path, doc);
    if (deleted) return true;
    var dotIndex = domain.indexOf('.');
    if (dotIndex == 0) {
      return _recursivelyDeleteCookieDomains(cookieName, domain.substring(1), path, doc);
    } else if (dotIndex != -1) {
      return _recursivelyDeleteCookieDomains(cookieName, domain.substring(dotIndex), path, doc);
    } else {
      // No more dots; try just not passing in a domain at all
      return _maybeDeleteCookie(cookieName, null, path, doc);
    }
  };
  var _recursivelyDeleteCookie = function(cookieName, domain, path, doc) {
    var slashIndex = path.lastIndexOf('/');
    var finalIndex = path.length-1;
    if (slashIndex == finalIndex) {
      slashIndex--;
    }
    if (slashIndex != -1) {
      deleted = _recursivelyDeleteCookie(cookieName, domain, path.substring(0, slashIndex+1), doc);
    }
    return _recursivelyDeleteCookieDomains(cookieName, domain, path, doc);
  };
  var recursivelyDeleteCookie = function(cookieName, domain, path, win) {
    if (!win) win = window;
    var doc = win.document;
    if (!domain) {
      domain = doc.domain;
    }
    if (!path) {
      path = win.location.pathname;
    }
    var deleted = _recursivelyDeleteCookie(cookieName, '.' + domain, path, doc);
    // Finally try a null path (Try it last because it's uncommon)
    deleted = _recursivelyDeleteCookieDomains(cookieName, '.' + domain, null, doc);
  };
  recursivelyDeleteCookie(name);
}";

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="environment">The <see cref="CommandEnvironment"/> to use in executing the command.</param>
        /// <param name="parameters">The <see cref="Dictionary{string, object}"/> containing the command parameters.</param>
        /// <returns>The JSON serialized string representing the command response.</returns>
        public override Response Execute(CommandEnvironment environment, Dictionary<string, object> parameters)
        {
            List<object> cookieList = new List<object>();
            CookieCollection cookies = null;
            ManualResetEvent synchronizer = new ManualResetEvent(false);
            environment.Browser.Dispatcher.BeginInvoke(() =>
            {
                if (environment.Browser.Source != null)
                {
                    cookies = environment.Browser.GetCookies();
                }

                synchronizer.Set();
            });

            synchronizer.WaitOne();

            if (cookies != null)
            {
                foreach (Cookie currentCookie in cookies)
                {
                    string result = this.EvaluateAtom(environment, DeleteCookieScript, currentCookie.Name, environment.CreateFrameObject());
                }
            }

            return Response.CreateSuccessResponse();
        }
    }
}
