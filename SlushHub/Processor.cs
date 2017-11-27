using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Bespoke.Common.Osc;
using SlushHub.Statistics;

namespace SlushHub
{
    internal class Processor
    {
        private readonly int windowSize;

        public Processor(int windowSize)
        {
            this.windowSize = windowSize;

            packetStatisticses = new ConcurrentDictionary<string, DataStatistics>();
        }

        private readonly ConcurrentDictionary<string, DataStatistics> packetStatisticses;

        public void InsertEmotion(int emtotion)
        {
            Task.Run(() =>
            {
                Push("/emotion", emtotion);
            });
        }

        public void InsertReason(int reason)
        {
            Task.Run(() =>
            {
                Push("/reason", reason);
            });
        }

        public void InsertOscPacket(OscPacket oscPacket)
        {
            Push(oscPacket.Address, oscPacket.At<int>(0));
        }

        private void Push(string address, int value)
        {
            Task.Run(() =>
            {
                DataStatistics dataStatistics = new DataStatistics(windowSize);

                packetStatisticses.TryAdd(address, dataStatistics);

                packetStatisticses[address].Push(value);
            });
        }

        public int this[string index] => packetStatisticses.ContainsKey(index) ? (int)packetStatisticses[index].Mean : 0;

        public string[] Addresses => packetStatisticses.Keys.ToArray();
    }
}
