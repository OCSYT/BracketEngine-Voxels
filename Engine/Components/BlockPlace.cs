using Engine.Core;
using Engine.Core.Components.Rendering;
using Engine.Core.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Engine.Components
{
    public class BlockPlace : Component
    {
        public ChunkManager Manager;
        public Entity Cam;
        public List<BlockType> BlockTypes = new List<BlockType>
        {
            new BlockType { Name = "Grass", Id = 1 },
            new BlockType { Name = "Dirt", Id = 2 },
            new BlockType { Name = "Stone", Id = 3 },
            new BlockType { Name = "Cobblestone", Id = 9 },
            new BlockType { Name = "Sand", Id = 4 },
            new BlockType { Name = "Oak Log", Id = 5 },
            new BlockType { Name = "Oak Leaves", Id = 6 },
            new BlockType { Name = "Oak Planks", Id = 7 },
            new BlockType { Name = "Glass", Id = 10 },
        };
        public int CurrentBlockIndex = 0;
        private bool RPressed;
        private bool LPressed;

        public override void FixedUpdate(GameTime gameTime)
        {
            // Check for raycast hit
            if (Manager.Raycast(Cam.Transform.Position, Cam.Transform.Forward, 10, out Vector3 hit, out Vector3 hitVoxel, out Vector3 hitNormal))
            {
                if (EngineManager.Instance.IsActive)
                {
                    HandleRaycastHit(hit, hitVoxel, hitNormal);
                }
            }

            // Handle block type switching with keyboard input
            HandleBlockSwitching();
        }

        private void HandleRaycastHit(Vector3 hit, Vector3 hitVoxel, Vector3 hitNormal)
        {
            // Handle right mouse button for placing blocks
            if (Mouse.GetState().RightButton == ButtonState.Pressed && !RPressed)
            {
                RPressed = true;
                BlockType currentBlock = BlockTypes[CurrentBlockIndex];
                Chunk hitChunk = Manager.GetChunkAtWorldPosition(hit);
                if (hitChunk != null)
                {
                    hitVoxel += hitNormal;
                    hitVoxel = Vector3.Floor(hitVoxel);
                    hitChunk.chunkData[(int)hitVoxel.X, (int)hitVoxel.Y, (int)hitVoxel.Z] = currentBlock.Id;
                    hitChunk.GenerateChunk(true);
                    Manager.UpdateChunkCache(hitChunk.Transform.Position, hitChunk.chunkData);
                }
            }
            else if (Mouse.GetState().RightButton == ButtonState.Released && RPressed)
            {
                RPressed = false;
            }

            // Handle left mouse button for removing blocks
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && !LPressed)
            {
                LPressed = true;
                Chunk hitChunk = Manager.GetChunkAtWorldPosition(hit);
                if (hitChunk != null)
                {
                    hitVoxel = Vector3.Floor(hitVoxel);
                    hitChunk.chunkData[(int)hitVoxel.X, (int)hitVoxel.Y, (int)hitVoxel.Z] = 0; // Set to empty block
                    hitChunk.GenerateChunk(true);
                    Manager.UpdateChunkCache(hitChunk.Transform.Position, hitChunk.chunkData);
                }
            }
            else if (Mouse.GetState().LeftButton == ButtonState.Released && LPressed)
            {
                LPressed = false;
            }
        }

        private int lastMouseWheelValue = 0;
        private void HandleBlockSwitching()
        {
            MouseState mouseState = Mouse.GetState();

            if (mouseState.ScrollWheelValue != lastMouseWheelValue)
            {
                int scrollDelta = mouseState.ScrollWheelValue - lastMouseWheelValue;
                if (scrollDelta != 0)
                {
                    CurrentBlockIndex += Math.Sign(scrollDelta);
                    CurrentBlockIndex = Math.Clamp(CurrentBlockIndex, 0, BlockTypes.Count - 1);
                }
            }

            lastMouseWheelValue = mouseState.ScrollWheelValue;

            EngineManager.Instance.UIManager.BlockLabel.Text = BlockTypes[CurrentBlockIndex].Name;
        }

    }

    public class BlockType
    {
        public string Name { get; set; }
        public short Id { get; set; }
    }
}
