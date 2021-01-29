using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Prototype1.Foundation.Logging;

namespace Prototype1.Foundation
{
    public abstract class ProducerConsumerQueue<T>
    {
        private readonly IExceptionLogger _logger;
        private readonly BlockingCollection<T> _items = new BlockingCollection<T>();
        private readonly OrderablePartitioner<T> _itemsToProcess;
        private readonly ParallelOptions _parallelOptions;

        protected ProducerConsumerQueue(IExceptionLogger logger, int maxDegreesOfParallelism)
        {
            _logger = logger;
            _parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesOfParallelism };

            _itemsToProcess = Partitioner.Create(_items.GetConsumingEnumerable(), EnumerablePartitionerOptions.NoBuffering);

            Task.Factory.StartNew(() =>
                                  Parallel.ForEach(_itemsToProcess, _parallelOptions,
                                                   x =>
                                                   {
                                                       try
                                                       {
                                                           ProcessItem(x);
                                                       }
                                                       catch (Exception ex)
                                                       {
                                                           _logger.LogException(ex);
                                                       }
                                                   }));
        }

        public abstract void ProcessItem(T item);


        public virtual void AddItem(T item)
        {
            _items.Add(item);
        }
    }
}
