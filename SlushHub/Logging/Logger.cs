using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlushHub.Logging
{
    public class Logger
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());

        public static Logger Instance => lazy.Value;

        private readonly FileStream fileStream;

        private readonly ConcurrentQueue<string> concurrentQueue;

        public Logger()
        {
            fileStream = File.OpenWrite(Path.Combine(Environment.CurrentDirectory, "log.log"));

            concurrentQueue = new ConcurrentQueue<string>();
        }

        public void LogLines(params string[] lines)
        {
            foreach (string line in lines)
            {
                concurrentQueue.Enqueue(line);
            }

            WriteOut();
        }

        private void WriteOut()
        {
            if (started && !taskRunning)
            {
                taskRunning = true;

                Task.Run(() =>
                {
                    while (!concurrentQueue.IsEmpty)
                    {
                        concurrentQueue.TryDequeue(out string result);

                        if (!string.IsNullOrEmpty(result))
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes($"{result}{Environment.NewLine}");

                            fileStream.Write(bytes, 0, bytes.Length);

                            fileStream.Flush(true);
                        }
                    }

                    taskRunning = false;
                });
            }
        }

        private volatile bool taskRunning;

        private volatile bool started;

        public void Start()
        {
            started = true;

            WriteOut();
        }

        public void Log(string log)
        {
            LogLines(log);
        }

        public void Flush()
        {
            if (!concurrentQueue.IsEmpty)
            {
                WriteOut();

                while (!concurrentQueue.IsEmpty)
                {
                    Thread.Sleep(125);
                }
            }
        }
    }
}
