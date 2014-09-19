using System;
using System.Collections.Concurrent;

namespace PushSharp.Core
{
    public class ConcurrentNotificationQueue:INotificationQueue
    {
        private readonly BlockingCollection<INotification> _queue = new BlockingCollection<INotification>(); 
        public void Enqueue(INotification notification)
        {
            _queue.Add(notification);
        }

        public void EnqueueAtStart(INotification notification)
        {
          throw new NotSupportedException("this queue doesn't support adding to the front");
        }

        public void EnqueueAt(INotification notification, int index = 0)
        {
            throw new NotSupportedException("this queue doesn't support adding to the middle");
        }

        public INotification Dequeue(bool block = false)
        {
            if (block)
                return _queue.Take();
            INotification notification;
            _queue.TryTake(out notification, 0);
            return notification;
        }

        public int Count { get { return _queue.Count; } }
    }
}