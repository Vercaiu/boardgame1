using Unity.Netcode;
using Unity.Collections;
using System;

public struct FieldState : INetworkSerializable, IEquatable<FieldState>
{
    public ulong ownerClientId;
    public FixedList128Bytes<CardData> cards;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ownerClientId);

        int length = cards.Length;
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            cards.Clear();
            for (int i = 0; i < length; i++)
            {
                CardData card = default;
                serializer.SerializeValue(ref card);
                cards.Add(card);
            }
        }
        else
        {
            for (int i = 0; i < cards.Length; i++)
            {
                CardData card = cards[i];
                serializer.SerializeValue(ref card);
            }
        }
    }

    public bool Equals(FieldState other)
    {
        if (ownerClientId != other.ownerClientId)
            return false;

        if (cards.Length != other.cards.Length)
            return false;

        for (int i = 0; i < cards.Length; i++)
        {
            if (!cards[i].Equals(other.cards[i]))
                return false;
        }

        return true;
    }
}
