using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

            Log = () =>
            {
                foreach (KeyValuePair<string, DataStatistics> dataStatisticse in packetStatisticses.Where(pair => pair.Key.StartsWith("/person")).OrderBy(pair => pair.Key))
                {
                    Console.WriteLine($"{dataStatisticse.Key}: {dataStatisticse.Value.Mean}");
                }

                Console.WriteLine();

                foreach (KeyValuePair<string, DataStatistics> dataStatisticse in packetStatisticses.Where(pair => !pair.Key.StartsWith("/person")).OrderBy(pair => pair.Key))
                {
                    Console.WriteLine($"{dataStatisticse.Key}: {dataStatisticse.Value.Mean}");
                }
            };
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

        public Action Log { get; }
    }
}
