using System.Collections.Concurrent;

namespace SlushHub.Statistics
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private int capacity;

        public int Capacity
        {
            get => capacity;

            set
            {
                capacity = value;

                ShrinkQueue();
            }
        }

        public FixedSizedQueue(int capacity)
        {
            Capacity = capacity;
        }

        public new void Enqueue(T item)
        {
            if (Count == Capacity)
            {
                TryDequeue(out _);
            }

            base.Enqueue(item);
        }

        private void ShrinkQueue()
        {
            while (Count > Capacity)
            {
                TryDequeue(out _);
            }
        }

        public void TrimExcess()
        {
            while (Count > Capacity)
            {
                TryDequeue(out _);
            }

            Capacity = Count;
        }
    }
}
