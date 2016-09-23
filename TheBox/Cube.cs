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

        public Cube(DotStarStrip leftStrip, DotStarStrip rightStrip)
        {
            this.leftStrip = leftStrip;
            this.rightStrip = rightStrip;

            bottomFrontEdge = new Edge(0, 13, rightStrip);
            bottomRightEdge = new Edge(13, 26, rightStrip);
            bottomBackEdge = new Edge(0, 13, leftStrip);
            bottomLeftEdge = new Edge(13, 26, leftStrip);

            frontLeftEdge = new Edge(26, 39, leftStrip);
            frontTopEdge = new Edge(39, 52, leftStrip);

            rightLeftEdge = new Edge(65, 78, rightStrip);
            rightTopEdge = new Edge(52, 65, leftStrip);

            backLeftEdge = new Edge(26, 39, rightStrip);
            backTopEdge = new Edge(39, 52, rightStrip);

            leftLeftEdge = new Edge(65, 78, leftStrip);
            leftTopEdge = new Edge(52, 65, rightStrip);

            bottom = new Face(bottomFrontEdge, bottomRightEdge, bottomBackEdge, bottomLeftEdge);
            front = new Face(frontTopEdge, rightLeftEdge, bottomFrontEdge, frontLeftEdge);
            right = new Face(rightTopEdge, backLeftEdge, bottomRightEdge, rightLeftEdge);
            back = new Face(backTopEdge, leftLeftEdge, bottomBackEdge, backLeftEdge);
            left = new Face(leftTopEdge, frontLeftEdge, bottomLeftEdge, leftLeftEdge);
            top = new Face(frontTopEdge, rightTopEdge, backTopEdge, leftTopEdge);
        }

        public void SetEdgeColor(Edge edge, Color color)
        {
            edge.SetColor(color);
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
