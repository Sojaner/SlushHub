using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Bespoke.Common.Osc;
using SlushHub.Statistics;

namespace SlushHub
{
    internal class Pulser
    {
        private readonly IPAddress[] forwardingIPAddresses;

        private readonly Timer timer1;

        private readonly Timer timer2;

        private readonly Timer timer3;

        private readonly Timer timer0;

        private readonly DataStatistics dataStatistics1;

        private readonly DataStatistics dataStatistics2;

        private readonly DataStatistics dataStatistics3;

        private const int threshold = 25;

        public Pulser(int windowSize, IPAddress[] forwardingIPAddresses)
        {
            this.forwardingIPAddresses = forwardingIPAddresses;

            timer0 = new Timer { Enabled = false };

            timer0.Elapsed += (sender, args) => Task.Run(() =>
            {
                x0++;

                Broadcast("/heartbeat/average");
            });

            timer1 = new Timer { Enabled = false };

            timer1.Elapsed += (sender, args) => Task.Run(() =>
            {
                x1++;

                Broadcast("/heartbeat/person1");
            });

            timer2 = new Timer { Enabled = false };

            timer2.Elapsed += (sender, args) => Task.Run(() =>
            {
                x2++;

                Broadcast("/heartbeat/person2");
            });

            timer3 = new Timer { Enabled = false };

            timer3.Elapsed += (sender, args) => Task.Run(() =>
            {
                x3++;

                Broadcast("/heartbeat/person3");
            });

            dataStatistics1 = new DataStatistics(windowSize);

            dataStatistics2 = new DataStatistics(windowSize);

            dataStatistics3 = new DataStatistics(windowSize);

            Task.Run(() =>
            {
                while (true)
                {
                    Console.Clear();

                    Console.WriteLine($"0: {timer0.Interval} {timer0.Enabled} {x0}");

                    Console.WriteLine($"1: {timer1.Interval} {timer1.Enabled} {x1}");

                    Console.WriteLine($"2: {timer2.Interval} {timer2.Enabled} {x2}");

                    Console.WriteLine($"3: {timer3.Interval} {timer3.Enabled} {x3}");

                    System.Threading.Thread.Sleep(50);
                }
            });
        }

        private int x0;
        private int x1;
        private int x2;
        private int x3;

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
            double average = new double[] { dataStatistics1.Mean, dataStatistics2.Mean, dataStatistics3.Mean }.Average();

            if (average > 0)
            {
                int interval = 60 * 1000 / (int)average;

                if (Math.Abs(interval - timer0.Interval) > threshold)
                {
                    timer0.Interval = interval;

                    if (timer1.Enabled && timer2.Enabled && timer3.Enabled && !timer0.Enabled)
                    {
                        timer0.Enabled = true;
                    }
                }
            }
        }

        public void Push1(int bpm)
        {
            dataStatistics1.Push(bpm);

            SetInterval((int) dataStatistics1.Mean, timer1);

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

        private static void SetInterval(int mean, Timer timer)
        {
            int interval = 60 * 1000 / mean;

            if (mean > 0 && Math.Abs(interval - timer.Interval) > threshold)
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
