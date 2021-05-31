using System;
using Terminal.Gui;

namespace IsIdentifiableReviewer.Views
{
    class SpinnerView : View
    {
        int stage;

        public SpinnerView()
        {
            Width = 1;
            Height = 1;
            CanFocus = false;

            Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(0.25), Tick);
        }

        private bool Tick(MainLoop arg)
        {
            if (Visible)
            {
                stage = (stage + 1) % 4;
                SetNeedsDisplay();
            }

            return true;
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            Move(0, 0);

            Rune rune;
            switch(stage)
            {
                case 0:
                    rune = Driver.VLine;
                    break;
                case 1: rune= '/';
                    break;
                case 2:
                    rune = Driver.HLine;
                    break;
                case 3:
                    rune = '\\';
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(stage));
            }

            AddRune(0,0,rune);

        }
    }
}
