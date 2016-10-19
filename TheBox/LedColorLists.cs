using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public static class LedColorLists
    {
        public static List<Color> rainbowColors = new List<Color>(new Color[]
        {
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 213, 42, 0),
            Color.FromArgb(255, 171, 85, 0),
            Color.FromArgb(255, 171, 127, 0),
            Color.FromArgb(255, 171, 171, 0),
            Color.FromArgb(255, 86, 213, 0),
            Color.FromArgb(255, 0, 255, 0),
            Color.FromArgb(255, 0, 213, 42),
            Color.FromArgb(255, 0, 171, 85),
            Color.FromArgb(255, 0, 86, 170),
            Color.FromArgb(255, 0, 0, 255),
            Color.FromArgb(255, 42, 0, 213),
            Color.FromArgb(255, 85, 0, 171),
            Color.FromArgb(255, 127, 0, 129),
            Color.FromArgb(255, 171, 0, 85),
            Color.FromArgb(255, 213, 0, 43)
        });

        public static List<Color> redRainbow = new List<Color>(new Color[]
        {
            Color.FromArgb(255, 171, 0, 85),
            Color.FromArgb(255, 213, 0, 43),
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 213, 42, 0),
            Color.FromArgb(255, 171, 85, 0),
            Color.FromArgb(255, 171, 127, 0),
            Color.FromArgb(255, 171, 85, 0),
            Color.FromArgb(255, 213, 42, 0),
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 213, 0, 43),
        });

        public static List<Color> greenRainbow = new List<Color>(new Color[]
        {
            Color.FromArgb(255, 171, 171, 0),
            Color.FromArgb(255, 86, 213, 0),
            Color.FromArgb(255, 0, 255, 0),
            Color.FromArgb(255, 0, 213, 42),
            Color.FromArgb(255, 0, 171, 85),
            Color.FromArgb(255, 0, 213, 42),
            Color.FromArgb(255, 0, 255, 0),
            Color.FromArgb(255, 86, 213, 0),
        });

        public static List<Color> blueRainbow = new List<Color>(new Color[]
        {
            Color.FromArgb(255, 0, 86, 170),
            Color.FromArgb(255, 0, 0, 255),
            Color.FromArgb(255, 42, 0, 213),
            Color.FromArgb(255, 85, 0, 171),
            Color.FromArgb(255, 127, 0, 129),
            Color.FromArgb(255, 85, 0, 171),
            Color.FromArgb(255, 42, 0, 213),
            Color.FromArgb(255, 0, 0, 255),
        });

        public static List<Color> redSingle = new List<Color>(new Color[]
        {
            Colors.Red,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black
        });

        public static List<Color> greenSingle = new List<Color>(new Color[]
        {
            Colors.Green,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black
        });

        public static List<Color> blueSingle = new List<Color>(new Color[]
        {
            Colors.Blue,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black
        });

        public static List<Color> redAlternating = new List<Color>(new Color[]
        {
            Colors.Red,
            Colors.Black
        });

        public static List<Color> greenAlternating = new List<Color>(new Color[]
        {
            Colors.Green,
            Colors.Black
        });

        public static List<Color> blueAlternating = new List<Color>(new Color[]
        {
            Colors.Blue,
            Colors.Black
        });
    }
}
