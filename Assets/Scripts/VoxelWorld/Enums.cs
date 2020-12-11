using System;
using UnityEngine;

namespace VoxelWorld
{
    [Serializable]
    public enum CubeSide
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    [Serializable]
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Planks,
        Brick,
        Wood,
        Bedrock,
        CoalOre,
        IronOre,
        GoldOre,
        RedstoneOre
    }

    [Serializable]
    public enum ChunkState
    {
        Idle,
        Keep,
        Draw
    }

    [Serializable]
    public enum Crack
    {
        Crack0,
        Crack10,
        Crack20,
        Crack30,
        Crack40,
        Crack50,
        Crack60,
        Crack70,
        Crack80,
        Crack90,
        Crack100
}
    
}