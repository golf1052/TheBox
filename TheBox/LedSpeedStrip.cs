using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public class LedSpeedStrip
    {
        public enum Sides
        {
            MiddleToBeginning,
            MiddleToEnd
        }

        private int offset;
        private List<Color> ledColors;
        public List<Color> LedColors
        {
            get
            {
                return ledColors;
            }
            set
            {
                ledColors = value;
                offset = 0;
            }
        }

        private Edge edge;
        private Sides side;

        private float speedRollover = 500000;
        private float currentSpeed = 0;

        public LedSpeedStrip(Edge edge, Sides side)
        {
            ledColors = new List<Color>();
            this.edge = edge;
            this.side = side;
        }

        public LedSpeedStrip(Edge edge, Sides side, float speedRollover) : this(edge, side)
        {
            this.speedRollover = speedRollover;
        }

        public void UpdateSpeed(float speed)
        {
            if (currentSpeed >= speedRollover)
            {
                currentSpeed %= speedRollover;
                Step();
            }
            else
            {
                currentSpeed += speed;
            }
        }

        private void Step()
        {
            if (side == Sides.MiddleToBeginning)
            {
                DoMiddleToBeginning();
            }
            else if (side == Sides.MiddleToEnd)
            {
                DoMiddleToEnd();
            }
            offset++;
            if (offset >= ledColors.Count)
            {
                offset = 0;
            }
            edge.Update();
        }

        private void DoMiddleToBeginning()
        {
            int j = offset;
            for (int i = 5; i >= 0; i--)
            {
                edge.strip.strip[edge.leds[i]] = ledColors[j];
                j++;
                j %= ledColors.Count - 1;
            }
        }

        private void DoMiddleToEnd()
        {
            int j = offset;
            for (int i = 7; i < 13; i++)
            {
                edge.strip.strip[edge.leds[i]] = ledColors[j];
                j++;
                j %= ledColors.Count - 1;
            }
        }
    }
}
