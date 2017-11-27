using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bespoke.Common.Net;
using Bespoke.Common.Osc;
using SlushHub.Logging;

namespace SlushHub
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            OscPacket.UdpClient = new UdpClient();

            Task.Run(() =>
            {
                while (true)
                {
                    OscMessage message = new OscMessage(new IPEndPoint(IPAddress.Parse("192.168.0.18"), 8888), "/");

                    message.Append(DateTime.UtcNow.ToString("T"));

                    message.Send(new IPEndPoint(IPAddress.Parse("192.168.0.18"), 8888));

                    Thread.Sleep(500);
                }
            });

            ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);

            if (!args.Any(argument => Regex.IsMatch(argument, @"--listening-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")) || !args.Any(argument => Regex.IsMatch(argument, @"--forwarding-ips:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?:,\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})*")) || !args.Any(argument => Regex.IsMatch(argument, @"--listening-port:\d{1,3}")) || !args.Any(argument => Regex.IsMatch(argument, @"--forwarding-port:\d{1,3}")))
            {
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --listening-ip:xxx.xxx.xxx.xxx --listening-port:xxxx --forwarding-ips:xxx.xxx.xxx.xxx[,xxx.xxx.xxx.xxx...] --forwarding-port:xxxx");

                Console.WriteLine($"Example: {Assembly.GetExecutingAssembly().GetName().Name} --listening-ip:192.168.1.20 --listening-port:8888 --forwarding-ips:192.168.1.30,192.168.1.42 --forwarding-port:8890");

                return;
            }

            IPAddress listeningIP;

            IPAddress[] forwardingIPs;

            int listeningPort;

            int forwardingPort;

            bool log = args.Any(argument => argument.Equals("--log"));

            if (log)
            {
                Logger.Instance.Start();
            }

            try
            {
                listeningIP = IPAddress.Parse(args.Single(argument => Regex.IsMatch(argument, @"--listening-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Split(":")[1].Trim());

                if (!IPServer.GetLocalIPAddresses().Contains(listeningIP))
                {
                    Console.WriteLine("Warning: 'Listening IP Address' does not belong to this device.");

                    if (log)
                    {
                        Logger.Instance.Log("Warning: 'Listening IP Address' does not belong to this device.");
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Listening IP Address'.");

                if (log)
                {
                    Logger.Instance.Log("Error: Problem with 'Listening IP Address'.");

                    Logger.Instance.Flush();
                }

                return;
            }

            try
            {
                forwardingIPs = args.Single(argument => Regex.IsMatch(argument, @"--forwarding-ips:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?:,\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})*")).Split(":")[1].Trim().Split(",").Select(IPAddress.Parse).ToArray();
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding IP Addresses'.");

                if (log)
                {
                    Logger.Instance.Log("Error: Problem with 'Forwarding IP Addresses'.");

                    Logger.Instance.Flush();
                }

                return;
            }

            try
            {
                listeningPort = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--listening-port:\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Listening Port'.");

                if (log)
                {
                    Logger.Instance.Log("Error: Problem with 'Listening Port'.");

                    Logger.Instance.Flush();
                }

                return;
            }

            try
            {
                forwardingPort = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--forwarding-port:\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding Port'.");

                if (log)
                {
                    Logger.Instance.Log("Error: Problem with 'Forwarding Port'.");

                    Logger.Instance.Flush();
                }

                return;
            }

            OscServer oscServer = new OscServer(TransportType.Udp, listeningIP, listeningPort);

            oscServer.RegisterMethod("/");

            oscServer.PacketReceived += (sender, eventArgs) =>
            {
                if (log)
                {
                    Logger.Instance.Log(eventArgs.Packet.At<string>(0));
                }

                foreach (IPAddress forwardingIP in forwardingIPs)
                {
                    Task.Run(() =>
                    {
                        IPEndPoint ipEndPoint = new IPEndPoint(forwardingIP, forwardingPort);

                        OscMessage message = new OscMessage(ipEndPoint, "/");

                        message.Append("Hello!");

                        message.Send(ipEndPoint);
                    });
                }
            };

            try
            {
                oscServer.Start();
            }
            catch
            {
                Console.WriteLine("Error: Couldn't start the OSC server, probably because of a problem in 'Listening IP Address'.");

                if (log)
                {
                    Logger.Instance.Log("Error: Couldn't start the OSC server, probably because of a problem in 'Listening IP Address'.");

                    Logger.Instance.Flush();
                }

                return;
            }

            manualResetEventSlim.Wait();

            if (log)
            {
                Logger.Instance.Flush();
            }
        }
    }
}
