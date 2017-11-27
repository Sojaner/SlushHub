using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Bespoke.Common.Osc;

namespace SlushHub
{
    internal class Broadcaster
    {
        private readonly Processor processor;

        private readonly IPAddress[] forwardingIPAddresses;

        private readonly Timer timer;

        public Broadcaster(int interval, Processor processor, IPAddress[] forwardingIPAddresses)
        {
            this.processor = processor;

            this.forwardingIPAddresses = forwardingIPAddresses;

            timer = new Timer(interval);

            timer.Elapsed += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            foreach (IPAddress address in forwardingIPAddresses)
            {
                Task.Run(() =>
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(address, 9999);

                    foreach (string oscAddress in processor.Addresses)
                    {
                        int value = processor[oscAddress];

                        OscMessage message = new OscMessage(ipEndPoint, oscAddress);

                        message.Append(value);

                        message.Send(ipEndPoint);
                    }
                });
            }
        }

        public void Start()
        {
            timer.Start();
        }
    }
}
