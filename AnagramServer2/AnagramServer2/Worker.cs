using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AnagramServer2
{
    public class Worker
    {
        private CacheManager cache;
        private readonly string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");

        public Worker(CacheManager cache)
        {
            this.cache = cache;
        }

        public Task ProcessAsync(RequestData request)
        {
            Task<(int StatusCode, string ResponseMessage)> processingTask = ProcessCoreAsync(request);

            return processingTask.ContinueWith(async t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        Logger.Log($"GRESKA (Worker): {t.Exception?.GetBaseException().Message}");
                        await SendResponseAsync(request.Context, 500, "Interna greska servera.");
                    }
                    else
                    {
                        var result = t.Result;
                        await SendResponseAsync(request.Context, result.StatusCode, result.ResponseMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Kritična greška pri slanju odgovora: {ex.Message}");
                    request.Context.Response.Close();
                }
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }

        private async Task<(int StatusCode, string ResponseMessage)> ProcessCoreAsync(RequestData request)
        {
            Logger.Log($"Obrada zahteva započeta: {request.FileName} {request.Word}");

            string filePath = FileSearcher.FindFile(rootFolder, request.FileName);

            if (filePath == null)
            {
                return (404, "Fajl nije pronadjen.");
            }

            string cacheKey = $"{request.FileName}:{request.Word}";

            string result = await cache.GetOrAddAsync(cacheKey, () =>
                AnagramService.CountAnagramsAsync(filePath, request.Word));

            return (200, result);
        }

        private async Task SendResponseAsync(HttpListenerContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            byte[] data = Encoding.UTF8.GetBytes(message);
            context.Response.ContentLength64 = data.Length;

            await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
            context.Response.Close();
        }
    }
}