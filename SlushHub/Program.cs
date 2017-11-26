using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SlushHub
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!args.Any(argument => Regex.IsMatch(argument, @"--listening-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")) || !args.Any(argument => Regex.IsMatch(argument, @"--forwarding-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")) || !args.Any(argument => Regex.IsMatch(argument, @"--listening-port:\d{1,3}")) || !args.Any(argument => Regex.IsMatch(argument, @"--forwarding-port:\d{1,3}")))
            {
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --listening-ip:xxx.xxx.xxx.xxx --listening-port:xxxx --forwarding-ip:xxx.xxx.xxx.xxx --forwarding-port:xxxx");
            }

            IPAddress listeningIP;

            IPAddress forwardingIP;

            int listeningPort;

            int forwardingPort;

            try
            {
                listeningIP = IPAddress.Parse(args.Single(argument => Regex.IsMatch(argument, @"--listening-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Listening IP Address'.");
            }

            try
            {
                forwardingIP = IPAddress.Parse(args.Single(argument => Regex.IsMatch(argument, @"--forwarding-ip:\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding IP Address'.");
            }

            try
            {
                listeningPort = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--listening-port:\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Listening Port'.");
            }

            try
            {
                forwardingPort = int.Parse(args.Single(argument => Regex.IsMatch(argument, @"--forwarding-port:\d{1,3}")).Split(":")[1].Trim());
            }
            catch
            {
                Console.WriteLine("Error: Problem with 'Forwarding Port'.");
            }
        }
    }
}
