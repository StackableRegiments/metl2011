﻿using System.Net;
using System.Text;
using System.Xml.Linq;
using MeTLLib.Providers.Connection;
using MeTLLib.Providers;
using System.Diagnostics;
using Ninject;

namespace MeTLLib.Providers.Connection
{
    public class ResourceUploader
    {
        [Inject]
        public MeTLServerAddress metlServerAddress { private get; set; }
        private HttpResourceProvider _httpResourceProvider;
        public ResourceUploader(HttpResourceProvider provider)
        {
            _httpResourceProvider = provider;
        }
        private string RESOURCE_SERVER_UPLOAD { get { return string.Format("https://{0}:1188/upload_nested.yaws", metlServerAddress.uri.Host); } }
        public string uploadResource(string path, string file)
        {
            return uploadResource(path, file, false);
        }
        public string uploadResource(string path, string file, bool overwrite)
        {
            try
            {
                var fullPath = string.Format("{0}?path=Resource/{1}&overwrite={2}", RESOURCE_SERVER_UPLOAD, path, overwrite);
                var res = _httpResourceProvider.securePutFile(new System.Uri(fullPath), file);
                var url = XElement.Parse(res).Attribute("url").Value;
                return "https://" + url.Split(new[] { "://" }, System.StringSplitOptions.None)[1];
            }
            catch (WebException e)
            {
                Trace.TraceError("Cannot upload resource: " + e.Message);
            }
            return "failed";
        }
        public string uploadResourceToPath(byte[] resourceData, string path, string name)
        {
            return uploadResourceToPath(resourceData, path, name, true);
        }
        public string uploadResourceToPath(byte[] resourceData, string path, string name, bool overwrite)
        {
            var url = string.Format("{0}?path={1}&overwrite={2}&filename={3}", RESOURCE_SERVER_UPLOAD, path, overwrite.ToString().ToLower(), name);
            var res = _httpResourceProvider.securePutData(new System.Uri(url), resourceData);
            return XElement.Parse(res).Attribute("url").Value;
        }
        public string uploadResourceToPath(string localFile, string remotePath, string name)
        {
            return uploadResourceToPath(localFile, remotePath, name, true);
        }
        public string uploadResourceToPath(string localFile, string remotePath, string name, bool overwrite)
        {
            var url = string.Format("{0}?path=Resource/{1}&overwrite={2}&filename={3}", RESOURCE_SERVER_UPLOAD, remotePath, overwrite.ToString().ToLower(), name);
            var res = _httpResourceProvider.securePutFile(new System.Uri(url), localFile);
            return XElement.Parse(res).Attribute("url").Value;
        }
        /*
         * \Resources
         * \Resources\1011\a.png
         * \Structure
         * \Structure\listing.xml
         * \Structure\myFirstConversation\details.xml
         */
    }
}