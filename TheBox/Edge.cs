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
        }

        public void SetColor(Color color)
        {
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
