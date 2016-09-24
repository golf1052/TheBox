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

        public List<Color> ledColors;

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
    }
}
