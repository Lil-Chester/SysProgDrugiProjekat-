using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AnagramServer2
{
    public class RequestQueue
    {
        private BlockingCollection<RequestData> queue = new BlockingCollection<RequestData>();

        public void Enqueue(RequestData request)
        {
            if (!queue.IsAddingCompleted)
            {
                queue.Add(request);
            }
        }

        public bool TryDequeue(out RequestData request)
        {
            try
            {
                return queue.TryTake(out request, Timeout.Infinite);
            }
            catch (InvalidOperationException)
            {
                request = null;
                return false;
            }
        }

        public void StopAccepting()
        {
            queue.CompleteAdding();
        }
    }
}