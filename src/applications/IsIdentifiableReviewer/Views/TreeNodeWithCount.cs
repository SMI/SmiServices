﻿using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    internal class TreeNodeWithCount : TreeNode
    {
        public string Heading { get; }

        public TreeNodeWithCount(string heading)
        {
            Heading = heading;
        }

        public override string ToString()
        {
            return Heading + $" ({Children.Count:N0})";
        }
    }
}