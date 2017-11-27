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

        //gsr
        //pulse
        //bpm

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            foreach (IPAddress address in forwardingIPAddresses)
            {
                Task.Run(() =>
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(address, 9999);
                    //
                    string oscAddress = "/emotion";
                    
                    double value = processor[oscAddress];

                    OscMessage message = new OscMessage(ipEndPoint, oscAddress);

                    message.Append(value);

                    message.Send(ipEndPoint);
                    //
                    oscAddress = "/reason";

                    value = processor[oscAddress];

                    message = new OscMessage(ipEndPoint, oscAddress);

                    message.Append(value);

                    message.Send(ipEndPoint);
                    //
                    oscAddress = "/reason";

                    value = processor[oscAddress];

                    message = new OscMessage(ipEndPoint, oscAddress);

                    message.Append(value);

                    message.Send(ipEndPoint);
                    //
                    oscAddress = "/reason";

                    value = processor[oscAddress];

                    message = new OscMessage(ipEndPoint, oscAddress);

                    message.Append(value);

                    message.Send(ipEndPoint);
                    //
                    oscAddress = "/reason";

                    value = processor[oscAddress];

                    message = new OscMessage(ipEndPoint, oscAddress);

                    message.Append(value);

                    message.Send(ipEndPoint);
                });
            }
        }

        public void Start()
        {
            timer.Start();
        }
    }
}
