using System;
using Mirror;

namespace Project.Scripts
{
    public interface INetworkMessagingService
    {
        void Subscribe<T>(Action<T> onMessageReceived) where T : struct, NetworkMessage;
        void Unsubscribe<T>(Action<T> onMessageReceived) where T : struct, NetworkMessage;
        void SendToSubscribed<T>(T message) where T : struct, NetworkMessage;
    }
}