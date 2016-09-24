using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public class Cube
    {
        // 0 - 13
        // 13 - 26
        // 26 - 39
        // 39 - 52
        // 52 - 65
        // 65 - 78

        private DotStarStrip leftStrip;
        private DotStarStrip rightStrip;

        public Edge bottomFrontEdge;
        public Edge bottomRightEdge;
        public Edge bottomBackEdge;
        public Edge bottomLeftEdge;

        public Edge leftFrontEdge;
        public Edge topFrontEdge;

        public Edge rightFrontEdge;
        public Edge topRightEdge;

        public Edge leftBackEdge;
        public Edge topBackEdge;

        public Edge rightBackEdge;
        public Edge topLeftEdge;

        public Face bottom;
        public Face front;
        public Face right;
        public Face back;
        public Face left;
        public Face top;

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
                bottom.Brightness = value;
                front.Brightness = value;
                right.Brightness = value;
                back.Brightness = value;
                left.Brightness = value;
                top.Brightness = value;
            }
        }

        public Cube(DotStarStrip leftStrip, DotStarStrip rightStrip)
        {
            this.leftStrip = leftStrip;
            this.rightStrip = rightStrip;

            bottomFrontEdge = new Edge(0, 13, rightStrip);
            bottomRightEdge = new Edge(13, 26, rightStrip);
            bottomBackEdge = new Edge(0, 13, leftStrip);
            bottomLeftEdge = new Edge(13, 26, leftStrip);

            topFrontEdge = new Edge(39, 52, leftStrip);
            topRightEdge = new Edge(52, 65, leftStrip);
            topBackEdge = new Edge(39, 52, rightStrip);
            topLeftEdge = new Edge(52, 65, rightStrip);

            leftFrontEdge = new Edge(26, 39, leftStrip);
            leftBackEdge = new Edge(26, 39, rightStrip);

            rightFrontEdge = new Edge(65, 78, rightStrip);
            rightBackEdge = new Edge(65, 78, leftStrip);


            bottom = new Face(bottomFrontEdge, bottomRightEdge, bottomBackEdge, bottomLeftEdge);
            front = new Face(topFrontEdge, rightFrontEdge, bottomFrontEdge, leftFrontEdge);
            right = new Face(topRightEdge, rightBackEdge, bottomRightEdge, rightFrontEdge);
            back = new Face(topBackEdge, rightBackEdge, bottomBackEdge, leftBackEdge);
            left = new Face(topLeftEdge, leftFrontEdge, bottomLeftEdge, leftBackEdge);
            top = new Face(topFrontEdge, topRightEdge, topBackEdge, topLeftEdge);

            brightness = 255;
        }


        public void SetEdgeColor(Edge edge, Color color)
        {
            edge.SetColor(color);
        }

        public void SetColor(Color color)
        {
            bottom.SetColor(color);
            front.SetColor(color);
            right.SetColor(color);
            back.SetColor(color);
            left.SetColor(color);
            top.SetColor(color);
        }

        public void Reset()
        {
            bottom.Reset();
            front.Reset();
            right.Reset();
            back.Reset();
            left.Reset();
            top.Reset();
        }

        public void Update()
        {
            bottom.Update();
            front.Update();
            right.Update();
            back.Update();
            left.Update();
            top.Update();
        }
    }
}
