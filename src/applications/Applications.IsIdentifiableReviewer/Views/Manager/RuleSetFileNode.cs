using Microservices.IsIdentifiable.Rules;
using Microservices.IsIdentifiable.Runners;
using System.IO;
using YamlDotNet.Serialization;

namespace IsIdentifiableReviewer.Views.Manager
{
    internal class RuleSetFileNode
    {
        public FileInfo File { get; set; }

        /// <summary>
        /// The ruleset in the <see cref="File"/>.  It is important to populate this only once
        /// otherwise children generated from this class will end up with different collections
        /// over time.
        /// </summary>
        private RuleSet _ruleSet;

        public RuleSetFileNode(FileInfo file)
        {
            this.File = file;
        }

        /// <summary>
        /// Opens the ruleset file and reads all rules.  This caches 
        /// </summary>
        /// <returns></returns>
        public RuleSet GetRuleSet()
        {
            if(_ruleSet != null)
            {
                return _ruleSet;
            }

            var yaml = System.IO.File.ReadAllText(File.FullName);

            var deserializer = IsIdentifiableAbstractRunner.GetDeserializer();
            return _ruleSet = deserializer.Deserialize<RuleSet>(yaml);
        }

        public void Save(FileInfo toFile = null)
        {
            if (_ruleSet == null)
            {
                throw new System.Exception("Cannot save before children have been created, expected GetRuleSet() to have been called before now");
            }

            if(toFile == null)
            {
                toFile = File;
            }
            
            //populated rules file already existed
            var builder = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithIndentedSequences();

            var serializer = builder.Build();
            using (var sw = new StreamWriter(toFile.FullName))
            {
                serializer.Serialize(sw, _ruleSet);
            }
        }

        public override string ToString()
        {
            return File.Name;
        }
    }
}
