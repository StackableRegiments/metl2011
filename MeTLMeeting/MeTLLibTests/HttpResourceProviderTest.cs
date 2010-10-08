﻿using MeTLLib.Providers.Connection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;
using Ninject;
using System.Text;
using MeTLLib;
using Ninject.Modules;

namespace MeTLLibTests
{
    [TestClass()]
    public class HttpResourceProviderTest
    {
        //Screw this guy
        private TestContext testContextInstance;
        //This guy too 
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        //These guys are useful for setting up fixtures.  Kind of akin to that globals situation we talked about though so avoid it unless you're starting to rock data driven testing.
        #region Additional test attributes
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //I have no idea why they just couldn't call these Setup and Teardown like EVERYONE ELSE IN THE WORLD
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        #endregion
        //Don't test constructors, this is retarded.
        [TestMethod()]
        public void HttpResourceProviderConstructorTest()
        {
        }
        //This _Accessor it's come up with is pretty cool too.  No idea how it works.  It looks static, so how can it shadow instance members?  Of which instance?
        //Oh.  Everything in ResourceProvider is static.  Did I do that?  There's an incorrect singleton in client() which is not threadsafe by the way :D
        //So, the _Accessor is exactly the proxy object we talked about, which turns the class inside out.
        //Ps these all aren't static anymore
        //[TestMethod()]
        //[DeploymentItem("MeTLLib.dll")]
        //public void NotifyStatusTest()
        //{
            /*
            string status = string.Empty; // TODO: Initialize to an appropriate value
            string type = string.Empty; // TODO: Initialize to an appropriate value
            string uri = string.Empty; // TODO: Initialize to an appropriate value
            string filename = string.Empty; // TODO: Initialize to an appropriate value
            HttpResourceProvider_Accessor.NotifyStatus(status, type, uri, filename);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
             */
        //}
        /*
         * Ok, let's try for this one.  It pretty much involves all the hard bits of anything, so if we can get this we can do the rest.
         * it's got the remote file system, it's got asynchronous bullshit going on, there's plenty of action here.
         */
        [TestMethod()]
        public void providerCallsClientUploadFileWithCorrectlyFormattedUrl()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            Assert.AreEqual( "http://nowhere.adm.monash.edu/resources/something.ext", provider.securePutFile(new System.Uri("http://resourceServer.wherever"), "something.ext"));
        }
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void nullUriPassedToSecurePutFileFails()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            provider.securePutFile(null, "something.ext");
        }
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void nullFilePassedToSecurePutFileFails()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            provider.securePutFile(new System.Uri("http://resourceServer.wherever"), null);
        }
        [TestMethod()]
        public void providerCallsClientUploadDataWithCorrectlyFormattedUrl()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            Assert.AreEqual("http://nowhere.adm.monash.edu/resources/something.ext", provider.securePutFile(new System.Uri("http://resourceServer.wherever"), "something.ext"));
        }
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void nullUriPassedToSecurePutDataFails()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            provider.securePutData(null, new byte[]{});
        }
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void nullDataPassedToSecurePutDataFails()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            provider.securePutData(new System.Uri("http://nowhere.adm.monash.edu/resources/something.exe"), null);
        }

        [TestMethod()]
        public void providerCallsClientDownloadStringWithCorrectlyFormattedUrl()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            Assert.AreEqual("<type>data</type>", provider.secureGetString(new System.Uri("http://resourceServer.wherever")));
        }
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void nullUriPassedToSecureGetStringFails()
        {
            IKernel kernel = new StandardKernel(new BaseModule());
            kernel.Bind<IWebClientFactory>().To<StubWebClientFactory>().InSingletonScope();
            HttpResourceProvider provider = kernel.Get<HttpResourceProvider>();
            provider.secureGetString(null);
        }

    }
    #region Stubs
    public class StubWebClientFactory : MeTLLib.Providers.Connection.IWebClientFactory {
        public IWebClient client()
        {
            return new StubWebClient();
        }
    }
    public class StubWebClient : IWebClient {
        //Normal rules about encapsulation don't apply to testing utilities.  We WANT to be able to look inside them all the time.
        public long getSize(Uri resource)
        {
            if (resource == null) throw new ArgumentNullException("address", "Value cannot be null.");
            return (long)1000;
        }
        public bool exists(Uri resource)
        {
            if (resource == null) throw new ArgumentNullException("address", "Value cannot be null.");
            return true;
        }
        public void downloadStringAsync(Uri resource)
        {
            throw new NotImplementedException();
        }
        public string downloadString(Uri resource)
        {
            if (resource == null) throw new ArgumentNullException("address", "Value cannot be null.");
            return "<type>data</type>";
        }
        public byte[] downloadData(Uri resource)
        {
            if (resource == null) throw new ArgumentNullException("address", "Value cannot be null.");
            return new byte[] {60,116,121,112,101,62,100,97,116,97,60,47,116,121,112,101,62};
        }
        public string uploadData(Uri resource, byte[] data)
        {
            if (resource == null) throw new ArgumentNullException("address", "Value cannot be null.");
            if (data == null) throw new ArgumentNullException("data", "Value cannot be null.");
            return "http://nowhere.adm.monash.edu/resources/something.ext";
        }
        public void uploadDataAsync(Uri resource, byte[] data)
        {
            throw new NotImplementedException();
        }
        public byte[] uploadFile(Uri resource, string filename)
        {
            if (resource == null) throw new ArgumentNullException("address", "Value cannot be null.");
            if (filename == null) throw new ArgumentNullException("filename", "Value cannot be null.");
            return Encoding.UTF8.GetBytes("http://nowhere.adm.monash.edu/resources/something.ext");
        }
        public void uploadFileAsync(Uri resource, string filename)
        {
            throw new NotImplementedException();
        }
    }
    public class StubCredentials : ICredentials
    {
        public NetworkCredential GetCredential(Uri uri, string authType)
        {
            return new NetworkCredential();
        }
    }
#endregion
}