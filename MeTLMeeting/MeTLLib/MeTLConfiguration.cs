﻿namespace MeTLLib
{
    using mshtml;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Windows;
    using System.Xml.Linq;

    public class MeTLConfigurationProxy
    {
        public MeTLConfigurationProxy(
            string _name,
            Uri _imageUrl,
            Uri _host
        )
        {
            name = _name;
            imageUrl = _imageUrl;
            host = _host;
        }
        public string name { get; protected set; }
        public Uri imageUrl { get; protected set; }
        public Uri host { get; protected set; }
        public Uri serverStatus
        {
            get { return new System.Uri(host, new System.Uri("/serverStatus", UriKind.Relative)); }
        }
        public Uri authenticationUrl
        {
            get
            {
                return new Uri(host, new Uri("/authenticationState", UriKind.Relative));
            }
        }
    }
    public class MetlConfiguration
    {
        public MetlConfiguration(
            string _name,
            string _imageUrl,
            string _xmppDomain,
            string _xmppUsername,
            string _xmppPassword,
            Uri _host
       )
        {
            name = _name;
            imageUrl = _imageUrl;
            xmppDomain = _xmppDomain;
            xmppUsername = _xmppUsername;
            xmppPassword = _xmppPassword;
            host = _host;
        }

        public Uri themes(int id)
        {
            return new Uri(host, string.Format("/themes?source={0}", id));
        }

        public string name { get; protected set; }
        public string imageUrl { get; protected set; }
        public string xmppDomain { get; protected set; }
        public string xmppUsername { get; protected set; }
        public string xmppPassword { get; protected set; }
        public Uri host { get; protected set; }
        public string xmppHost
        {
            get { return host.Host; }
        }
        public Uri getSummary(string slideJid) {
            return new Uri(host, new Uri(string.Format("/describeHistory?source={0}",slideJid), UriKind.Relative));
        }
        public Uri getResource(string identity)
        {
            return new Uri(host, new Uri(String.Format("/resourceProxy/{0}", HttpUtility.UrlEncode(identity)), UriKind.Relative));
        }
        public Uri getImage(string jid, string url)
        {
            return new Uri(host, new Uri(String.Format("/proxyImageUrl/{0}?source={1}", jid, HttpUtility.UrlEncode(url)), UriKind.Relative));
        }
        public Uri thumbnailUri(string jid)
        {
            return new Uri(host, new Uri(String.Format("/thumbnail/{0}", jid), UriKind.Relative));
        }
        public Uri renderUri(string jid, int width, int height)
        {
            return new Uri(host, new Uri(String.Format("/render/{0}/{1}/{2}", jid, width, height), UriKind.Relative));
        }
        public Uri widgetUri
        {
            get
            {
                return new Uri(host, "/static/widget.html");
            }
        }
        public Uri authenticationUrl
        {
            get
            {
                return new Uri(host, new Uri("/authenticationState", UriKind.Relative));
            }
        }
        public Uri conversationDetails(string jid)
        {
            return new System.Uri(host, new Uri(String.Format("/details/{0}", jid), UriKind.Relative));
        }
        public Uri conversationQuery(string query)
        {
            return new Uri(host, new Uri(String.Format("/search?query={0}", HttpUtility.UrlEncode(query)), UriKind.Relative));
        }
        public Uri uploadResource(string preferredFilename, string jid)
        {
            return new Uri(host, new Uri(String.Format("/upload?filename={0}&jid={1}", preferredFilename, jid), UriKind.Relative));
        }
        public Uri addSlideAtIndex(string jid, int currentSlideId)
        {
            return new System.Uri(host, new Uri(String.Format("/addSlideAtIndex/{0}/{1}", jid, currentSlideId), UriKind.Relative));
        }
        public Uri duplicateSlide(int slideId, string conversationJid)
        {
            return new Uri(host, new Uri(String.Format("/duplicateSlide/{0}/{1}", slideId.ToString(), conversationJid), UriKind.Relative));
        }
        public Uri duplicateConversation(string conversationJid)
        {
            return new Uri(host, new Uri(String.Format("/duplicateConversation/{0}", conversationJid), UriKind.Relative));
        }
        public Uri updateConversation(string conversationJid)
        {
            return new System.Uri(host, new Uri(String.Format("/updateConversation/{0}", conversationJid), UriKind.Relative));
        }
        public Uri createConversation(string title)
        {
            return new Uri(host, new Uri(String.Format("/createConversation/{0}", HttpUtility.UrlEncode(title)), UriKind.Relative));
        }
        public Uri displayQuizOnNewSlideAtIndex(string conversation,int index, long quizId) {
            return new Uri(host, new Uri(String.Format("/addQuizViewSlideToConversationAtIndex/{0}/{1}/{2}", conversation,index, quizId), UriKind.Relative));
        }
        public Uri displayQuizResultsOnNewSlideAtIndex(string conversation, int index, long quizId)
        {
            return new Uri(host, new Uri(String.Format("/addQuizResultsViewSlideToConversationAtIndex/{0}/{1}/{2}", conversation,index, quizId), UriKind.Relative));
        }
        public Uri displaySubmissionOnNewSlideAtIndex(string conversation, int index)
        {
            return new Uri(host, new Uri(String.Format("/addSubmissionSlideToConversationAtIndex/{0}/{1}", conversation,index), UriKind.Relative));
        }
        public Uri serverStatus
        {
            get { return new System.Uri(host, new System.Uri("/serverStatus", UriKind.Relative)); }
        }
        public Uri getRoomHistory(string jid)
        {
            return new Uri(host, new Uri("/fullClientHistory?source=" + HttpUtility.UrlEncode(jid), UriKind.Relative));
        }
        public Uri importConversation()
        {
            return new Uri(host, new Uri("/conversationImportAsMe",UriKind.Relative));
        }
        public Uri importPowerpoint(string title, int magnification)
        {
            return new Uri(host, new Uri(String.Format("/powerpointImport?title={0}&magnification={1}", HttpUtility.UrlEncode(title), magnification.ToString()), UriKind.Relative));
        }
        public Uri importPowerpointFlexible(string title)
        {
            return new Uri(host, new Uri(String.Format("/powerpointImportFlexible?title={0}",HttpUtility.UrlEncode(title)), UriKind.Relative));
        }
        public string muc
        {
            get { return "conference." + xmppDomain; }
        }
        public string globalMuc
        {
            get { return "global@" + muc; }
        }
        public static readonly MetlConfiguration empty = new MetlConfiguration("", "", "", "", "", new Uri("http://localhost:8080/"));

    }
    public abstract class MetlConfigurationManager
    {
        public IAuditor auditor { get; protected set; }
        public MetlConfigurationManager(IAuditor _auditor)
        {
            auditor = _auditor;
            reload();
        }
        public MetlConfiguration getConfigFor(MeTLConfigurationProxy server)
        {
            return internalGetConfigFor(server);
        }
        public List<MeTLConfigurationProxy> servers { get; protected set; }
        protected abstract MetlConfiguration internalGetConfigFor(MeTLConfigurationProxy server);
        protected abstract void internalGetServers();
        public void reload()
        {
            internalGetServers();
        }
        protected List<XElement> getElementsByTag(List<XElement> x, String tagName)
        {
            // it's not recursive!
            var children = x.Select(xel => { return getElementsByTag(xel.Elements().ToList(), tagName); });
            var root = x.FindAll((xel) =>
            {
                return xel.Name.LocalName.ToString().Trim().ToLower() == tagName.Trim().ToLower();
            });
            foreach (List<XElement> child in children)
            {
                root.AddRange(child);
            }
            return root;
        }
        public List<MetlConfiguration> parseConfig(MeTLConfigurationProxy server, HTMLDocument doc)
        {
            var authDataContainer = doc.getElementById("authData");
            if (authDataContainer == null)
            {
                return new List<MetlConfiguration>();
            }
            var html = authDataContainer.innerHTML;
            if (html == null)
            {
                return new List<MetlConfiguration>();
            }
            var xml = XDocument.Parse(html).Elements().ToList();
            var cc = getElementsByTag(xml, "clientConfig");
            var xmppDomain = getElementsByTag(cc, "xmppDomain").First().Value;
            var xmppUsername = getElementsByTag(cc, "xmppUsername").First().Value;
            var xmppPassword = getElementsByTag(cc, "xmppPassword").First().Value;
            var imageUrl = getElementsByTag(cc, "imageUrl").First().Value;
            return new List<MetlConfiguration>
            {
                new MetlConfiguration(server.name,server.imageUrl.ToString(),xmppDomain,xmppUsername,xmppPassword,server.host)
            };
        }
        public List<MetlConfiguration> parseConfig(MeTLConfigurationProxy server, XElement doc)
        {
            var cc = doc.Descendants("clientconfig");
            var xmppDomain = cc.Descendants("xmppdomain").First().Value;
            var xmppUsername = cc.Descendants("xmppusername").First().Value;
            var xmppPassword = cc.Descendants("xmpppassword").First().Value;
            var imageUrl = cc.Descendants("imageurl").First().Value;
            return new List<MetlConfiguration>
            {
                new MetlConfiguration(server.name,server.imageUrl.ToString(),xmppDomain,xmppUsername,xmppPassword,server.host)
            };
        }
    }
    public class RemoteAppMeTLConfigurationManager : MetlConfigurationManager
    {
        public RemoteAppMeTLConfigurationManager(IAuditor auditor) : base(auditor) { }
        override protected MetlConfiguration internalGetConfigFor(MeTLConfigurationProxy server)
        {
            throw new NotImplementedException();
        }
        override protected void internalGetServers()
        {
            try
            {
                var wc = new WebClient();
                var xml = wc.DownloadString(new Uri("http://setup.stackableregiments.com/metlServers/metlServers.xml", UriKind.Absolute));
                var xdoc = XDocument.Parse(xml);
                servers = xdoc.Descendants("metlServer").Select(xms =>
                {
                    var name = xms.Descendants("name").First().Value;
                    var baseUrl = xms.Descendants("baseUrl").First().Value;
                    var imageUrl = xms.Descendants("imageUrl").First().Value;
                    return new MeTLConfigurationProxy(
                        name,
                        new Uri(imageUrl, UriKind.Absolute),
                        new Uri(baseUrl, UriKind.Absolute)
                        );
                }).ToList();
            }
            catch
            {
                servers = new List<MeTLConfigurationProxy>();
            }
        }
    }
    public class LocalAppMeTLConfigurationManager : MetlConfigurationManager
    {
        public LocalAppMeTLConfigurationManager(IAuditor auditor) : base(auditor) { }
        protected Dictionary<MeTLConfigurationProxy, MetlConfiguration> internalConfigs = new Dictionary<MeTLConfigurationProxy, MetlConfiguration>();
        override protected void internalGetServers()
        {
            var dmc = new deprecatedLib.MeTLConfiguration(auditor);
            dmc.Load();
            var config = dmc.Config;
            internalConfigs = new List<deprecatedLib.StackServerElement> { config.Production, config.Staging, config.External }
            .Where(conf => !String.IsNullOrEmpty(conf.Host))
            .Select(conf =>
              {
                  return new MetlConfiguration(
                      conf.Name,
                      conf.Image,
                          conf.xmppServiceName,
                          config.XmppCredential.Username,
                          config.XmppCredential.Password,
                          new Uri(String.Format("{0}://{1}:{2}", conf.Protocol, conf.Host, conf.HistoryPort))
                      );
              }).ToDictionary<MetlConfiguration, MeTLConfigurationProxy>(mc =>
              {
                  return new MeTLConfigurationProxy(
                      mc.name,
                      new Uri(mc.imageUrl),
                      mc.authenticationUrl
                      );
              });
            servers = internalConfigs.Keys.ToList();
        }
        override protected MetlConfiguration internalGetConfigFor(MeTLConfigurationProxy server)
        {
            return internalConfigs[server];
        }
    }
}

