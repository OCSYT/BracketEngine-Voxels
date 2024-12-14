using Engine.Core.ECS;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Engine.Components;
using MonoGame.Extended;

public class ChunkManager : Component
{
    public Dictionary<Vector3, Entity> ChunkEntities { get; private set; } = new Dictionary<Vector3, Entity>();
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

        Chunk chunk = new Chunk
        {
            Manager = this,
        };

        ECSManager.Instance.AddComponent(chunkEntity, chunk);
        ChunkEntities.Add(position, chunkEntity);
    }
}
