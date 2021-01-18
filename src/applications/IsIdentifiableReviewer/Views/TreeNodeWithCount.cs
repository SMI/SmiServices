using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    internal class TreeNodeWithCount : TreeNode
    {
        public TreeNodeWithCount(string text):base(text)
        {
        }
        public override string ToString()
        {
            return base.ToString() + $" ({Children.Count:N0})";
        }
    }
}