namespace deprecatedLib
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using MeTLLib;
    using System.Linq;
    public class MeTLConfiguration
    {
        public IAuditor auditor { get; protected set; }
        public MeTLConfiguration(IAuditor _auditor)
        {
            auditor = _auditor;
        }
        private MeTLConfigurationSection conf = null;
        public MeTLConfigurationSection Config
        {
            get
            {
                if (conf == null)
                {
                    throw new InvalidOperationException("MeTLConfiguration.Load must be called before using Config");
                }
                else
                {
                    return conf;
                }
            }
            private set
            {
                if (value == null)
                {
                    throw new InvalidOperationException("MeTLConfiguration cannot be set to a null value");
                }
                conf = value;
            }
        }

        public void Load()
        {
            try
            {
                var section = ConfigurationManager.GetSection("metlConfigurationGroup/metlConfiguration");
                var mc = section as MeTLConfigurationSection;
                if (mc != null)
                {
                    Config = mc;
                }
            }
            catch (Exception e)
            {
                auditor.error("Load","MeTLConfiguration",e);
                throw e;
            }
        }
    }

    public class MeTLConfigurationSection : ConfigurationSection
    {
        /*
        public MeTLServerAddress.serverMode ActiveStackEnum
        {
            get
            {
                return (MeTLServerAddress.serverMode)Enum.Parse(typeof(MeTLServerAddress.serverMode), ActiveStackConfig.Name, true);
            }
        }
        */
        public StackServerElement ActiveStack
        {
            get
            {
                var active = ActiveStackConfig.Name;
                if (Properties.Contains(active))
                    return (StackServerElement)this[active];
                else
                    throw new ArgumentException(String.Format("Active Stack Server {0} specified not found", active));
            }
        }

        [ConfigurationProperty("activeStackConfig")]
        public ActiveStackElement ActiveStackConfig
        {
            get
            {
                return (ActiveStackElement)this["activeStackConfig"];
            }
            set
            {
                this["activeStackConfig"] = value;
            }
        }

        [ConfigurationProperty("production")]
        public StackServerElement Production
        {
            get
            {
                return (StackServerElement)this["production"];
            }
            set
            {
                this["production"] = value;
            }
        }

        [ConfigurationProperty("staging")]
        public StackServerElement Staging
        {
            get
            {
                return (StackServerElement)this["staging"];
            }
            set
            {
                this["staging"] = value;
            }
        }

        [ConfigurationProperty("external")]
        public StackServerElement External
        {
            get
            {
                return (StackServerElement)this["external"];
            }
            set
            {
                this["external"] = value;
            }
        }


        [ConfigurationProperty("resourceCredential")]
        public CredentialElement ResourceCredential
        {
            get
            {
                return (CredentialElement)this["resourceCredential"];
            }
            set
            {
                this["resourceCredential"] = value;
            }
        }

        [ConfigurationProperty("xmppCredential")]
        public CredentialElement XmppCredential
        {
            get
            {
                return (CredentialElement)this["xmppCredential"];
            }
            set
            {
                this["xmppCredential"] = value;
            }
        }

        [ConfigurationProperty("crypto")]
        public CryptoElement Crypto
        {
            get
            {
                return (CryptoElement)this["crypto"];
            }
            set
            {
                this["crypto"] = value;
            }
        }
    }

    public class StackServerElement : ConfigurationElement
    {
        [ConfigurationProperty("displayIndex")]
        public int DisplayIndex
        {
            get
            {
                return (int)this["displayIndex"];
            }
            set
            {
                this["displayIndex"] = value;
            }
        }

        [ConfigurationProperty("name", IsRequired = false)]
        public String Name
        {
            get
            {
                return (String)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }
        [ConfigurationProperty("imageUrl", IsRequired = false)]
        public String Image
        {
            get
            {
                return (String)this["imageUrl"];
            }
            set
            {
                this["imageUrl"] = value;
            }
        }

        [ConfigurationProperty("webAuthenticationEndpoint", IsRequired = true)]
        public String WebAuthenticationEndpoint
        {
            get
            {
                return (String)this["webAuthenticationEndpoint"];
            }
            set
            {
                this["webAuthenticationEndpoint"] = value;
            }
        }
        [ConfigurationProperty("isBootstrapUrl", IsRequired = true)]
        public Boolean IsBootstrapUrl
        {
            get
            {
                return (Boolean)this["isBootstrapUrl"];
            }
            set
            {
                this["isBootstrapUrl"] = value;
            }
        }

        [ConfigurationProperty("meggleUrl", IsRequired = true)]
        public String MeggleUrl
        {
            get
            {
                return (String)this["meggleUrl"];
            }
            set
            {
                this["meggleUrl"] = value;
            }
        }
        [ConfigurationProperty("thumbnail")]
        public String Thumbnail
        {
            get
            {
                return (String)this["thumbnail"];
            }
            set
            {
                this["thumbnail"] = value;
            }
        }
        [ConfigurationProperty("protocol", IsRequired = true)]
        public String Protocol
        {
            get
            {
                return (String)this["protocol"];
            }
            set
            {
                this["protocol"] = value;
            }
        }
        [ConfigurationProperty("host", IsRequired = true)]
        public String Host
        {
            get
            {
                return (String)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }

        [ConfigurationProperty("xmppPort", IsRequired = true)]
        public String XmppPort
        {
            get
            {
                return (String)this["xmppPort"];
            }
            set
            {
                this["port"] = value;
            }
        }
        [ConfigurationProperty("historyPort", IsRequired = true)]
        public String HistoryPort
        {
            get
            {
                return (String)this["historyPort"];
            }
            set
            {
                this["port"] = value;
            }
        }
        [ConfigurationProperty("resourcePort", IsRequired = true)]
        public String ResourcePort
        {
            get
            {
                return (String)this["resourcePort"];
            }
            set
            {
                this["port"] = value;
            }
        }
        [ConfigurationProperty("xmppServiceName", IsRequired = true)]
        public String xmppServiceName
        {
            get
            {
                return (String)this["xmppServiceName"];
            }
            set
            {
                this["xmppServiceName"] = value;
            }
        }
        [ConfigurationProperty("uploadEndpoint", IsRequired = true)]
        public string UploadEndpoint
        {
            get
            {
                return (String)this["uploadEndpoint"];
            }
            set
            {
                this["uploadEndpoint"] = value;
            }
        }
    }

    public class ServerElement : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = true)]
        public String Host
        {
            get
            {
                return (String)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }
    }

    public class CredentialElement : ConfigurationElement
    {
        [ConfigurationProperty("username", IsRequired = true)]
        public String Username
        {
            get
            {
                return (String)this["username"];
            }
            set
            {
                this["username"] = value;
            }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public String Password
        {
            get
            {
                return (String)this["password"];
            }
            set
            {
                this["password"] = value;
            }
        }
    }

    public class ActiveStackElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public String Name
        {
            get
            {
                return (String)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }
    }

    public class CryptoElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true)]
        public String Key
        {
            get
            {
                return (String)this["key"];
            }
            set
            {
                this["key"] = value;
            }
        }

        [ConfigurationProperty("iv", IsRequired = true)]
        public String IV
        {
            get
            {
                return (String)this["iv"];
            }
            set
            {
                this["iv"] = value;
            }
        }
    }
}

