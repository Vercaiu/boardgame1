using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public struct CardData : INetworkSerializable, IEquatable<CardData>
{
    public int cardId;
    public int spriteId;
    public FixedString64Bytes cardName;
    public int snowgouleid;
    public int signid;
    public int trees;
    public int moose;
    public int bats;
    public int fire;
    public int geese;

    public bool Equals(CardData other)
    {
        return cardId == other.cardId &&
               spriteId == other.spriteId &&
               cardName.Equals(other.cardName) &&
               trees == other.trees &&
               moose == other.moose &&
               snowgouleid == other.snowgouleid &&
               signid == other.signid &&
               bats == other.bats &&
               fire == other.fire &&
               geese == other.geese;
    }

    public override bool Equals(object obj)
    {
        return obj is CardData other && Equals(other);
    }


    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardId);
        serializer.SerializeValue(ref spriteId);
        serializer.SerializeValue(ref cardName);
        serializer.SerializeValue(ref trees);
        serializer.SerializeValue(ref moose);
        serializer.SerializeValue(ref bats);
        serializer.SerializeValue(ref fire);
        serializer.SerializeValue(ref geese);
        serializer.SerializeValue(ref snowgouleid);
        serializer.SerializeValue(ref signid);
    }
}
