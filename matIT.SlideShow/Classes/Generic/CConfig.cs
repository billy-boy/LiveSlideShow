using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMS.Profile;

namespace matIT.Generic
{
    public class CConfig
    {

        #region Singleton

        private static CConfig _instance;

        public static CConfig getInstance()
        {
            if (_instance == null)
                _instance = new CConfig();
            return _instance;
        }

        #endregion

        private Profile _profile;
        private Dictionary<String,String> _config;

        public CConfig()
        {
            _profile = new Config();
            _config = new Dictionary<String, String>();
        }

        public String getConfigValue(String section, String key, String defaultString = "")
        {
            if (!_config.ContainsKey(section+@"\"+key))
                _config[section + @"\" + key] = _profile.GetValue(section, key, defaultString);
            return _config[section + @"\" + key];
        }

        public void setConfigValue(String section, String key, String value)
        {
            _config[section + @"\" + key] = value;
            _profile.SetValue(section, key, value);
        }

        private void reloadConfig()
        {
            _config.Clear();
        }

    }
}
