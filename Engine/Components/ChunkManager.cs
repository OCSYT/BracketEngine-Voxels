using Engine.Core.ECS;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Engine.Components;
using MonoGame.Extended;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using Engine.Core;

public class ChunkManager : Component
{
    public Dictionary<Vector3, Entity> ChunkEntities = new Dictionary<Vector3, Entity>();
    public Dictionary<Vector3, short[,,]> ChunkCache = new Dictionary<Vector3, short[,,]>();
    private int TotalSize { get; set; }
    public Entity Cam { get; set; }
    public int VoxelSize { get; set; } = 1;
    public Vector3 ChunkBounds { get; set; } = new Vector3(16, 128, 16);
    private float RenderDistance { get; set; } = 8;

    private List<Vector3> ChunkCreationQueue { get; set; } = new List<Vector3>();
    private HashSet<Vector3> EnqueuedChunks { get; set; } = new HashSet<Vector3>();

    public override void Start()
    {
        Chunk.VoxelSize = VoxelSize;
        Chunk.ChunkBounds = ChunkBounds;
        TotalSize = 16 * VoxelSize;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out Vector3 hitPoint, out Vector3 hitVoxel, out Vector3 hitNormal)
    {
        hitPoint = Vector3.Zero;
        hitVoxel = Vector3.Zero;
        hitNormal = Vector3.Zero;

        float stepSize = .001f;
        Vector3 currentPosition = origin + Vector3.One * VoxelSize / 2;
        float traveledDistance = 0;

        direction = Vector3.Normalize(direction);

        while (traveledDistance <= maxDistance)
        {
            Vector3 chunkPosition = GetChunkPosition(currentPosition);
            if (ChunkEntities.TryGetValue(chunkPosition, out Entity chunkEntity))
            {
                Chunk chunk = ECSManager.Instance.GetComponent<Chunk>(chunkEntity);
                if (chunk != null)
                {
                    Vector3 localVoxelPosition = GetLocalVoxelPosition(chunkPosition, currentPosition);
                    if (chunk.IsVoxelSolid(localVoxelPosition))
                    {
                        hitPoint = currentPosition;
                        hitVoxel = localVoxelPosition;
                        hitNormal = GetNormal(hitVoxel);
                        return true;
                    }
                }
            }

            currentPosition += direction * stepSize;
            traveledDistance += stepSize;
        }

        return false;
    }
    public Vector3 GetNormal(Vector3 hitVoxel)
    {
        Vector3 voxelCenter = Vector3.Floor(hitVoxel) + Vector3.One * VoxelSize / 2;
        Vector3 hitNormal = Vector3.Zero;

        Vector3 offset = hitVoxel - voxelCenter;

        if (Math.Abs(offset.X) > Math.Abs(offset.Y) && Math.Abs(offset.X) > Math.Abs(offset.Z))
        {
            hitNormal = new Vector3(Math.Sign(offset.X), 0, 0);
        }
        else if (Math.Abs(offset.Y) > Math.Abs(offset.X) && Math.Abs(offset.Y) > Math.Abs(offset.Z))
        {
            hitNormal = new Vector3(0, Math.Sign(offset.Y), 0);
        }
        else
        {
            hitNormal = new Vector3(0, 0, Math.Sign(offset.Z));
        }

        return hitNormal;
    }

    public void UpdateChunkCache(Vector3 ChunkPos, short[,,] ChunkData)
    {
        if (ChunkCache.ContainsKey(ChunkPos))
        {
            ChunkCache[ChunkPos] = ChunkData;
        }
        else
        {
            ChunkCache.Add(ChunkPos, ChunkData);
        }
    }

    public short[,,] GetChunkCache(Vector3 ChunkPos)
    {
        if (ChunkCache.ContainsKey(ChunkPos))
        {
            return ChunkCache[ChunkPos];
        }
        else
        {
            return null;
        }
    }


    public Chunk GetChunkAtWorldPosition(Vector3 worldPosition)
    {
        Vector3 chunkPosition = GetChunkPosition(worldPosition);
        if (ChunkEntities.TryGetValue(chunkPosition, out Entity chunkEntity))
        {
            return ECSManager.Instance.GetComponent<Chunk>(chunkEntity);
        }
        return null;
    }

