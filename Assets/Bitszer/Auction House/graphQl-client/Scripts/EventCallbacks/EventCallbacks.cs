using System;

namespace GraphQlClient.EventCallbacks
{
    /// <summary>
    /// Fired when an HTTP request begins.
    /// </summary>
    public class OnRequestBegin
    {
        public delegate void OnRequestBeginDelegate();
        public static event OnRequestBeginDelegate OnRequestBeginEvent;

        public void FireEvent()
        {
            OnRequestBeginEvent?.Invoke();
        }
    }

    /// <summary>
    /// Fired when an HTTP request ends (success or failure).
    /// </summary>
    public class OnRequestEnded
    {
        public delegate void OnRequestEndedDelegate(string data);
        public delegate void OnRequestEndedErrorDelegate(Exception exception);

        public static event OnRequestEndedDelegate OnRequestEndedEvent;
        public static event OnRequestEndedErrorDelegate OnRequestEndedErrorEvent;

        private string _data;
        private Exception _exception;
        private bool _isError;

        public OnRequestEnded(string data)
        {
            _data = data;
            _isError = false;
        }

        public OnRequestEnded(Exception exception)
        {
            _exception = exception;
            _isError = true;
        }

        public void FireEvent()
        {
            if (_isError)
            {
                OnRequestEndedErrorEvent?.Invoke(_exception);
            }
            else
            {
                OnRequestEndedEvent?.Invoke(_data);
            }
        }
    }

    /// <summary>
    /// Fired when a WebSocket subscription handshake completes.
    /// </summary>
    public class OnSubscriptionHandshakeComplete
    {
        public delegate void OnSubscriptionHandshakeCompleteDelegate();
        public static event OnSubscriptionHandshakeCompleteDelegate OnSubscriptionHandshakeCompleteEvent;

        public void FireEvent()
        {
            OnSubscriptionHandshakeCompleteEvent?.Invoke();
        }
    }

    /// <summary>
    /// Fired when subscription data is received via WebSocket.
    /// </summary>
    public class OnSubscriptionDataReceived
    {
        public delegate void OnSubscriptionDataReceivedDelegate(string data);
        public static event OnSubscriptionDataReceivedDelegate OnSubscriptionDataReceivedEvent;

        private string _data;

        public OnSubscriptionDataReceived(string data)
        {
            _data = data;
        }

        public void FireEvent()
        {
            OnSubscriptionDataReceivedEvent?.Invoke(_data);
        }
    }

    /// <summary>
    /// Fired when a WebSocket subscription is canceled.
    /// </summary>
    public class OnSubscriptionCanceled
    {
        public delegate void OnSubscriptionCanceledDelegate();
        public static event OnSubscriptionCanceledDelegate OnSubscriptionCanceledEvent;

        public void FireEvent()
        {
            OnSubscriptionCanceledEvent?.Invoke();
        }
    }
}
