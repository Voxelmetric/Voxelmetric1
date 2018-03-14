namespace Voxelmetric.Code.Common.Events
{
    public interface IEventSource<TEvent>
    {
        //! Unsubscribes all listeners
        void Clear();
        //! Registers a listener to receive a certain kind of notifications from the source
        bool Register(IEventListener<TEvent> listener);
        //! Unregisters a listener from receiving a certain kind of notifications from the source
        bool Unregister(IEventListener<TEvent> listener);
        //! Notifies subscribers about something (implementation specific)	
        void NotifyAll(TEvent evt);
    }
}
