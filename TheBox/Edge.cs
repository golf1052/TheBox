using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public class Edge
    {
        public int[] leds;
        public DotStarStrip strip;

        private byte brightness;
        public byte Brightness
        {
            get
            {
                return brightness;
            }
            set
            {
                brightness = value;
                for (int i = 0; i < speedStripBeginning.LedColors.Count; i++)
                {
                    Color c = speedStripBeginning.LedColors[i];
                    c.A = brightness;
                    speedStripBeginning.LedColors[i] = c;
                }
                for (int i = 0; i < speedStripEnd.LedColors.Count; i++)
                {
                    Color c = speedStripEnd.LedColors[i];
                    c.A = brightness;
                    speedStripEnd.LedColors[i] = c;
                }
                foreach (int i in leds)
                {
                    Color c = strip.strip[i];
                    c.A = value;
                    strip.strip[i] = c;
                }
            }
        }

        private AdjustableMax leftMax;
        private AdjustableMax rightMax;
        public bool Reverse { get; set; }

        public LedSpeedStrip speedStripBeginning;
        public LedSpeedStrip speedStripEnd;
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
                speedStripBeginning.Reverse = reverseSpeedStrip;
                speedStripEnd.Reverse = reverseSpeedStrip;
            }
        }

        public int flairOffset;

        public Edge(int start, int end, DotStarStrip strip)
        {
            this.strip = strip;
            leds = new int[end - start];
            int j = 0;
            for (int i = start; i < end; i++)
            {
                leds[j] = i;
                j++;
            }
            brightness = 127;
            leftMax = new AdjustableMax();
            rightMax = new AdjustableMax();
            float speedRollover = 0.5f;
            speedStripBeginning = new LedSpeedStrip(this, LedSpeedStrip.Sides.MiddleToBeginning, speedRollover);
            speedStripEnd = new LedSpeedStrip(this, LedSpeedStrip.Sides.MiddleToEnd, speedRollover);
            flairOffset = 0;
        }

        public void SetSpeedStripLedColors(List<Color> ledColors)
        {
            for (int i = 0; i < ledColors.Count; i++)
            {
                Color c = ledColors[i];
                c.A = Brightness;
                ledColors[i] = c;
            }
            speedStripBeginning.LedColors = ledColors;
            speedStripEnd.LedColors = ledColors;
        }

        public void SetColor(Color color)
        {
            color.A = Brightness;
            foreach (int i in leds)
            {
                strip.strip[i] = color;
            }
        }

        public void StepFlair(List<Color> ledColors, bool reverse)
        {
            HelperMethods.SendAlongStrip(this, ledColors, ref flairOffset, reverse: reverse);
        }

        public void Reset()
        {
            SetColor(Colors.Black);
        }

        public void ResetMaxes()
        {
            leftMax.Reset();
            rightMax.Reset();
        }

        public void Update()
        {
            strip.SendPixels();
        }

        public void UpdateLeft(float value, Color color)
        {
            color.A = Brightness;
            leftMax.Value = value;
            float val = leftMax.Value;
            if (!Reverse)
            {
                DoMiddleToBeginning(val, color);
            }
            else
            {
                DoBeginningToMiddle(val, color);
            }
        }

        public void UpdateRight(float value, Color color)
        {
            color.A = Brightness;
            rightMax.Value = value;
            float val = rightMax.Value;
            if (!Reverse)
            {
                DoMiddleToEnd(val, color);
            }
            else
            {
                DoEndToMiddle(val, color);
            }
        }

        private void DoMiddleToBeginning(float value, Color color)
        {
            for (int i = 5; i >= 0; i--)
            {
                if (i >= 6 - Math.Floor(value * 6f))
                {
                    strip.strip[leds[i]] = color;
                }
                else
                {
                    strip.strip[leds[i]] = Colors.Black;
                }
            }
        }

        private void DoBeginningToMiddle(float value, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                if (i < Math.Floor(value * 6f))
                {
                    strip.strip[leds[i]] = color;
                }
                else
                {
                    strip.strip[leds[i]] = Colors.Black;
                }
            }
        }

        private void DoMiddleToEnd(float value, Color color)
        {
            for (int i = 7; i < 13; i++)
            {
                if (i < Math.Floor(value * 6f) + 7)
                {
                    strip.strip[leds[i]] = color;
                }
                else
                {
                    strip.strip[leds[i]] = Colors.Black;
                }
            }
        }

        private void DoEndToMiddle(float value, Color color)
        {
            for (int i = 12; i >= 7; i--)
            {
                if (i >= 13 - Math.Floor(value * 6f))
                {
                    strip.strip[leds[i]] = color;
                }
                else
                {
                    strip.strip[leds[i]] = Colors.Black;
                }
            }
        }
    }
}
