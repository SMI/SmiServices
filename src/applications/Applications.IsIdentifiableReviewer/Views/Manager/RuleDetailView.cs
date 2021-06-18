using Microservices.IsIdentifiable.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace IsIdentifiableReviewer.Views.Manager
{
    class RuleDetailView : View
    {
        private Label lblType;

        public RuleDetailView()
        {
            lblType = new Label() { Text = "Type:", Height = 1, Width = Dim.Fill()};

            Add(lblType);
        }

        public void SetRule(ICustomRule rule)
        {
            lblType.Text = "Type:" + rule.GetType().Name;
            SetNeedsDisplay();
        }
    }
}