    private Vector3 GetChunkPosition(Vector3 worldPosition)
    {
        return new Vector3(
            MathF.Floor(worldPosition.X / TotalSize) * TotalSize,
            MathF.Floor(worldPosition.Y / ChunkBounds.Y) * ChunkBounds.Y,
            MathF.Floor(worldPosition.Z / TotalSize) * TotalSize
        );
    }
    public bool IsWithinChunkBounds(Vector3 voxelPosition)
    {
        return voxelPosition.X >= 0 && voxelPosition.X < ChunkBounds.X &&
               voxelPosition.Y >= 0 && voxelPosition.Y < ChunkBounds.Y &&
               voxelPosition.Z >= 0 && voxelPosition.Z < ChunkBounds.Z;
    }

    public Vector3 GetLocalVoxelPosition(Vector3 chunkPosition, Vector3 worldPosition)
    {
        return Vector3.Clamp(worldPosition - chunkPosition * VoxelSize, Vector3.Zero, ChunkBounds);
    }

    public override void FixedUpdate(GameTime gameTime)
    {

        GenerateChunksNearTarget(Cam.Transform.Position, RenderDistance);

        if (ChunkCreationQueue.Count > 0)
        {
            ChunkCreationQueue.Sort((a, b) =>
            {
                float distanceA = Vector3.DistanceSquared(Cam.Transform.Position, a);
                float distanceB = Vector3.DistanceSquared(Cam.Transform.Position, b);
                return distanceA.CompareTo(distanceB);
            });

            Vector3 chunkPos = ChunkCreationQueue[0];
            ChunkCreationQueue.RemoveAt(0);
            EnqueuedChunks.Remove(chunkPos);

            Vector3 targetPosition = new Vector3(
                MathF.Round(Cam.Transform.Position.X / TotalSize) * TotalSize,
                0,
                MathF.Round(Cam.Transform.Position.Z / TotalSize) * TotalSize
            );
            float distanceFromTarget = Vector3.Distance(targetPosition, chunkPos);
            if (distanceFromTarget <= RenderDistance * TotalSize)
            {
                CreateChunk(Cam.Transform.Position, chunkPos, RenderDistance);
            }
        }
    }

    public void GenerateChunksNearTarget(Vector3 targetPosition, float range)
    {
        Vector3 chunkPosStart = new Vector3(
            MathF.Round(targetPosition.X / TotalSize) * TotalSize,
            0,
            MathF.Round(targetPosition.Z / TotalSize) * TotalSize
        );

        for (float xOffset = -range * TotalSize; xOffset <= range * TotalSize; xOffset += TotalSize)
        {
            for (float zOffset = -range * TotalSize; zOffset <= range * TotalSize; zOffset += TotalSize)
            {
                Vector3 chunkPos = chunkPosStart + new Vector3(xOffset, 0, zOffset);
                if (!ChunkEntities.ContainsKey(chunkPos) && !EnqueuedChunks.Contains(chunkPos))
                {
                    ChunkCreationQueue.Add(chunkPos);
                    EnqueuedChunks.Add(chunkPos);
                }
            }
        }

        RemoveChunksOutsideRange(chunkPosStart, range * TotalSize);
    }

    private void RemoveChunksOutsideRange(Vector3 targetPosition, float range)
    {
        List<Vector3> chunksToRemove = new List<Vector3>();

        foreach (var chunkPos in ChunkEntities.Keys)
        {
            float distance = Vector3.Distance(targetPosition, chunkPos);
            if (distance > range)
            {
                ECSManager.Instance.RemoveEntity(ChunkEntities[chunkPos]);
                ChunkEntities.Remove(chunkPos);
            }
        }
    }

    public void CreateChunk(Vector3 targetPosition, Vector3 position, float range)
    {
        Entity chunkEntity = ECSManager.Instance.CreateEntity();
        chunkEntity.Transform.Position = position;

        Chunk chunk = new Chunk { Manager = this };
        ECSManager.Instance.AddComponent(chunkEntity, chunk);
        ChunkEntities.Add(position, chunkEntity);
        short[,,] ChunkData = GetChunkCache(position);
        if (ChunkData != null)
        {
            chunk.chunkData = ChunkData;
            chunk.GenerateChunk(true);
        }
        else
        {
            chunk.GenerateTerrain();
        }

    }
}
