using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Bespoke.Common.Net;
using Bespoke.Common.Osc;

namespace SlushHub
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);

            if (!args.Any(argument => Regex.IsMatch(argument, @"^--listening-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")) || !args.Any(argument => Regex.IsMatch(argument, @"^--forwarding-ips:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?:,\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})*$")) || !args.Any(argument => Regex.IsMatch(argument, @"^--listening-port:\d{1,3}$")) || !args.Any(argument => Regex.IsMatch(argument, @"^--forwarding-port:\d{1,3}$")))
            {
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --listening-ip:xxx.xxx.xxx.xxx --listening-port:xxxx --forwarding-ips:xxx.xxx.xxx.xxx[,xxx.xxx.xxx.xxx...] --forwarding-port:xxxx");

                Console.WriteLine($"Example: {Assembly.GetExecutingAssembly().GetName().Name} --listening-ip:192.168.1.20 --listening-port:8888 --forwarding-ips:192.168.1.30,192.168.1.42 --forwarding-port:8890");
            }

            IPAddress listeningIP;

            IPAddress[] forwardingIPs;

            int listeningPort;

            int forwardingPort;

            try
            {
                listeningIP = IPAddress.Parse(args.Single(argument => Regex.IsMatch(argument, @"^--listening-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")).Split(":")[1].Trim());

                if (!IPServer.GetLocalIPAddresses().Contains(listeningIP))
                {
                    Console.WriteLine("Warning: 'Listening IP Address' does not belong to this device.");
                }
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Listening IP Address'.");

                return;
            }

            try
            {
                forwardingIPs = args.Single(argument => Regex.IsMatch(argument, @"^--forwarding-ips:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?:,\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})*$")).Split(":")[1].Trim().Split(",").Select(IPAddress.Parse).ToArray();
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding IP Addresses'.");

                return;
            }

            try
            {
                listeningPort = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--listening-port:\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Listening Port'.");

                return;
            }

            try
            {
                forwardingPort = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--forwarding-port:\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding Port'.");

                return;
            }

            OscClient[] clients = new OscClient[forwardingIPs.Length];

            for (int i = 0; i < forwardingIPs.Length; i++)
            {
                clients[i] = new OscClient(forwardingIPs[i], forwardingPort);

                try
                {
                    clients[i].Connect();
                }
                catch
                {
                    Console.WriteLine($"Warning: Couldn't connect to {forwardingIPs[i]}:{forwardingPort}.");
                }
            }

            clients = clients.Where(client => client.IsConnected).ToArray();

            if (clients.Length == 0)
            {
                Console.WriteLine("Warning: Couldn't connect to any of the 'Forwarding IP Addresses'.");
            }

            OscServer oscServer = new OscServer(listeningIP, listeningPort);

            oscServer.RegisterMethod("/");

            oscServer.PacketReceived += (sender, eventArgs) =>
            {
                foreach (OscClient client in clients)
                {
                    client.Send(eventArgs.Packet);
                }
            };

            oscServer.Start();

            manualResetEventSlim.Wait();
        }
    }
}
