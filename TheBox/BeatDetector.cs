using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheBox
{
    public class BeatDetector
    {
        public bool Beat { get; private set; }
        private AdjustableMax currentValue;
        private List<AdjustableMax> previousValues;
        private int size;

        public BeatDetector(int size)
        {
            this.size = size;
            currentValue = new AdjustableMax();
            previousValues = new List<AdjustableMax>(size);
        }

        public void UpdateBeat(float value)
        {
            currentValue.Value = value;
            if (previousValues.Count >= size)
            {
                List<float> values = new List<float>(size);
                for (int i = 0; i < size; i++)
                {
                    values.Add(previousValues[i].Value);
                }
                float average = HelperMethods.Average(values);
                if (currentValue.Value > average + 0.1f)
                {
                    Beat = true;
                }
                else
                {
                    Beat = false;
                }
                while (previousValues.Count > size)
                {
                    previousValues.RemoveAt(0);
                }
            }
            previousValues.Add(currentValue);
        }
    }
}
