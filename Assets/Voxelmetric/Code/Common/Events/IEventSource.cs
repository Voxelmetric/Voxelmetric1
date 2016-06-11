namespace Voxelmetric.Code.Common.Events
{
    public interface IEventSource<TEvent>
    {
        //! Unsubscribes all listeners
        void Clear();
        //! Registers listener to receive a certain kind of notifications from source
        bool Subscribe(IEventListener<TEvent> listener, TEvent evt, bool register);
        //! Notifies subscribers about something (implementation specific)	
        void NotifyAll(TEvent evt);
        //! Notifies one specific subscriber
        void NotifyOne(IEventListener<TEvent> listener, TEvent evt);
    }
}
