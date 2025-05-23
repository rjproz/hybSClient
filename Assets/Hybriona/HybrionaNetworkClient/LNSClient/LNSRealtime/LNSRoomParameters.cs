﻿using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class LNSCreateRoomParameters
{
    public bool isPublic { get; set; } = true;
    public string password { get; set; } = null;
    public int maxPlayers { get; set; } = 10;

    /// <summary>
    /// Makes the room active for sometime (in minutes) even if there are no players in the rooms. Good for MMO and persistent worlds
    /// </summary>
    public long idleLife { get; set; } = 0;

    public bool isQuadTreeAllowed { get; set; } = false;
    public Rect quadTreeBounds { get; set; }
    public int maxPlayersInQuadCell { get; set; } = 5;
    public LNSJoinRoomFilter filters { get; set; }


    public void EnableQuadTreeCellOptimization(Vector2 center, Vector2 size)
    {
        this.isQuadTreeAllowed = true;
        this.quadTreeBounds = new Rect(center.x - size.x * .5f, center.y - size.y * .5f, size.x, size.y);
        this.maxPlayersInQuadCell = maxPlayersInQuadCell;
    }

    public void AppendToWriter(NetDataWriter writer)
    {

        writer.Put(isPublic);
        if (string.IsNullOrEmpty(password))
        {
            writer.Put(false);
        }
        else
        {
            writer.Put(true);
            writer.Put(password);
        }

        if (filters != null)
        {
            filters.AppendToWriter(writer);
        }
        else
        {
            writer.Put((byte)0);
        }
        writer.Put(maxPlayers);
        writer.Put(isQuadTreeAllowed);
        if (isQuadTreeAllowed)
        {
            writer.Put(quadTreeBounds.x);
            writer.Put(quadTreeBounds.y);
            writer.Put(quadTreeBounds.width);
            writer.Put(quadTreeBounds.height);
        }

        writer.Put(idleLife * 60);
    }

    public static LNSCreateRoomParameters FromReader(NetPacketReader reader)
    {
        if (reader.AvailableBytes > 0)
        {
            LNSCreateRoomParameters o = new LNSCreateRoomParameters();
            try
            {
                o.isPublic = reader.GetBool();
                if (reader.GetBool())
                {
                    o.password = reader.GetString();
                }


                o.filters = LNSJoinRoomFilter.FromReader(reader);
                o.maxPlayers = reader.GetInt();
                o.isQuadTreeAllowed = reader.GetBool();
                if (o.isQuadTreeAllowed)
                {
                    Rect bounds = new Rect(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                    //bounds.center = new Vector2, reader.GetFloat());

                    o.quadTreeBounds = bounds;
                }
                o.idleLife = reader.GetLong();
            }
            catch
            {
                o.isQuadTreeAllowed = false;
            }
            return o;

        }
        return null;
    }
}


public class LNSJoinRoomFilter
{
    private Dictionary<byte, byte> filters = new Dictionary<byte, byte>();

    public void Reset()
    {
        filters.Clear();
    }

    public bool Set(byte key, byte value)
    {
        if (filters.Count == 256)
        {
            return false;
        }
        if (filters.ContainsKey(key))
        {
            filters[key] = value;
        }
        else
        {
            filters.Add(key, value);
        }

        return true;
    }

    public byte GetLength()
    {
        return (byte)filters.Count;
    }

    public void AppendToWriter(NetDataWriter writer)
    {
        writer.Put((byte)filters.Count);
        foreach (var filter in filters)
        {
            writer.Put(filter.Key);
            writer.Put(filter.Value);
        }
    }

    public static LNSJoinRoomFilter FromReader(NetPacketReader reader)
    {
        int filterCount = reader.GetByte();
        if (filterCount > 0)
        {
            LNSJoinRoomFilter o = new LNSJoinRoomFilter();
            for (int i = 0; i < filterCount; i++)
            {
                o.Set(reader.GetByte(), reader.GetByte());
            }
            return o;
        }
        return null;
    }

    public bool IsFilterMatch(LNSJoinRoomFilter source)
    {
        foreach (var sourceFilter in source.filters)
        {
            if (!filters.ContainsKey(sourceFilter.Key) || filters[sourceFilter.Key] != sourceFilter.Value)
            {
                return false;
            }
        }
        return true;
    }
}

