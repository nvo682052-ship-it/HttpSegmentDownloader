using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader.Threading
{
    /// <summary>
    /// Custom thread pool manager that optimizes for download scenarios.
    /// Provides better control over thread count, queue handling, and work distribution.
    /// </summary>
    public class ThreadPoolManager : IDisposable
    {
        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly ConcurrentQueue<WorkItem> _workQueue;
        private readonly Semaphore _queueSemaphore;
        private readonly CancellationTokenSource _shutdownToken;
        private readonly Task[] _workerThreads;
        private volatile int _activeThreadCount;
        private volatile bool _disposed;

        public int MinThreads => _minThreads;
        public int MaxThreads => _maxThreads;
        public int ActiveThreadCount => _activeThreadCount;
        public int QueuedWorkCount => _workQueue.Count;

        private class WorkItem
        {
            public Func<Task> TaskFactory { get; set; }
            public TaskCompletionSource<object> CompletionSource { get; set; }
            public CancellationToken CancellationToken { get; set; }
        }

        /// <summary>
        /// Initializes a new thread pool manager with specified thread count.
        /// </summary>
        public ThreadPoolManager(int minThreads, int maxThreads)
        {
            if (minThreads < 1)
                throw new ArgumentException("Minimum threads must be at least 1", nameof(minThreads));
            if (maxThreads < minThreads)
                throw new ArgumentException("Maximum threads cannot be less than minimum threads", nameof(maxThreads));

            _minThreads = minThreads;
            _maxThreads = maxThreads;
            _workQueue = new ConcurrentQueue<WorkItem>();
            _queueSemaphore = new Semaphore(0, int.MaxValue);
            _shutdownToken = new CancellationTokenSource();
            _activeThreadCount = 0;

            // Start minimum number of worker threads
            _workerThreads = new Task[_minThreads];
            for (int i = 0; i < _minThreads; i++)
            {
                _workerThreads[i] = Task.Run(() => WorkerLoop(), _shutdownToken.Token);
            }
        }

        /// <summary>
        /// Creates a thread pool manager optimized for HTTP downloads (typically 4-8 threads).
        /// </summary>
        public static ThreadPoolManager CreateForDownloads()
        {
            int processorCount = Environment.ProcessorCount;
            int optimalThreadCount = Math.Max(4, processorCount / 2);
            return new ThreadPoolManager(optimalThreadCount, optimalThreadCount * 2);
        }

        /// <summary>
        /// Queues a task for execution on the thread pool.
        /// </summary>
        public Task QueueWorkAsync(Func<Task> taskFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var tcs = new TaskCompletionSource<object>();
            var workItem = new WorkItem
            {
                TaskFactory = taskFactory,
                CompletionSource = tcs,
                CancellationToken = cancellationToken
            };

            _workQueue.Enqueue(workItem);
            _queueSemaphore.Release();

            return tcs.Task;
        }

        /// <summary>
        /// Queues a work item with a return value.
        /// </summary>
        public Task<T> QueueWorkAsync<T>(Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var tcs = new TaskCompletionSource<T>();

            async Task Wrapper()
            {
                try
                {
                    var result = await taskFactory();
                    tcs.SetResult(result);
                }
                catch (OperationCanceledException ex)
                {
                    tcs.SetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            var workItem = new WorkItem
            {
                TaskFactory = Wrapper,
                CompletionSource = new TaskCompletionSource<object>(),
                CancellationToken = cancellationToken
            };

            _workQueue.Enqueue(workItem);
            _queueSemaphore.Release();

            return tcs.Task;
        }

        /// <summary>
        /// Main worker loop for thread pool threads.
        /// </summary>
        private void WorkerLoop()
        {
            try
            {
                while (!_shutdownToken.Token.IsCancellationRequested)
                {
                    // Wait for work with timeout to allow checking shutdown
                    if (!_queueSemaphore.WaitOne(1000))
                        continue;

                    if (_workQueue.TryDequeue(out var workItem))
                    {
                        ProcessWorkItem(workItem);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected during shutdown
            }
            finally
            {
                Interlocked.Decrement(ref _activeThreadCount);
            }
        }

        /// <summary>
        /// Processes a single work item.
        /// </summary>
        private void ProcessWorkItem(WorkItem workItem)
        {
            try
            {
                Interlocked.Increment(ref _activeThreadCount);

                // Check if cancellation was requested
                if (workItem.CancellationToken.IsCancellationRequested)
                {
                    workItem.CompletionSource.TrySetCanceled();
                    return;
                }

                // Execute the work
                var task = workItem.TaskFactory();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        workItem.CompletionSource.TrySetException(t.Exception);
                    else if (t.IsCanceled)
                        workItem.CompletionSource.TrySetCanceled();
                    else
                        workItem.CompletionSource.TrySetResult(null);
                });
            }
            catch (Exception ex)
            {
                workItem.CompletionSource.TrySetException(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _activeThreadCount);
            }
        }

        /// <summary>
        /// Gets statistics about the thread pool.
        /// </summary>
        public ThreadPoolStatistics GetStatistics()
        {
            return new ThreadPoolStatistics
            {
                MinThreads = _minThreads,
                MaxThreads = _maxThreads,
                ActiveThreadCount = _activeThreadCount,
                QueuedWorkCount = _workQueue.Count,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        /// <summary>
        /// Waits for all queued work to complete.
        /// </summary>
        public async Task WaitForCompletionAsync(TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(30);

            var deadline = DateTime.UtcNow + timeout;

            while (_workQueue.Count > 0 || _activeThreadCount > 0)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Thread pool work did not complete within timeout");

                await Task.Delay(10);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _shutdownToken.Cancel();

            // Wait for all threads to complete (with timeout)
            if (!Task.WaitAll(_workerThreads, TimeSpan.FromSeconds(10)))
            {
                // Log warning: threads did not complete in time
            }

            _queueSemaphore?.Dispose();
            _shutdownToken?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Statistics about thread pool performance and state.
    /// </summary>
    public class ThreadPoolStatistics
    {
        public int MinThreads { get; set; }
        public int MaxThreads { get; set; }
        public int ActiveThreadCount { get; set; }
        public int QueuedWorkCount { get; set; }
        public int ProcessorCount { get; set; }
        public double ThreadUtilization => ProcessorCount > 0 ? (double)ActiveThreadCount / ProcessorCount : 0;

        public override string ToString()
        {
            return $"ThreadPool[Min={MinThreads}, Max={MaxThreads}, Active={ActiveThreadCount}, Queued={QueuedWorkCount}, Util={ThreadUtilization:P0}]";
        }
    }
}
