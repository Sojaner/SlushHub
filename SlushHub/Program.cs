using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --fis:xxx.xxx.xxx.xxx[,xxx.xxx.xxx.xxx...] --ws:xxxx --oi:xxxx --hrt:xxxx --minhr:xxxx --maxhr:xxxx --log");

                Console.WriteLine($"Example: {Assembly.GetExecutingAssembly().GetName().Name} --fis:192.168.1.30,192.168.1.42 --ws:100 --oi:125 --hrt:25 --minhr:40 --maxhr:150 --log");

                return;
            }

            IPAddress[] forwardingIPs;

            int interval = 125;

            int windowSize = 50;

            bool log = false;

            int heartRateThreshold = 25;

            int minimumHeartRate = 40;

            int maximumHeartRate = 150;

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

            try
            {
                heartRateThreshold = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--hrt:\d{1,5}")).Split(":")[1].Trim());
            }
            catch
            {
                //
            }

            try
            {
                minimumHeartRate = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--minhr:\d{1,5}")).Split(":")[1].Trim());
            }
            catch
            {
                //
            }

            try
            {
                maximumHeartRate = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--maxhr:\d{1,5}")).Split(":")[1].Trim());
            }
            catch
            {
                //
            }

            try
            {
                log = args.Any(argument => Regex.IsMatch(argument, @"--log"));
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

            Pulser pulser = new Pulser(windowSize, forwardingIPs, heartRateThreshold, maximumHeartRate, minimumHeartRate);

            OscServer oscServer = new OscServer(TransportType.Udp, IPAddress.Any, 8888);

            oscServer.PacketReceived += (sender, eventArgs) =>
            {
                try
                {
                    processor.InsertOscPacket(eventArgs.Packet);

                    Task.Run(() =>
                    {
                        if (eventArgs.Packet.Address.StartsWith("/person") && eventArgs.Packet.Address.EndsWith("/bpm"))
                        {
                            int person = int.Parse(eventArgs.Packet.Address.Split("/person")[1].Split("/")[0]);

                            int bpm = eventArgs.Packet.At<int>(0);

                            switch (person)
                            {
                                case 1:
                                    {
                                        pulser.Push1(bpm);

                                        break;
                                    }
                                case 2:
                                    {
                                        pulser.Push2(bpm);

                                        break;
                                    }
                                case 3:
                                    {
                                        pulser.Push3(bpm);

                                        break;
                                    }
                            }
                        }
                    });
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

            UdpServer<int> emotionListener = new UdpServer<int>(8877, manualResetEventSlim, "Emotion");

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

            UdpServer<int> reasonListener = new UdpServer<int>(8899, manualResetEventSlim, "Reason");

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

            if (log)
            {
                Task.Run(() =>
                {
                    while (!manualResetEventSlim.IsSet)
                    {
                        Console.Clear();

                        Console.BackgroundColor = ConsoleColor.DarkGray;

                        Console.ForegroundColor = ConsoleColor.DarkRed;

                        pulser.Log();

                        Console.WriteLine();

                        Console.ForegroundColor = ConsoleColor.DarkBlue;

                        emotionListener.Log();

                        reasonListener.Log();

                        Console.WriteLine();

                        Console.ForegroundColor = ConsoleColor.White;

                        processor.Log();

                        Thread.Sleep(125);
                    }
                });
            }

            manualResetEventSlim.Wait();
        }
    }
}
