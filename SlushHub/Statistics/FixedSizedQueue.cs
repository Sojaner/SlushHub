using System.Collections.Generic;

namespace SlushHub.Statistics
{
    public class FixedSizedQueue<T> : Queue<T>
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

        public FixedSizedQueue(int capacity) : base(capacity)
        {
            Capacity = capacity;
        }

        public new void Enqueue(T item)
        {
            if (Count == Capacity)
            {
                Dequeue();
            }

            base.Enqueue(item);
        }

        private void ShrinkQueue()
        {
            while (Count > Capacity)
            {
                Dequeue();
            }
        }

        public new void TrimExcess()
        {
            base.TrimExcess();

            Capacity = Count;
        }
    }
}
