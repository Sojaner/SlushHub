using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bespoke.Common.Osc;
using SlushHub.Statistics;
using Timer = System.Timers.Timer;

namespace SlushHub
{
    internal class Pulser
    {
        private readonly IPAddress[] forwardingIPAddresses;

        private readonly Timer timer0;

        private readonly Timer timer1;

        private readonly Timer timer2;

        private readonly Timer timer3;

        private readonly DataStatistics dataStatistics1;

        private readonly DataStatistics dataStatistics2;

        private readonly DataStatistics dataStatistics3;

        private readonly int threshold;

        private readonly int maximumHeartRate;

        private readonly int minimumHeartRate;

        private int calls0;

        private int called1;

        private int called2;

        private int called3;

        public Action Log { get; }

        public Pulser(int windowSize, IPAddress[] forwardingIPAddresses, int threshold, int maximumHeartRate, int minimumHeartRate)
        {
            this.threshold = threshold;

            this.maximumHeartRate = maximumHeartRate;

            this.minimumHeartRate = minimumHeartRate;

            this.forwardingIPAddresses = forwardingIPAddresses;

            timer0 = new Timer { Enabled = false };

            timer0.Elapsed += (sender, args) => Task.Run(() =>
            {
                calls0++;

                Broadcast("/heartbeat/average");
            });

            timer1 = new Timer { Enabled = false };

            timer1.Elapsed += (sender, args) => Task.Run(() =>
            {
                called1++;

                Broadcast("/heartbeat/person1");
            });

            timer2 = new Timer { Enabled = false };

            timer2.Elapsed += (sender, args) => Task.Run(() =>
            {
                called2++;

                Broadcast("/heartbeat/person2");
            });

            timer3 = new Timer { Enabled = false };

            timer3.Elapsed += (sender, args) => Task.Run(() =>
            {
                called3++;

                Broadcast("/heartbeat/person3");
            });

            dataStatistics1 = new DataStatistics(windowSize);

            dataStatistics2 = new DataStatistics(windowSize);

            dataStatistics3 = new DataStatistics(windowSize);

            Log = () =>
            {
                Console.WriteLine($"AV: {timer0.Interval} {timer0.Enabled} {calls0}");

                Console.WriteLine($"P1: {timer1.Interval} {timer1.Enabled} {called1}");

                Console.WriteLine($"P2: {timer2.Interval} {timer2.Enabled} {called2}");

                Console.WriteLine($"P3: {timer3.Interval} {timer3.Enabled} {called3}");
            };
        }

        private void Broadcast(string person)
        {
            foreach (IPAddress address in forwardingIPAddresses)
            {
                Task.Run(() =>
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(address, 9999);

                    OscMessage message = new OscMessage(ipEndPoint, person);

                    message.Send(ipEndPoint);
                });
            }
        }

        private void Push0()
        {
            List<double> doubles = new List<double>(new double[] { dataStatistics1.Mean, dataStatistics2.Mean, dataStatistics3.Mean });

            doubles.RemoveAll(d => d <= 0);

            int average = (int)doubles.Average();

            SetInterval(average, timer0);
        }

        public void Push1(int bpm)
        {
            dataStatistics1.Push(bpm);

            SetInterval((int)dataStatistics1.Mean, timer1);

            Push0();
        }

        public void Push2(int bpm)
        {
            dataStatistics2.Push(bpm);

            SetInterval((int)dataStatistics2.Mean, timer2);

            Push0();
        }

        public void Push3(int bpm)
        {
            dataStatistics3.Push(bpm);

            SetInterval((int)dataStatistics3.Mean, timer3);

            Push0();
        }

        private void SetInterval(int mean, Timer timer)
        {
            int localMean = mean.LimitTo(minimumHeartRate, maximumHeartRate);

            if (localMean > 0)
            {
                int interval = 60 * 1000 / localMean;

                if (localMean > 0 && Math.Abs(interval - timer.Interval) > threshold)
                {
                    timer.Interval = interval;

                    if (!timer.Enabled)
                    {
                        timer.Enabled = true;
                    }
                }
            }
        }
    }
}
