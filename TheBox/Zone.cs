using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public class Zone : Face
    {
        private bool reverseSpeedStrip;
        public bool ReverseSpeedStrip
        {
            get
            {
                return reverseSpeedStrip;
            }
            set
            {
                reverseSpeedStrip = value;
                top.ReverseSpeedStrip = reverseSpeedStrip;
                right.ReverseSpeedStrip = reverseSpeedStrip;
                bottom.ReverseSpeedStrip = reverseSpeedStrip;
                left.ReverseSpeedStrip = reverseSpeedStrip;
            }
        }

        private bool reverse;
        public bool Reverse
        {
            get
            {
                return reverse;
            }
            set
            {
                reverse = value;
                top.Reverse = reverse;
                right.Reverse = reverse;
                bottom.Reverse = reverse;
                left.Reverse = reverse;
            }
        }

        public Zone(Edge front, Edge right, Edge back, Edge left) : base(front, right, back, left)
        {
        }

        public void SetSpeedStripLedColors(List<Color> ledColors)
        {
            top.SetSpeedStripLedColors(ledColors);
            right.SetSpeedStripLedColors(ledColors);
            bottom.SetSpeedStripLedColors(ledColors);
            left.SetSpeedStripLedColors(ledColors);
        }

        public void ResetMaxes()
        {
            top.ResetMaxes();
            right.ResetMaxes();
            bottom.ResetMaxes();
            left.ResetMaxes();
        }
    }
}
