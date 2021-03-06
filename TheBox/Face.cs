﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace TheBox
{
    public class Face
    {
        protected Edge top;
        protected Edge right;
        protected Edge bottom;
        protected Edge left;

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
                top.Brightness = value;
                right.Brightness = value;
                bottom.Brightness = value;
                left.Brightness = value;
            }
        }

        public Face(Edge top, Edge right, Edge bottom, Edge left)
        {
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.left = left;
            brightness = 127;
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
