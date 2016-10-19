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
                foreach (int i in leds)
                {
                    Color c = strip.strip[i];
                    c.A = value;
                    strip.strip[i] = c;
                }
            }
        }

        public List<Color> ledColors;

        private AdjustableMax leftMax;
        private AdjustableMax rightMax;
        public bool Reverse { get; set; }

        public LedSpeedStrip speedStripBeginning;
        public LedSpeedStrip speedStripEnd;

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
            brightness = 255;
            ledColors = new List<Color>();
            leftMax = new AdjustableMax();
            rightMax = new AdjustableMax();
            float speedRollover = 0.5f;
            speedStripBeginning = new LedSpeedStrip(this, LedSpeedStrip.Sides.MiddleToBeginning, speedRollover);
            speedStripEnd = new LedSpeedStrip(this, LedSpeedStrip.Sides.MiddleToEnd, speedRollover);
        }

        public void SetSpeedStripLedColors(List<Color> ledColors)
        {
            speedStripBeginning.LedColors = ledColors;
            speedStripEnd.LedColors = ledColors;
        }

        public async Task DoLine()
        {
            int offset = 0;
            for (int times = 0; times < ledColors.Count; times++)
            {
                int j = offset;
                for (int i = 0; i < leds.Length; i++)
                {
                    strip.strip[leds[i]] = ledColors[j];
                    j++;
                    j %= ledColors.Count - 1;
                }
                offset++;
                if (offset >= ledColors.Count)
                {
                    offset = 0;
                }
                Update();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        public void SetColor(Color color)
        {
            color.A = Brightness;
            foreach (int i in leds)
            {
                strip.strip[i] = color;
            }
        }

        public void SetLedColors()
        {
            for (int i = 0; i < leds.Length; i++)
            {
                strip.strip[leds[i]] = ledColors[i];
            }
        }

        public void Reset()
        {
            SetColor(Colors.Black);
        }

        public void Update()
        {
            strip.SendPixels();
        }

        float speedRollover = 500000;
        float speed = 1;
        float currentSpeed = 0;

        public void UpdateSpeedLeft()
        {
            int offset = 0;
            for (int times = 0; times < ledColors.Count; times++)
            {
                int j = offset;
                for (int i = 0; i < leds.Length; i++)
                {
                    strip.strip[leds[i]] = ledColors[j];
                    j++;
                    j %= ledColors.Count - 1;
                }
                offset++;
                if (offset >= ledColors.Count)
                {
                    offset = 0;
                }
                Update();
                while (true)
                {
                    if (currentSpeed >= speedRollover)
                    {
                        currentSpeed %= speedRollover;
                        break;
                    }
                    else
                    {
                        currentSpeed += speed;
                    }
                }
            }
        }

        int leftOffset = 0;
        int rightOffset = 0;

        public void StepLeft()
        {
            int j = leftOffset;
            for (int i = 5; i >= 0; i--)
            {
                strip.strip[leds[i]] = ledColors[j];
                j++;
                j %= ledColors.Count - 1;
            }
            leftOffset++;
            if (leftOffset >= ledColors.Count)
            {
                leftOffset = 0;
            }
            Update();
        }

        public void UpdateLeft(float value, Color color)
        {
            leftMax.Value = value;
            float val = leftMax.Value;
            if (!Reverse)
            {
                DoMiddleToBeginning(val, color);
            }
            else
            {
                DoMiddleToEnd(val, color);
            }
        }

        public void UpdateRight(float value, Color color)
        {
            rightMax.Value = value;
            float val = rightMax.Value;
            if (!Reverse)
            {
                DoMiddleToEnd(val, color);
            }
            else
            {
                DoMiddleToBeginning(val, color);
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
                    strip.strip[leds[i]] = Colors.White;
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
                    strip.strip[leds[i]] = Colors.White;
                }
            }
        }
    }
}
