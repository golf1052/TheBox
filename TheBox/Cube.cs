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

        public Edge frontLeftEdge;
        public Edge frontTopEdge;

        public Edge rightLeftEdge;
        public Edge rightTopEdge;

        public Edge backLeftEdge;
        public Edge backTopEdge;

        public Edge leftLeftEdge;
        public Edge leftTopEdge;

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

        public Dictionary<Edge, Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> edgeCoordinates = new Dictionary<Edge, Tuple<Tuple<int, int, int>, Tuple<int, int, int>>>();



        public Cube(DotStarStrip leftStrip, DotStarStrip rightStrip)
        {
            this.leftStrip = leftStrip;
            this.rightStrip = rightStrip;



            bottomFrontEdge = new Edge(0, 13, rightStrip);
            var BOTTOM_FRONT_EDGE_COORDS = new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(1, 14, 0), new Tuple<int, int, int>(13, 14, 0));
            bottomRightEdge = new Edge(13, 26, rightStrip);
            var BOTTOM_RIGHT_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(14, 13, 0), new Tuple<int, int, int>(14, 1, 0));
            bottomBackEdge = new Edge(0, 13, leftStrip);
            var BOTTOM_BACK_EDGE_COORDS = new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(13, 0, 0), new Tuple<int, int, int>(1, 0, 0));
            bottomLeftEdge = new Edge(13, 26, leftStrip);
            var BOTTOM_LEFT_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(0, 1, 0), new Tuple<int, int, int>(0, 13, 0));
            //

            frontLeftEdge = new Edge(26, 39, leftStrip);
            var FRONT_LEFT_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(14, 14, 1), new Tuple<int, int, int>(14, 14, 13));

            frontTopEdge = new Edge(39, 52, leftStrip);
            var FRONT_TOP_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(13, 14, 14), new Tuple<int, int, int>(1, 14, 14));
            //



            rightLeftEdge = new Edge(65, 78, rightStrip);
            var RIGHT_LEFT_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(14, 14, 13), new Tuple<int, int, int>(14, 14, 1));

            rightTopEdge = new Edge(52, 65, leftStrip);
            var RIGHT_TOP_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(14, 13, 14), new Tuple<int, int, int>(14, 1, 14));

            backLeftEdge = new Edge(26, 39, rightStrip);
            var BACK_LEFT_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(14, 0, 1), new Tuple<int, int, int>(14, 0, 13));

            backTopEdge = new Edge(39, 52, rightStrip);
            var BACK_TOP_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(13, 0, 14), new Tuple<int, int, int>(1, 0, 14));

            leftLeftEdge = new Edge(65, 78, leftStrip);
            var LEFT_LEFT_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(0, 0, 13), new Tuple<int, int, int>(0, 0, 1));

            leftTopEdge = new Edge(52, 65, rightStrip);
            var LEFT_TOP_EDGE_COORDS= new Tuple<Tuple<int, int, int>, Tuple<int, int, int>>(new Tuple<int, int, int>(0, 1, 14), new Tuple<int, int, int>(0, 13, 14));


            edgeCoordinates.Add(bottomFrontEdge, BOTTOM_FRONT_EDGE_COORDS);
            edgeCoordinates.Add(bottomRightEdge, BOTTOM_RIGHT_EDGE_COORDS);
            edgeCoordinates.Add(bottomBackEdge, BOTTOM_BACK_EDGE_COORDS);
            edgeCoordinates.Add(bottomLeftEdge, BOTTOM_LEFT_EDGE_COORDS);
            edgeCoordinates.Add(frontLeftEdge, FRONT_LEFT_EDGE_COORDS);
            edgeCoordinates.Add(frontTopEdge, FRONT_TOP_EDGE_COORDS);
            edgeCoordinates.Add(rightLeftEdge, RIGHT_LEFT_EDGE_COORDS);
            edgeCoordinates.Add(rightTopEdge, RIGHT_TOP_EDGE_COORDS);
            edgeCoordinates.Add(backLeftEdge, BACK_LEFT_EDGE_COORDS);
            edgeCoordinates.Add(backTopEdge, BACK_TOP_EDGE_COORDS);
            edgeCoordinates.Add(leftLeftEdge, LEFT_LEFT_EDGE_COORDS);
            edgeCoordinates.Add(leftTopEdge, LEFT_TOP_EDGE_COORDS);


            bottom = new Face(bottomFrontEdge, bottomRightEdge, bottomBackEdge, bottomLeftEdge);
            front = new Face(frontTopEdge, rightLeftEdge, bottomFrontEdge, frontLeftEdge);
            right = new Face(rightTopEdge, backLeftEdge, bottomRightEdge, rightLeftEdge);
            back = new Face(backTopEdge, leftLeftEdge, bottomBackEdge, backLeftEdge);
            left = new Face(leftTopEdge, frontLeftEdge, bottomLeftEdge, leftLeftEdge);
            top = new Face(frontTopEdge, rightTopEdge, backTopEdge, leftTopEdge);

            brightness = 255;
        }


        public delegate Color ColorFunction(double x, double y, double z);

        public void ApplyColorFunction(ColorFunction colorFunction)
        {
            foreach (var edgeCoord in edgeCoordinates)
            {
                var colors = new List<Color>();
                var edge = edgeCoord.Key;
                var edgeCoords = edgeCoord.Value;
                var startCoords = edgeCoords.Item1;
                var endCoords = edgeCoords.Item2;

                var reverseList = false;

                var x1 = Math.Min(startCoords.Item1, endCoords.Item1);
                var y1 = Math.Min(startCoords.Item2, endCoords.Item2);
                var z1 = Math.Min(startCoords.Item3, endCoords.Item3);
                var x2 = Math.Max(startCoords.Item1, endCoords.Item1);
                var y2 = Math.Max(startCoords.Item2, endCoords.Item2);
                var z2 = Math.Max(startCoords.Item3, endCoords.Item3);

                int i1;
                int i2;
                bool ix = false;
                bool iy = false;
                bool iz = false;

                if (x1 != x2)
                {
                    i1 = x1;
                    i2 = x2;
                    ix = true;
                }
                else if (y1 != y2)
                {
                    i1 = y1;
                    i2 = y2;
                    iy = true;
                }
                else
                {
                    i1 = z1;
                    i2 = z2;
                    iz = true;
                }

                if (x1 < startCoords.Item1)
                {
                    reverseList = true;
                }
                if (y1 < startCoords.Item2)
                {
                    reverseList = true;
                }
                if (z1 < startCoords.Item3)
                {
                    reverseList = true;
                }

                for (var i = i1; i <= i2; i++)
                {

                    Color color;
                    if (ix)
                    {
                        color = colorFunction.Invoke(i, y1, z1);
                    } else if (iy)
                    {
                        color = colorFunction.Invoke(x1, i, z1);
                    } else
                    {
                        color = colorFunction.Invoke(x1, y1, i);
                    }

                    colors.Add(color);
          
                }

                if (reverseList)
                {
                    colors.Reverse();
                }

                edge.ledColors = colors;
            }
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

        public void SetLedColors()
        {
            bottom.SetLedColors();
            front.SetLedColors();
            right.SetLedColors();
            back.SetLedColors();
            left.SetLedColors();
            top.SetLedColors();
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
