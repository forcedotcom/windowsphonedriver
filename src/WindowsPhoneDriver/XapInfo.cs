// <copyright file="XapInfo.cs" company="Salesforce.com">
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace WindowsPhoneDriver
{
    /// <summary>
    /// Class that gives the information about a Windows Phone application bundle.
    /// </summary>
    public class XapInfo
    {
        private XapInfo(string archiveFilePath)
        {
            this.ArchiveFilePath = archiveFilePath;
        }

        /// <summary>
        /// Gets the application ID.
        /// </summary>
        public Guid? ApplicationId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the version of the manifest.
        /// </summary>
        public Version ManifestVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether or not the application is native.
        /// </summary>
        public bool IsNative
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the file path to the archive.
        /// </summary>
        public string ArchiveFilePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Reads the application info.
        /// </summary>
        /// <param name="appArchiveFilePath">Path to the file of the application bundle.</param>
        /// <returns>The <see cref="XapInfo"/> containing the information about the application.</returns>
        public static XapInfo ReadApplicationInfo(string appArchiveFilePath)
        {
            XapInfo appInfo = new XapInfo(appArchiveFilePath);
            try
            {
                // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
                // we specify otherwise.
                FileStream appArchiveFileStream = new FileStream(appArchiveFilePath, FileMode.Open, FileAccess.Read);
                using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry appManifestEntry = zipArchive.GetEntry("WMAppManifest.xml");
                    using (Stream appManifestFileStream = appManifestEntry.Open())
                    {
                        XPathDocument manifestDocument = new XPathDocument(appManifestFileStream);
                        XPathNavigator manifestNavigator = manifestDocument.CreateNavigator();
                        XPathNavigator appNodeNavigator = manifestNavigator.SelectSingleNode("//App");
                        appInfo.ApplicationId = new Guid?(new Guid(appNodeNavigator.GetAttribute("ProductID", string.Empty)));
                        string attribute = appNodeNavigator.GetAttribute("RuntimeType", string.Empty);
                        if (attribute.Equals("Modern Native", StringComparison.OrdinalIgnoreCase))
                        {
                            appInfo.IsNative = true;
                        }

                        manifestNavigator.MoveToFirstChild();
                        appInfo.ManifestVersion = new Version(manifestNavigator.GetAttribute("AppPlatformVersion", string.Empty));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new WindowsPhoneDriverException("Unexpected error reading application information.", ex);
            }

            return appInfo;
        }

        /// <summary>
        /// Extracts the icon file from the application bundle.
        /// </summary>
        /// <returns>The full path to the extracted icon file.</returns>
        public string ExtractIconFile()
        {
            return this.ExtractFileFromApplicationArchive(@"Assets\ApplicationIcon.png");
        }

        /// <summary>
        /// Deletes a file from the application bundle.
        /// </summary>
        /// <param name="pathInArchive">The relative path of the file inside the application bundle.</param>
        public void DeleteFileFromApplicationArchive(string pathInArchive)
        {
            // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
            // we specify otherwise.
            FileStream appArchiveFileStream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.ReadWrite);
            using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Update))
            {
                ZipArchiveEntry iconFileEntry = zipArchive.GetEntry(pathInArchive);
                iconFileEntry.Delete();
            }
        }

        /// <summary>
        /// Inserts a file into the application bundle.
        /// </summary>
        /// <param name="filePath">The file to insert into the application bundle.</param>
        /// <param name="pathInArchive">The relative path of the file inside the application bundle.</param>
        public void InsertFileIntoApplicationArchive(string filePath, string pathInArchive)
        {
            // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
            // we specify otherwise.
            FileStream appArchiveFileStream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.ReadWrite);
            using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Update))
            {
                ZipArchiveEntry iconFileEntry = zipArchive.CreateEntry(pathInArchive, CompressionLevel.Fastest);
                using (Stream iconFileStream = iconFileEntry.Open())
                {
                    if (iconFileStream == null)
                    {
                        throw new WindowsPhoneDriverException("Could not get file stream for icon from application archive.");
                    }

                    using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open))
                    {
                        inputFileStream.CopyTo(iconFileStream);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a file from the application bundle.
        /// </summary>
        /// <param name="pathInArchive">The relative path of the file inside the application bundle.</param>
        /// <returns>The full path to the extracted file.</returns>
        public string ExtractFileFromApplicationArchive(string pathInArchive)
        {
            string result = string.Empty;

            // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
            // we specify otherwise.
            FileStream appArchiveFileStream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.Read);
            using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Read))
            {
                string tempFileName = Path.GetTempFileName();
                ZipArchiveEntry iconFileEntry = zipArchive.GetEntry(pathInArchive);
                using (Stream iconFileStream = iconFileEntry.Open())
                {
                    if (iconFileStream == null)
                    {
                        throw new WindowsPhoneDriverException("Could not get file stream for icon from application archive.");
                    }

                    using (FileStream iconOutputFileStream = new FileStream(tempFileName, FileMode.Create))
                    {
                        iconFileStream.CopyTo(iconOutputFileStream);
                    }
                }

                result = tempFileName;
            }

            return result;
        }
    }
}
