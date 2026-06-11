using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AnagramServer2
{
    public class HttpServer
    {
        private HttpListener listener;
        private RequestQueue queue = new RequestQueue();
        private CacheManager cache = new CacheManager();
        private Worker worker;

        private const int WORKER_COUNT = 4;
        private SemaphoreSlim semaphore = new SemaphoreSlim(WORKER_COUNT);

        public HttpServer(string prefix)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            worker = new Worker(cache);
        }

        public void Start()
        {
            listener.Start();

            Console.WriteLine("Server pokrenut na http://localhost:5050/");
            Logger.Log("Server pokrenut.");

            Thread listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "ListenerThread"
            };
            listenerThread.Start();

            Thread dispatcherThread = new Thread(DispatchLoop)
            {
                IsBackground = true,
                Name = "DispatcherThread"
            };
            dispatcherThread.Start();
        }

        // Nova metoda za bezbedno gašenje
        public void Stop()
        {
            Logger.Log("Inicirano gašenje servera.");

            // Zaustavlja primanje novih HTTP zahteva
            listener.Stop();

            // Obaveštava red da više neće biti dodavanja
            queue.StopAccepting();

            // Dajemo kratko vreme trenutnim taskovima da se završe, a zatim gasimo Logger
            Thread.Sleep(1000);
            Logger.Stop();
        }

        private void ListenLoop()
        {
            try
            {
                while (listener.IsListening)
                {
                    HttpListenerContext context = listener.GetContext();

                    string file = context.Request.QueryString["fajl"];
                    string word = context.Request.QueryString["rec"];

                    if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(word))
                    {
                        context.Response.StatusCode = 400;
                        byte[] error = System.Text.Encoding.UTF8.GetBytes("Neispravni parametri.");
                        context.Response.OutputStream.Write(error, 0, error.Length);
                        context.Response.Close();
                        continue;
                    }

                    RequestData request = new RequestData
                    {
                        Context = context,
                        FileName = file,
                        Word = word
                    };

                    queue.Enqueue(request);
                    Logger.Log($"Primljen zahtev: {file} {word} (Smešten u red)");
                }
            }
            catch (HttpListenerException)
            {
                // Ovaj izuzetak se očekivano okida kada pozovemo listener.Stop()
                Logger.Log("Listener je uspešno zaustavljen.");
            }
        }

        private void DispatchLoop()
        {
            // TryDequeue vraća true dokle god ima elemenata ili dok red nije zvanično zatvoren
            while (queue.TryDequeue(out RequestData request))
            {
                semaphore.Wait();

                Task.Run(async () =>
                {
                    try
                    {
                        await worker.ProcessAsync(request);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }

            Logger.Log("Dispečer je završio sa radom.");
        }
    }
}