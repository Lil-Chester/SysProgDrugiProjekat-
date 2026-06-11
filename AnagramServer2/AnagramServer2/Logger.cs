using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace AnagramServer2
{
    public static class Logger
    {
        private static readonly BlockingCollection<string> logQueue = new BlockingCollection<string>();
        private static Thread loggingThread;

        public static void Init()
        {
            loggingThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "LoggerThread"
            };
            loggingThread.Start();
        }

        public static void Log(string message)
        {
            if (!logQueue.IsAddingCompleted)
            {
                logQueue.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }
        }

        // Nova metoda za gašenje loggera bez gubitka poslednjih logova
        public static void Stop()
        {
            logQueue.CompleteAdding();

            // Blokira glavnu nit dok logger ne upiše poslednju poruku iz reda
            loggingThread?.Join();
        }

        private static void ProcessLogQueue()
        {
            foreach (var message in logQueue.GetConsumingEnumerable())
            {
                try
                {
                    File.AppendAllText("logs.txt", message + Environment.NewLine);
                }
                catch { /* Ignorišemo greške kod logovanja */ }
            }
        }
    }
}