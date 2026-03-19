using Unity.Netcode;
using Unity.Collections;

public struct ChatMessage : INetworkSerializable
{
    public ulong senderId;
    public FixedString64Bytes senderName;
    public FixedString128Bytes message;
    public bool isSystemMessage;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref senderId);
        serializer.SerializeValue(ref senderName);
        serializer.SerializeValue(ref message);
        serializer.SerializeValue(ref isSystemMessage);
    }
}