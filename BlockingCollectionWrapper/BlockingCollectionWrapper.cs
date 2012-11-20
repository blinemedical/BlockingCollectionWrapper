using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BlockingCollectionWrapper
{
public class BlockingCollectionWrapper<T> : IDisposable
{
        
    private Thread _thread;
    private CancellationTokenSource _cancellationTokenSource;
    private BlockingCollection<T> _queue = new BlockingCollection<T>();

    private object _lockObj = new object();

    /// <summary>
    /// Set when the queue consumer is complete and the thread is ending
    /// </summary>
    public Boolean Finished { get; private set; }

    /// <summary>
    /// Dispatched when the queue consumer is complete and the thread is ending
    /// </summary>
    public event EventHandler<BlockingCollectionEventArgs> FinishedEvent;

    /// <summary>
    /// The actual consumer queue that runs in a seperate thread
    /// </summary>
    private void QueueConsumer()
    {
        try
        {
            // block on _queue.GetConsumerEnumerable. When an item is added the _queue lets us consume
            foreach (var item in _queue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                // get a synchronized snapshot of the action
                var consumerAction = QueueConsumingAction;
                
                // execute our registered consuming action
                if (consumerAction != null)
                {
                    consumerAction(item);
                }
            }

            // dispose of the token source
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
            }

            //Log.Debug(this, "Done with queue consumer");

            Finished = true;

            if (FinishedEvent != null)
            {
                FinishedEvent(this, new BlockingCollectionEventArgs());
            }
        }
        catch(OperationCanceledException)
        {
            //Log.Debug(this, "Blocking collection<{0}> cancelled", typeof(T));
        }
        catch (Exception ex)
        {
            //Log.Error(this, ex, "Error consuming from queue of type {0}", typeof(T));
        }
    }

    private Action<T> _queueConsumingAction;

    /// <summary>
    /// Registered queue consumer action
    /// </summary>
    public Action<T> QueueConsumingAction
    {
        get
        {
            lock (_lockObj)
            {
                return _queueConsumingAction;
            }
        }
        set
        {
            lock(_lockObj)
            {
                _queueConsumingAction = value;
            }
        }
    }

    /// <summary>
    /// Add an item for the consumer queue
    /// </summary>
    /// <param name="item"></param>
    public void AddItem(T item)
    {
        if (_queue != null)
        {
            _queue.Add(item);
        }
    }


    /// <summary>
    /// Manually mark the consumer queue to finish consuming what is left but no longer block and then exit
    /// </summary>
    public void CompleteAdding()
    {
        if (_queue != null)
        {
            _queue.CompleteAdding();
        }
    }

    /// <summary>
    /// Start the consumer
    /// </summary>
    public void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _thread = new Thread(QueueConsumer) {Name = "BlockingConsumer"};
        _thread.Start();
    }

    /// <summary>
    /// Force abort the consumer thread
    /// </summary>
    public void ForceAbortConsumer()
    {
        if (_thread != null)
        {
            _thread.Abort();
        }
    }

    /// <summary>
    /// Issue a cancellation
    /// </summary>
    public void Cancel()
    {
        if(_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();    
        }
    }

    protected void Dispose(bool disposing)
    {
        if(disposing)
        {
            if (_queue !=null && !_queue.IsAddingCompleted)
            {
                _queue.CompleteAdding();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}

public class BlockingCollectionEventArgs : EventArgs
{
}
}
