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
            // Dodajemo samo ako red nije u procesu gašenja
            if (!queue.IsAddingCompleted)
            {
                queue.Add(request);
            }
        }

        // Nova metoda: vraća false kada se server gasi i red ostane prazan
        public bool TryDequeue(out RequestData request)
        {
            try
            {
                // Uzima element, blokira dok se ne pojavi, 
                // ali baca izuzetak ako se pozove CompleteAdding()
                return queue.TryTake(out request, Timeout.Infinite);
            }
            catch (InvalidOperationException)
            {
                request = null;
                return false;
            }
        }

        // Zatvara red za nove zahteve
        public void StopAccepting()
        {
            queue.CompleteAdding();
        }
    }
}