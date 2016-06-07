namespace Voxelmetric.Code.Common.Events
{
    public interface IEventSource<TEvent>
    {
        //! Unsubscribes all listeners
        void Clear();
        //! Registers caller for receiving notification from parent
        bool Subscribe(IEventListener<TEvent> listener, bool register);
        //! Notifies subscribers about something (implementation specific)	
        void NotifyAll(TEvent evt);
        //! Notifies one specific subscriber
        void NotifyOne(IEventListener<TEvent> listener, TEvent evt);
    }
}
