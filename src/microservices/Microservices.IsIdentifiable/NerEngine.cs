using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.ling;
using java.io;
using java.util.zip;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Whitelists;
using ArrayList = java.util.ArrayList;
using File = System.IO.File;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace Microservices.IsIdentifiable
{
    /// <summary>
    /// Wrapper for Stanford NER library with extra whitelist handling
    /// </summary>
    public class NerEngine
    {
        private static readonly object OLockClassifier = new object();

        private static CRFClassifier _classifier;
        private static readonly Dictionary<IWhitelistSource,HashSet<string>> AllCachedWhitelists = new Dictionary<IWhitelistSource, HashSet<string>>();
        
        private readonly HashSet<string> _whitelist;

        /// <summary>
        /// True to process ^ symbols as spaces.  Defaults to true.
        /// </summary>
        public bool TreatCaretAsSpace { get; set; }

        /// <summary>
        /// Primes engine for classifying strings based on the provided classifier
        /// </summary>
        /// <param name="pathToStanfordNerClassifier">Full path to classifier file e.g.  C:\temp\stanford-ner-2016-10-31\classifiers\english.all.3class.distsim.crf.ser.gz</param>
        /// <param name="whitelistSource">Optional provider of strings which should be ignored when classifying</param>
        public NerEngine(string pathToStanfordNerClassifier, IWhitelistSource whitelistSource)
        {
            TreatCaretAsSpace = true;

            //initialize one classifier for all instances
            lock (OLockClassifier)
            {
                if (_classifier == null)
                {
                    if(!File.Exists(pathToStanfordNerClassifier))
                        throw new FileNotFoundException("Could not find file:"+pathToStanfordNerClassifier);

                    if (string.IsNullOrWhiteSpace(pathToStanfordNerClassifier))
                        throw new Exception("PathToStanfordNERClassifier is null, set it to the path to a classifier e.g. english.all.3class.distsim.crf.ser.gz. See https://stanfordnlp.github.io/CoreNLP/index.html#download for classifiers.");
                    
                    using(var byteStream = new ByteArrayInputStream(File.ReadAllBytes(pathToStanfordNerClassifier)))
                        using(var gzipStream =  new GZIPInputStream(byteStream))
                            _classifier = CRFClassifier.getClassifier(gzipStream);

                }

                //if theres a whitelist source and we haven't cached, get it
                if (whitelistSource != null)
                    if (!AllCachedWhitelists.ContainsKey(whitelistSource))
                    {
                        _whitelist = new HashSet<string>(whitelistSource.GetWhitelist(),StringComparer.CurrentCultureIgnoreCase);

                        //cache it for future users
                        AllCachedWhitelists.Add(whitelistSource, _whitelist);
                    }
                    else
                        _whitelist = AllCachedWhitelists[whitelistSource]; //someone else is already using this source, reuse the cached list
            }
        }

        /// <summary>
        /// Returns all person strings in <paramref name="inString"/> unless it is matched by a whitelist
        /// </summary>
        /// <param name="inString"></param>
        /// <returns></returns>
        public IEnumerable<FailurePart> MatchNames(string inString)
        {
            return Match(inString, new[] {"PERSON"});
        }

        /// <summary>
        /// Returns all strings in <paramref name="inString"/> classified as one of the <paramref name="classifications"/> unless it is matched by a whitelist
        /// </summary>
        /// <param name="inString"></param>
        /// <param name="classifications">Array of at least one annotation e.g. "PERSON"</param>
        /// <returns></returns>
        public IEnumerable<FailurePart> Match(string inString, string[] classifications)
        {
            if (string.IsNullOrWhiteSpace(inString))
                yield break;

            if (TreatCaretAsSpace)
                inString = inString.Replace('^', ' ');

            if (_whitelist != null && _whitelist.Contains(inString.Trim()))
                yield break;

            var answer = (IEnumerable)_classifier.classify(inString);

            //CoreLabel
            foreach (var x in answer)
            {
                var list = x as ArrayList;
                if (list != null)
                    foreach (CoreLabel label in list)
                    {
                        var labelAsString = label.getString(typeof (CoreAnnotations.AnswerAnnotation));
                        
                        if (classifications.Contains(labelAsString))
                        {
                            var word = label.value();
                            if (_whitelist != null && _whitelist.Contains(word))
                                continue;

                            FailureClassification classification;
                            if (FailureClassification.TryParse(labelAsString, true, out classification))
                                yield return new FailurePart(word, classification, label.beginPosition());
                            else
                                throw new Exception("Could not parse Classification '" + labelAsString + "' into a FailureClassification enum value");
                        }
                    }
            }
        }
    }
}
