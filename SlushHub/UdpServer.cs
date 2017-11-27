using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SlushHub
{
    internal class UdpServer<T> : IDisposable
    {
        private IPEndPoint ipEndPoint;

        private readonly ManualResetEventSlim manualResetEventSlim;

        public event EventHandler<T> DataReceived;

        private readonly UdpClient listener;

        private bool running;

        public UdpServer(int port, ManualResetEventSlim manualResetEventSlim)
        {
            ipEndPoint = new IPEndPoint(IPAddress.Any, port);

            this.manualResetEventSlim = manualResetEventSlim;

            listener = new UdpClient(port);
        }

        public void Start()
        {
            Task.Run(() =>
            {
                if (!running)
                {
                    running = true;

                    while (running && !manualResetEventSlim.IsSet)
                    {
                        byte[] data = listener.Receive(ref ipEndPoint);

                        Array.Reverse(data);

                        Type type = typeof(T);

                        if (type == typeof(int))
                        {
                            T value = (T)Convert.ChangeType(BitConverter.ToInt32(data, 0), typeof(T));

                            OnDataReceived(value);
                        }
                        else if(type == typeof(float))
                        {
                            T value = (T)Convert.ChangeType(BitConverter.ToSingle(data, 0), typeof(T));

                            OnDataReceived(value);
                        }
                    }
                }
            });
        }

        public void Stop()
        {
            running = false;
        }

        protected virtual void OnDataReceived(T e)
        {
            DataReceived?.Invoke(this, e);
        }

        public void Dispose()
        {
            listener?.Close();

            listener?.Dispose();
        }
    }
}
