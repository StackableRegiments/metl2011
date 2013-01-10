﻿namespace MeTLLib
{
    using System;
    using System.Configuration;
    using System.Diagnostics;

    public static class MeTLConfiguration
    {
        private static MeTLConfigurationSection conf = null;
        public static MeTLConfigurationSection Config 
        { 
            get
            {
                if (conf == null)
                    throw new InvalidOperationException("MeTLConfiguration.Load must be called before using Config");
                else
                    return conf;
            }
            private set
            {
                conf = value; 
            }
        }

        public static void Load()
        {
            try
            {
                Config = ConfigurationManager.GetSection("metlConfigurationGroup/metlConfiguration") as MeTLConfigurationSection;
            }
            catch (ConfigurationErrorsException e)
            {
                Trace.TraceError("Unable to load MeTL Configuration from app.config. Reason: " + e.Message);
            }
        }
    }

    public class MeTLConfigurationSection : ConfigurationSection 
    {
        [ConfigurationProperty("production")]
        public ProductionServerElement Production
        {
            get
            {
                return (ProductionServerElement)this["production"];
            }
            set
            {
                this["production"] = value;
            }
        }

        [ConfigurationProperty("staging")]
        public StagingServerElement Staging
        {
            get
            {
                return (StagingServerElement)this["staging"];
            }
            set
            {
                this["staging"] = value;
            }
        }

        [ConfigurationProperty("external")]
        public ExternalServerElement External
        {
            get
            {
                return (ExternalServerElement)this["external"];
            }
            set
            {
                this["external"] = value;
            }
        }

        [ConfigurationProperty("logging")]
        public LoggingServerElement Logging
        {
            get
            {
                return (LoggingServerElement)this["logging"];
            }
            set
            {
                this["logging"] = value; 
            }
        }
    }

    public class ProductionServerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=false)]
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

        [ConfigurationProperty("isBootstrapUrl", DefaultValue=true, IsRequired=false)]
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

        [ConfigurationProperty("meggleUrl", DefaultValue="http://meggle-prod.adm.monash.edu:8080/search?query=", IsRequired=true)]
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

        [ConfigurationProperty("host", DefaultValue="http://metl.adm.monash.edu.au/server.xml", IsRequired=true)]
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

    public class StagingServerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=false)]
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

        [ConfigurationProperty("isBootstrapUrl", DefaultValue=true, IsRequired=false)]
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

        [ConfigurationProperty("meggleUrl", DefaultValue="http://meggle-staging.adm.monash.edu:8080/search?query=", IsRequired=true)]
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

        [ConfigurationProperty("host", DefaultValue="http://metl.adm.monash.edu.au/stagingServer.xml", IsRequired=true)]
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

    public class ExternalServerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=false)]
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

        [ConfigurationProperty("isBootstrapUrl", DefaultValue=false, IsRequired=false)]
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

        [ConfigurationProperty("host", DefaultValue="http://civic.adm.monash.edu.au", IsRequired=true)]
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

        [ConfigurationProperty("meggleUrl", DefaultValue="http://meggle-ext.adm.monash.edu:8080/search?query=", IsRequired=true)]
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
    }

    public class LoggingServerElement : ConfigurationElement
    {
        [ConfigurationProperty("host", DefaultValue="https://madam.adm.monash.edu.au:1188/log_message.yaws", IsRequired=true)]
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
}