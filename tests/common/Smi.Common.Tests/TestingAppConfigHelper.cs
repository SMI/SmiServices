
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Smi.Common.Tests
{
    public class TestingAppConfigHelper
    {
        public NameValueCollection Collection { get; private set; }

        public TestingAppConfigHelper()
        {
            Collection = new NameValueCollection();
        }

        DirectoryInfo GetSolutionDirectory()
        {
            var cur = new DirectoryInfo(Environment.CurrentDirectory);

            while (cur != null && cur.Exists)
            {
                if (cur.GetFiles("SMIPlugin.sln").Any())
                    return cur;

                cur = cur.Parent;
            }

            return null;
        }

        public void AppendAppConfigFile(string pathRelativeToRoot)
        {
            AppendCommonConfigFile(@".\Microservices\Microservices.Common\common.config");

            var fi = GetFile(pathRelativeToRoot);

            var appSettings = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = fi.FullName
            }, ConfigurationUserLevel.None);

            foreach (var key in appSettings.AppSettings.Settings.AllKeys)
            {
                // Ignore any defined virtual hosts for testing
                if (key.Equals("RabbitMqVirtualHost"))
                    continue;

                AddToCollection(appSettings, key);
            }
        }

        private void AddToCollection(Configuration appSettings, string key)
        {
            string value = appSettings.AppSettings.Settings[key].Value;
            AddToCollection(key, value);
        }

        private FileInfo GetFile(string pathRelativeToRoot)
        {
            var slnDir = GetSolutionDirectory();

            var fi = new FileInfo(Path.Combine(slnDir.FullName, pathRelativeToRoot));

            if (!fi.Exists)
                throw new Exception("Expected app.config to exist at '" + fi.FullName + "'");
            return fi;
        }

        private void AppendCommonConfigFile(string path)
        {
            var fi = GetFile(path);

            var xDoc = XDocument.Load(fi.FullName);

            foreach (var setting in xDoc.Root.Elements("add"))
            {
                // Ignore any defined virtual hosts for testing
                if (setting.Attribute("key").Value == "RabbitMqVirtualHost")
                    continue;

                AddToCollection(setting.Attribute("key").Value, setting.Attribute("value").Value);
            }
        }

        private void AddToCollection(string key, string value)
        {
            if (Collection.AllKeys.Contains(key))
            {
                if (!Equals(Collection.Get(key), value))
                {
                    throw new Exception("There is a disagreement in app.configs over the value of key '" + key + "' ('" +
                                        Collection.Get(key) + "' and '" + value + "'");
                }
            }
            else
                Collection.Add(key, value);
        }
    }
}
