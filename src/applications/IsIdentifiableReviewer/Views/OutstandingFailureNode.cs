using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    internal class OutstandingFailureNode : TreeNode
    {
        public string InputValue { get; }
        public int NumberOfTimesReported { get; }

        public OutstandingFailureNode(string inputValue, int numberOfTimesReported)
        {
            InputValue = inputValue;
            NumberOfTimesReported = numberOfTimesReported;
        }

        public override string ToString()
        {
            return $"{ InputValue } x{NumberOfTimesReported:N0}";
        }
    }
}