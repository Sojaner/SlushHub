using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Bespoke.Common.Osc;

namespace SlushHub
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            OscPacket.UdpClient = new UdpClient();

            ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);

            if (!args.Any(argument => Regex.IsMatch(argument, @"--fis:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?:,\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})*")))
            {
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --fis:xxx.xxx.xxx.xxx[,xxx.xxx.xxx.xxx...] -i:xxx");

                Console.WriteLine($"Example: {Assembly.GetExecutingAssembly().GetName().Name} --fis:192.168.1.30,192.168.1.42 -i:xxx");

                return;
            }

            IPAddress[] forwardingIPs;

            int interval = 250;

            Processor processor = new Processor(100);

            try
            {
                interval = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--i:\d{1,5}")));
            }
            catch
            {
                //
            }

            try
            {
                forwardingIPs = args.Single(argument => Regex.IsMatch(argument, @"--fis:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?:,\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})*")).Split(":")[1].Trim().Split(",").Select(IPAddress.Parse).ToArray();
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding IP Addresses'.");

                return;
            }

            OscServer oscServer = new OscServer(TransportType.Udp, IPAddress.Any, 8888);

            oscServer.PacketReceived += (sender, eventArgs) =>
            {
                processor.InsertOscPacket(eventArgs.Packet);
            };

            try
            {
                oscServer.Start();
            }
            catch
            {
                Console.WriteLine("Error: Couldn't start the OSC server, probably because of a problem in 'Listening IP Address'.");

                return;
            }

            UdpServer<int> emotionListener = new UdpServer<int>(8877, manualResetEventSlim);

            emotionListener.DataReceived += (sender, data) =>
            {
                processor.InsertEmotion(data);
            };

            emotionListener.Start();

            UdpServer<int> reasonListener = new UdpServer<int>(8899, manualResetEventSlim);

            reasonListener.DataReceived += (sender, data) =>
            {
                processor.InsertReason(data);
            };

            reasonListener.Start();

            Broadcaster broadcaster = new Broadcaster(interval, processor, forwardingIPs);

            broadcaster.Start();

            manualResetEventSlim.Wait();
        }
    }
}
