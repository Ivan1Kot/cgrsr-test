using Mirror;

namespace Project.Scripts
{
    public struct SubscribeMessage : NetworkMessage
    {
        public ushort MessageId;
    }

    public struct UnsubscribeMessage : NetworkMessage
    {
        public ushort MessageId;
    }
    
    public struct HelloMessage : NetworkMessage
    {
        public string Text;
    }
}