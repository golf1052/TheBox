using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheBox
{
    public class AdjustableMax
    {
        private float currentMax;
        private float lastValue;
        private float value;
        public float Value
        {
            get
            {
                if (currentMax == 0)
                {
                    return 0;
                }
                float returningValue = value / currentMax;
                if (returningValue < 0.1)
                {
                    return lastValue / currentMax;
                }
                else
                {
                    return returningValue;
                }
            }
            set
            {
                this.lastValue = this.value;
                this.value = value;
                if (currentMax < value)
                {
                    currentMax = value;
                }
            }
        }

        public AdjustableMax()
        {
            currentMax = 0;
            value = 0;
        }

        public void Reset()
        {
            currentMax = 0;
            value = 0;
        }
    }
}
