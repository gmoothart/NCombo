using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace NCombo
{
    public class NComboSectionHandler: ConfigurationSection
    {
        [ConfigurationProperty("baseDir", DefaultValue = "~/yui/")]
        public string BaseDir { 
            get { return (string)this["baseDir"]; }
            set { this["baseDir"] = value; }
        }

        [ConfigurationProperty("gzip", DefaultValue = "false")]
        public bool Gzip
        {
            get { return (bool)this["gzip"]; }
            set { this["gzip"] = value; }
        }

        [ConfigurationProperty("cache")]
        public NComboCacheElment Cache {
            get { return (NComboCacheElment)this["cache"]; }
            set { this["cache"] = value; }
        }
    }

    public class NComboCacheElment: ConfigurationElement 
    {
        [ConfigurationProperty("enabled", DefaultValue = "true")]
        public bool Enabled {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }
        
        [ConfigurationProperty("directory", DefaultValue = "~/yui/cache/")]
        public string Directory {
            get { return (string)this["directory"]; }
            set { this["directory"] = value; }
        }

    }
}
