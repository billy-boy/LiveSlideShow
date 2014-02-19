/*
 * Licensed under Creative Commons BY SA 3.0 German
 * http://creativecommons.org/licenses/by-sa/3.0/de/ 
 * 
 * Markus Müller, m@-IT
 * created 2012
 * info@mat-it.de - http://www.mat-it.de
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.Xml;

namespace matIT.Generic.Config.AppConfig
{
    /// <summary>
    /// CONFIG-Read & Write
    /// Buffered loader class for App.config - Configurations
    /// --------------------
    /// This class reads from App.config (<add key="" value="" />) and buffers the key value pairs until application is destroyed or reloaded
    /// or the save method is called. This increases performance for the applications using it.
    /// --------------------
    /// Should be used as an singleton but can be used as an obeject!
    /// --------------------
    /// (c) Markus Müller, m@-IT, 2012
    /// </summary>
    public class MAppConfig
    {

        #region Singleton

        private static MAppConfig _instance;

        public static MAppConfig getInstance()
        {
            if (_instance == null)
                _instance = new MAppConfig();
            return _instance;
        }
        
        #endregion

        #region Object

        #region Object-Variables

        private String _configFile = "App.config";
        private Dictionary<String, String> _config = new Dictionary<String, String>();

        #endregion

        #region Konstrukt & Destrukt

        public MAppConfig()
        {
        }

        public MAppConfig(String configFile)
        {
            _configFile = configFile;
        }

        #endregion

        #region Public-Methods

        public String ConfigFile
        {
            get { return _configFile; }
            set { _configFile = value; }
        }

        public String getConfigValue(String section, String key, String defaultString = null)
        {
            if (!_config.ContainsKey(section + "/" + key))
                loadConfigValue(section, key);
            if (!_config.ContainsKey(section + "/" + key))
                return defaultString;
            return _config[section+"/"+key];
        }

        public List<string> getConfigValues(String section, String key)
        {
            List<string> ret = new List<string>();
            if (!_config.ContainsKey(section + "/" +key + "s/" + key))
                loadConfigValues(section +  "/" + key + "s", key);
            if (!_config.ContainsKey(section + "/" + key + "s/" + key))
                return ret;
            foreach (KeyValuePair<string,string> pair in _config)
            {
                if (pair.Key.IndexOf(section + "/" + key + "s/" + key + ".") == 0)
                {
                    ret.Add(pair.Value);
                }
            }
            return ret;
        }

        public void reloadConfig()
        {
            _config.Clear();
        }

        public void setConfigValue(String section, String key, String value)
        {
            _config[section + "/" + key] = value;
            saveConfigValue(section, key, value);
        }

        #endregion

        #region Private-XML-Working-Methods

        private void loadConfigValues(String section, String key)
        {
            //Document initiator
            XPathDocument doc = new XPathDocument(_configFile);
            XPathNavigator nav = doc.CreateNavigator();

            //Navigation to all <add> in Section
            XPathExpression expr;
            expr = nav.Compile("/configuration/" + section + "/add[@key]");
            XPathNodeIterator iterator = nav.Select(expr);
            int i = 1;
            while (iterator.MoveNext())
            {
                XPathNavigator nav2 = iterator.Current.Clone();
                if (nav2.GetAttribute("key", "") == key)
                {
                    //Dummy-Füller
                    if (!_config.ContainsKey(section + "/" + key)) _config[section + "/" + key] = String.Empty;
                    //Durchnummerierte Sub-Items
                    _config[section + "/" + key+"."+i.ToString()] = nav2.GetAttribute("value", "");
                    i++;
                }
            }
        }

        private void loadConfigValue(String section, String key)
        {
            //Document initiator
            XPathDocument doc = new XPathDocument(_configFile);
            XPathNavigator nav = doc.CreateNavigator();

            //Navigation to all <add> in Section
            XPathExpression expr;
            expr = nav.Compile("/configuration/" + section + "/add[@key]");
            XPathNodeIterator iterator = nav.Select(expr);
            while (iterator.MoveNext())
            {
                XPathNavigator nav2 = iterator.Current.Clone();
                if (nav2.GetAttribute("key", "") == key)
                {
                    _config[section + "/" + key] = nav2.GetAttribute("value", "");
                    break;
                }
            }
        }

        private void saveConfigValue(String section, String key, String value)
        {
            //Load the document
            XmlTextReader reader = new XmlTextReader(_configFile);
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            reader.Close();

            //Create an new XML Node
            XmlElement newNode = doc.CreateElement("add");
            newNode.SetAttribute("key", key);
            newNode.SetAttribute("value", value);

            //Select the matching old XML Node
            XmlNode oldNode;
            XmlNode parent;
            //Select the <configuration> Element (root Element)
            XmlElement root = doc.DocumentElement;
            oldNode = root.SelectSingleNode("/configuration/" + section + "[1]/add[@key='"+key+"']");
            parent = root.SelectSingleNode("/configuration/" + section + "[1]");
            //Replace or Insert
            if (oldNode != null)
            {
                parent.ReplaceChild(newNode, oldNode);
            }
            else
            {
                //Detect section
                oldNode = root.SelectSingleNode("/configuration/" + section + "[1]");
                if (oldNode == null)
                {
                    //Create section
                    XmlNode sectionNode = doc.CreateElement(section);
                    //Insert section to root
                    root.AppendChild(sectionNode);
                    //Select section
                    oldNode = root.SelectSingleNode("/configuration/" + section + "[1]");
                }
                //Insert new node
                if (oldNode != null)
                    oldNode.AppendChild(newNode);
            }

            //Save the file
            doc.Save(_configFile);
        }
    
        #endregion

        #endregion

    }
}
