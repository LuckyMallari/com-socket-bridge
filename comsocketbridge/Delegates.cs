using System;

namespace ComSocketBridge
{
    public delegate void LogDelegate(string message);
    public delegate void TcpOnListeningStartedHandler(object sender, MessengerEventArgs args);
    public delegate void TcpOnListeningFailHandler(object sender, MessengerEventArgs args);
    public delegate void SerialDataReceivedDelegate(object sender, MessengerEventArgs args);
    public delegate void OnReceivedFromClientDelegate(object sender, MessengerEventArgs args);
    public delegate void ComSocketBridgeStartedHandler(object sender, MessengerEventArgs args);
    public delegate void ComSocketBridgeOnBeforeStartHandler(object sender, MessengerEventArgs args);
    public delegate void SerialConnectFailedHandler(object sender, MessengerEventArgs args);
    public delegate void SerialConnectSuccessHandler(object sender, MessengerEventArgs args);

    public class MessengerEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public MessengerEventArgs(string m)
        {
            Message = m;
        }
    }
}