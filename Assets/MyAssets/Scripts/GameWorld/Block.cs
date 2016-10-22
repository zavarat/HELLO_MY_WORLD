﻿using UnityEngine;
using System.Collections;

public struct Block {

    private byte _type;
    public byte type
    {
        set { _type = value; }
        get { return _type; }
    }

    public CustomAABB aabb;

    private string _belongWorld;
    public string belongWorld
    {
        set { _belongWorld = value; }
        get { return _belongWorld; }
    }
}
