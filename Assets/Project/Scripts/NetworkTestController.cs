using Mirror;
using UnityEngine;
using Zenject;

namespace Project.Scripts
{
    public class NetworkTestController: MonoBehaviour
    {
        private INetworkMessagingService _messagingService;

        [Inject]
        public void Construct(INetworkMessagingService messagingService)
        {
            _messagingService = messagingService;
        }

        public void StartHost()
        {
            if (!NetworkClient.active && !NetworkServer.active)
            {
                NetworkManager.singleton.StartHost();
                Debug.Log("[Test] Host started.");
            }
        }

        public void StartClient()
        {
            if (!NetworkClient.active && !NetworkServer.active)
            {
                NetworkManager.singleton.StartClient();
                Debug.Log("[Test] Client starting connection...");
            }
        }
        public void StopNetwork()
        {
            if (NetworkServer.active && NetworkClient.active)
            {
                NetworkManager.singleton.StopHost();
                Debug.Log("[Test] Host stopped.");
            }
            else if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
                Debug.Log("[Test] Client stopped.");
            }
        }
        public void SubscribeToHello()
        {
            if (NetworkClient.isConnected)
            {
                _messagingService.Subscribe<HelloMessage>(OnHelloMessageReceived);
                Debug.Log("[Test] Subscribed to HelloMessage.");
            }
            else
            {
                Debug.LogWarning("[Test] Cannot subscribe: Client is not connected.");
            }
        }
        public void UnsubscribeFromHello()
        {
            if (NetworkClient.isConnected)
            {
                _messagingService.Unsubscribe<HelloMessage>(OnHelloMessageReceived);
                Debug.Log("[Test] Unsubscribed from HelloMessage.");
            }
            else
            {
                Debug.LogWarning("[Test] Cannot unsubscribe: Client is not connected.");
            }
        }
        public void SendHelloMessage()
        {
            if (NetworkServer.active)
            {
                var msg = new HelloMessage { Text = "Hello Client!" };
                _messagingService.SendToSubscribed(msg);
                Debug.Log("[Test] Sent HelloMessage from Server to all subscribed clients.");
            }
            else
            {
                Debug.LogWarning("[Test] Cannot send: Server is not active.");
            }
        }
        
        private void OnHelloMessageReceived(HelloMessage message)
        {
            Debug.Log($"<color=green>[Client Received] HelloMessage: {message.Text}</color>");
        }
        
        private void OnDestroy()
        {
            if (_messagingService != null)
            {
                _messagingService.Unsubscribe<HelloMessage>(OnHelloMessageReceived);
            }
        }
    }
}