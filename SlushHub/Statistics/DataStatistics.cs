using System.Linq;

namespace SlushHub.Statistics
{
    public class DataStatistics
    {
        private int count;

        private double lastMean;

        private readonly FixedSizedQueue<double> values;

        public int Count => values.Count;

        public double MovingMean => values != null && !values.IsEmpty ? values.Average() : 0;

        public double Mean => MovingMean;

        public double RunningMean { get; private set; }

        public double NormalizedMean => MovingMean - RunningMean;

        public DataStatistics(int windowSize)
        {
            values = new FixedSizedQueue<double>(windowSize);
        }

        public void Push(double value)
        {
            ++count;

            if (count == 1)
            {
                RunningMean = lastMean = value;
            }
            else
            {
                RunningMean = lastMean + (value - lastMean) / count;

                lastMean = RunningMean;
            }

            values.Enqueue(value);
        }

        public void Clear()
        {
            values.Clear();

            count = 0;

            RunningMean = lastMean = 0.0;
        }
    }
}
