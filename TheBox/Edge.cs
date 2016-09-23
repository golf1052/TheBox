using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public struct Edge
    {
        public int[] leds;
        private DotStarStrip strip;

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

        public List<Color> testLine;

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
            testLine = new List<Color>();
            for (int i = 0; i < 26; i++)
            {
                Color c = Colors.Red;
                if (i < 13)
                {
                    c.A = (byte)(255.0 * (1.0 - ((double)i / 12.0)));
                }
                else
                {
                    c.A = 0;
                }
                
                testLine.Add(c);
            }
        }

        public async Task DoLine()
        {
            int offset = 0;
            for (int times = 0; times < testLine.Count; times++)
            {
                int j = offset;
                for (int i = 0; i < leds.Length; i++)
                {
                    strip.strip[leds[i]] = testLine[j];
                    j++;
                    j %= testLine.Count - 1;
                }
                offset++;
                if (offset >= testLine.Count)
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

        public void Reset()
        {
            SetColor(Colors.Black);
        }

        public void Update()
        {
            strip.SendPixels();
        }
    }
}
