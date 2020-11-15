using System;
using System.Collections.Generic;
using System.Linq;
using Dicom.Imaging.Codec;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Rules
{
    /// <summary>
    /// Rule in which 2 or more subrules must agree on common sections of problem
    /// </summary>
    public class ConsensusRule : ICustomRule
    {
        /// <summary>
        /// Rules which must all reach a consesus on classifications
        /// </summary>
        public ICustomRule[] Rules {get;set;}

        /// <summary>
        /// Applies all <see cref="Rules"/> and returns the consensus <see cref="RuleAction"/>.  All rules must agree on the action and at least one index of one word that is a <see cref="FailurePart"/>
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <param name="badParts"></param>
        /// <returns></returns>
        public RuleAction Apply(string fieldName, string fieldValue, out IEnumerable<FailurePart> badParts)
        {
            RuleAction? firstRuleOperation = null;
            List<FailurePart> parts = new List<FailurePart>();

            foreach(var rule in Rules)
            {
                //first time
                if(firstRuleOperation == null)
                {
                    firstRuleOperation = rule.Apply(fieldName,fieldValue,out IEnumerable<FailurePart> newParts);
                    parts.AddRange(newParts);
                }
                else
                {
                    //subsequent times
                    var newOperation = rule.Apply(fieldName,fieldValue,out IEnumerable<FailurePart> newParts);

                    // There is not consensus on whether the value should be classified as bad
                    if(newOperation != firstRuleOperation)
                    {
                        badParts = new FailurePart[0];
                        return RuleAction.None;
                    }

                    parts = Intersect(parts,new List<FailurePart>(newParts));
                    
                    if(!parts.Any() && firstRuleOperation == RuleAction.Report)
                    {
                        //if both rules agree it should be reported but cannot agree on the specific words that should be reported

                        //do not report anything
                        
                        badParts = new FailurePart[0];
                        return RuleAction.None;
                    }
                }

                //if anyone wants no action taken at any point stop consulting others.  Either they will agree in which case we have wasted time or they disagree so we say there is no consensus
                if(firstRuleOperation == RuleAction.None)
                {
                    badParts = new FailurePart[0];
                    return RuleAction.None;
                }
            }

            badParts = parts;
            return firstRuleOperation ?? RuleAction.None;
        }
        
        /// <summary>
        /// Returns the intersection of <paramref name="currentParts"/> with <paramref name="newParts"/>.  To be returned a <see cref="FailurePart"/>
        /// must have a corresponding <see cref="FailurePart"/> in the other set where the <see cref="FailurePart.Classification"/> is the same and
        /// at least one index of the word is contained in the other.        /// 
        /// </summary>
        /// <param name="currentParts"></param>
        /// <param name="newParts"></param>
        /// <returns></returns>
        public List<FailurePart> Intersect(List<FailurePart> currentParts, List<FailurePart> newParts)
        {
            return currentParts.Where(a => newParts.Any(b=>
            
                // They are both the same classification of problem
                a.Classification == b.Classification &&
            
                //they start in the same place or overlap (note this also handles the case where both are -1 e.g. OCR text detected)
                (a.Offset == b.Offset || a.Includes(b.Offset,b.Word.Length))
            
            )).ToList();
        }
    }
}
