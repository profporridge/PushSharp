namespace PushSharp.Core
{
    public interface INotificationQueue
    {
        void Enqueue(INotification notification);
        void EnqueueAtStart(INotification notification);
        void EnqueueAt(INotification notification, int index = 0);
        INotification Dequeue(bool block = false);
        int Count { get; }
    }
}