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

        [ConfigurationProperty("cacheDir", DefaultValue = "~/yui/cache/")]
        public string CacheDir
        {
            get { return (string)this["cacheDir"]; }
            set { this["cacheDir"] = value; }
        }
    }
}
