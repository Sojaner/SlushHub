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
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --fis:xxx.xxx.xxx.xxx[,xxx.xxx.xxx.xxx...] --ws:xxxx --oi:xxxx");

                Console.WriteLine($"Example: {Assembly.GetExecutingAssembly().GetName().Name} --fis:192.168.1.30,192.168.1.42 --ws:100 --oi:125");

                return;
            }

            IPAddress[] forwardingIPs;

            int interval = 125;

            int windowSize = 50;

            try
            {
                interval = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--io:\d{1,5}")).Split(":")[1].Trim());
            }
            catch
            {
                //
            }

            try
            {
                windowSize = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--ws:\d{1,5}")).Split(":")[1].Trim());
            }
            catch
            {
                //
            }

            Processor processor = new Processor(windowSize);

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
                try
                {
                    processor.InsertOscPacket(eventArgs.Packet);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OSC Packet Error: {e.Message}");
                }
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
                try
                {
                    processor.InsertEmotion(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Emotion Error: {e.Message}");
                }
            };

            emotionListener.Start();

            UdpServer<int> reasonListener = new UdpServer<int>(8899, manualResetEventSlim);

            reasonListener.DataReceived += (sender, data) =>
            {
                try
                {
                    processor.InsertReason(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Reason Error: {e.Message}");
                }
            };

            reasonListener.Start();

            Broadcaster broadcaster = new Broadcaster(interval, processor, forwardingIPs);

            broadcaster.Start();

            manualResetEventSlim.Wait();
        }
    }
}
