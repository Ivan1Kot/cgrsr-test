using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Zenject;

namespace Project.Scripts
{
    public class MirrorNetworkMessagingService: INetworkMessagingService, IInitializable, IDisposable
    {
        private readonly Dictionary<ushort, HashSet<NetworkConnectionToClient>> _serverSubscriptions = new();
        private readonly Dictionary<ushort, List<Delegate>> _clientSubscriptions = new();
        private readonly Dictionary<ushort, Action> _clientUnregisterActions = new ();
        
        public void Initialize()
        {
            NetworkServer.RegisterHandler<SubscribeMessage>(OnSubscribeMessageReceived);
            NetworkServer.RegisterHandler<UnsubscribeMessage>(OnUnsubscribeMessageReceived);

            NetworkServer.OnDisconnectedEvent += OnServerDisconnected;
            
            NetworkClient.OnConnectedEvent += OnClientConnected;
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<SubscribeMessage>();
            NetworkServer.UnregisterHandler<UnsubscribeMessage>();
            
            NetworkServer.OnDisconnectedEvent -= OnServerDisconnected;
            
            NetworkClient.OnConnectedEvent -= OnClientConnected;

            foreach (var unregisterAction in _clientUnregisterActions.Values)
            {
                unregisterAction?.Invoke();
            }
            _clientUnregisterActions.Clear();
            _clientSubscriptions.Clear();
        }
        
        private void OnSubscribeMessageReceived(NetworkConnectionToClient conn, SubscribeMessage msg)
        {
            if (!_serverSubscriptions.TryGetValue(msg.MessageId, out var connections))
            {
                connections = new HashSet<NetworkConnectionToClient>();
                _serverSubscriptions[msg.MessageId] = connections;
            }
            
            connections.Add(conn);
            Debug.Log($"[Server] Client {conn.connectionId} subscribed to message ID: {msg.MessageId}");
        }

        private void OnUnsubscribeMessageReceived(NetworkConnectionToClient conn, UnsubscribeMessage msg)
        {
            if (_serverSubscriptions.TryGetValue(msg.MessageId, out var connections))
            {
                connections.Remove(conn);
                Debug.Log($"[Server] Client {conn.connectionId} unsubscribed from message ID: {msg.MessageId}");
            }
        }
        
        private void OnServerDisconnected(NetworkConnectionToClient conn)
        {
            foreach (var connections in _serverSubscriptions.Values)
            {
                connections.Remove(conn);
            }
            
            Debug.Log($"[Server] Cleaned up subscriptions for disconnected client {conn.connectionId}");
        }
        
        public void SendToSubscribed<T>(T message) where T : struct, NetworkMessage
        {
            ushort msgId = NetworkMessages.GetId<T>();

            if (_serverSubscriptions.TryGetValue(msgId, out var connections))
            {
                foreach (var conn in connections)
                {
                    if (conn != null && conn.isReady)
                    {
                        conn.Send(message);
                        Debug.Log($"[Server] Sent message {typeof(T).Name} to client {conn.connectionId}");
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> onMessageReceived) where T : struct, NetworkMessage
        {
            ushort msgId = NetworkMessages.GetId<T>();

            if (!_clientSubscriptions.TryGetValue(msgId, out var delegates))
            {
                delegates = new List<Delegate>();
                _clientSubscriptions[msgId] = delegates;
                
                NetworkClient.RegisterHandler<T>(OnMessageReceivedInternal);

                _clientUnregisterActions[msgId] = () => NetworkClient.UnregisterHandler<T>();

                if (NetworkClient.isConnected)
                {
                    NetworkClient.Send(new SubscribeMessage {MessageId = msgId});
                }
            }

            if (!delegates.Contains(onMessageReceived))
            {
                delegates.Add(onMessageReceived);
            }
        }
        
        public void Unsubscribe<T>(Action<T> onMessageReceived) where T : struct, NetworkMessage
        {
            ushort msgId = NetworkMessages.GetId<T>();

            if (_clientSubscriptions.TryGetValue(msgId, out var delegates))
            {
                delegates.Remove(onMessageReceived);

                if (delegates.Count == 0)
                {
                    if (_clientUnregisterActions.TryGetValue(msgId, out var unregisterAction))
                    {
                        unregisterAction?.Invoke();
                        _clientUnregisterActions.Remove(msgId);
                    }

                    if (NetworkClient.isConnected)
                    {
                        NetworkClient.Send(new UnsubscribeMessage {MessageId = msgId});
                    }
                    
                    _clientSubscriptions.Remove(msgId);
                }
            }
        }
        
        private void OnClientConnected()
        {
            Debug.Log("[Client] Connected to server. Resending active subscriptions...");

            foreach (var msgId in _clientSubscriptions.Keys)
            {
                NetworkClient.Send(new SubscribeMessage {MessageId = msgId});
                Debug.Log($"[Client] Resending subscription for message ID: {msgId}");
            }
        }

        private void OnMessageReceivedInternal<T>(T message) where T : struct, NetworkMessage
        {
            ushort msgId = NetworkMessages.GetId<T>();

            if (_clientSubscriptions.TryGetValue(msgId, out var delegates))
            {
                var listCopy = new List<Delegate>(delegates);
                foreach (var del in listCopy)
                {
                    if (del is Action<T> action)
                    {
                        try
                        {
                            action.Invoke(message);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }
    }
}