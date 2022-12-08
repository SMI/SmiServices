using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace IsIdentifiableReviewer.Views
{
    internal class FailureGroupingNode : TreeNodeWithCount
    {
        public string Group {get;}
        public OutstandingFailureNode[] Failures {get;}

        public FailureGroupingNode(string group, OutstandingFailureNode[] failures):base(group)
        {
            this.Group = group;
            this.Failures = failures;

            Children = failures.OrderByDescending(f=>f.NumberOfTimesReported).Cast<ITreeNode>().ToList();
        }
    }
}
