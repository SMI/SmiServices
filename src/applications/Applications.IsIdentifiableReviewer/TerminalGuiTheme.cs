using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace IsIdentifiableReviewer
{
    public class TerminalGuiTheme
    {
        public Color FocusForeground { get; set; }
        public Color FocusBackground { get; set; }

        public Color DisabledForeground { get; set; }
        public Color DisabledBackground { get; set; }

        public Color HotFocusForeground { get; set; }
        public Color HotFocusBackground { get; set; }

        public Color HotNormalForeground { get; set; }
        public Color HotNormalBackground { get; set; }

        public Color NormalForeground { get; set; }
        public Color NormalBackground { get; set; }



        public ColorScheme GetScheme()
        {
            return new ColorScheme
            {
                Focus = Application.Driver.MakeAttribute(FocusForeground, FocusBackground),
                Disabled = Application.Driver.MakeAttribute(DisabledForeground, DisabledBackground),
                HotFocus = Application.Driver.MakeAttribute(HotFocusForeground, HotFocusBackground),
                HotNormal = Application.Driver.MakeAttribute(HotNormalForeground, HotNormalBackground),
                Normal = Application.Driver.MakeAttribute(NormalForeground, NormalBackground),
            };
        }
    }
}
