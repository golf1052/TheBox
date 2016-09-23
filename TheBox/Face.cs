﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public struct Face
    {
        private Edge top;
        private Edge right;
        private Edge bottom;
        private Edge left;

        public Face(Edge top, Edge right, Edge bottom, Edge left)
        {
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.left = left;
        }

        public void SetColor(Color color)
        {
            top.SetColor(color);
            right.SetColor(color);
            bottom.SetColor(color);
            left.SetColor(color);
        }

        public void Reset()
        {
            top.Reset();
            right.Reset();
            bottom.Reset();
            left.Reset();
        }

        public void Update()
        {
            top.Update();
            right.Update();
            bottom.Update();
            left.Update();
        }
    }
}