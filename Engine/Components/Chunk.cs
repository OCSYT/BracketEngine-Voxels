using Engine.Core;
using Engine.Core.Components.Physics;
using Engine.Core.Components.Rendering;
using Engine.Core.ECS;
using Engine.Core.Physics;
using Engine.Core.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simplex;

namespace Engine.Components
{
    public class Chunk : Component
    {
        public static int VoxelSize = 5;
        public static Vector3 ChunkBounds = new Vector3(16, 128, 16);

        public ChunkManager Manager;
        private MeshRenderer renderer;
        public short[,,] chunkData = new short[(short)ChunkBounds.X, (short)ChunkBounds.Y, (short)ChunkBounds.Z];

        int TextureLookUp(int BlockID, Face Face)
        {
            if(BlockID == 1) //Grass
            {
                if (Face == Face.Top)
                {
                    return 0;
                }
                else if (Face == Face.Bottom)
                {
                    return 2;
                }
                else if (Face == Face.Left)
                {
                    return 3;
                }
                else if (Face == Face.Right)
                {
                    return 3;
                }
                else if (Face == Face.Front)
                {
                    return 3;
                }
                else if (Face == Face.Back)
                {
                    return 3;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return BlockID;
            }
        }

        public override void Start()
        {
            Texture2D ChunkTex = EngineManager.Instance.Content.Load<Texture2D>("GameContent/Textures/terrain");
            renderer = new MeshRenderer(new StaticMesh(), [new Material { DiffuseTexture = ChunkTex }]);
            ECSManager.Instance.AddComponent(Entity, renderer);
            GenerateTerrain();
            Console.WriteLine("Created chunk at position: " + Transform.Position);
        }

        public override void OnDestroy()
        {
            Console.WriteLine("Destroyed chunk at position: " + Transform.Position);
        }

        public void GenerateTerrain()
        {
            Noise simplexNoise = new Noise();
            float noiseScale = 0.005f;
            float noiseHeight = 0.1f;

            Parallel.For(0, (int)ChunkBounds.X, x =>
            {
                for (int z = 0; z < ChunkBounds.Z; z++)
                {
                    float worldVoxelPositionX = (Transform.Position.X + x * VoxelSize);
                    float worldVoxelPositionZ = (Transform.Position.Z + z * VoxelSize);

                    float height = ChunkBounds.Y / 2 + simplexNoise.CalcPixel2D((short)worldVoxelPositionX, (short)worldVoxelPositionZ, noiseScale) * noiseHeight;

                    height = Math.Clamp(height, 0, ChunkBounds.Y - 1);

                    for (short y = 0; y < ChunkBounds.Y; y++)
                    {
                        chunkData[x, y, z] = (y <= height) ? (short)1 : (short)0;
                    }
                }
            });
            GenerateChunk(true);
        }

        public void GenerateChunk(bool updateNeighbors)
        {
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<short> indices = new List<short>();
            int totalVertexCount = 0;

            for (int x = 0; x < ChunkBounds.X; x++)
            {
                for (int y = 0; y < ChunkBounds.Y; y++)
                {
                    for (int z = 0; z < ChunkBounds.Z; z++)
                    {
                        if (chunkData[x, y, z] != 0)
                        {
                            List<Face> exposedFaces = GetExposedFaces(x, y, z);

                            foreach (var face in exposedFaces)
                            {
                                var (voxelVertices, voxelIndices) = CreateVoxel(new Vector3(x, y, z) * VoxelSize, Vector3.One * VoxelSize, face, TextureLookUp(chunkData[x,y,z], face));
                                vertices.AddRange(voxelVertices);
                                for (int i = 0; i < voxelIndices.Length; i++)
                                {
                                    indices.Add((short)(voxelIndices[i] + totalVertexCount));
                                }
                                totalVertexCount += voxelVertices.Length;
                            }
                        }
                    }
                }
            }

            if (vertices.Count > 0)
            {
                renderer.StaticMesh = PrimitiveModel.CreateStaticMesh(vertices.ToArray(), indices.ToArray());
            }

            if (updateNeighbors)
            {
                UpdateNeighborChunks();
            }
        }

        private void UpdateNeighborChunks()
        {
            Vector3[] neighborOffsets = new Vector3[]
            {
                new Vector3(-1, 0, 0) * VoxelSize * ChunkBounds.X,
                new Vector3(1, 0, 0) * VoxelSize * ChunkBounds.X,
                new Vector3(0, 0, -1) * VoxelSize * ChunkBounds.Z,
                new Vector3(0, 0, 1) * VoxelSize * ChunkBounds.Z
            };

            foreach (var offset in neighborOffsets)
            {
                Vector3 neighborChunkPos = Transform.Position + offset;

                if (Manager.ChunkEntities.TryGetValue(neighborChunkPos, out Entity neighborChunkEntity))
                {
                    Chunk neighborChunk = ECSManager.Instance.GetComponent<Chunk>(neighborChunkEntity);
                    if (neighborChunk != null)
                    {
                        neighborChunk.GenerateChunk(false);
                    }
                }
            }
        }

        private List<Face> GetExposedFaces(int x, int y, int z)
        {
            List<Face> exposedFaces = new List<Face>();
            if (y - 1 < 0)
            {
                exposedFaces.Add(Face.Bottom);
            }
            else if (y + 1 >= ChunkBounds.Y)
            {
                exposedFaces.Add(Face.Top);
            }
            else
            {
                if (IsVisible(x, y - 1, z)) exposedFaces.Add(Face.Bottom);
                if (IsVisible(x, y, z + 1)) exposedFaces.Add(Face.Back);
            }

            if (IsVisible(x + 1, y, z)) exposedFaces.Add(Face.Right);
            if (IsVisible(x - 1, y, z)) exposedFaces.Add(Face.Left);
            if (IsVisible(x, y + 1, z)) exposedFaces.Add(Face.Top);
            if (IsVisible(x, y, z - 1)) exposedFaces.Add(Face.Front);

            return exposedFaces;
        }

        private bool IsVisible(int x, int y, int z)
        {
            if (x >= 0 && x < ChunkBounds.X && y >= 0 && y < ChunkBounds.Y && z >= 0 && z < ChunkBounds.Z)
            {
                return chunkData[x, y, z] == 0;
            }

            int neighborChunkX = x < 0 ? -1 : x >= ChunkBounds.X ? 1 : 0;
            int neighborChunkZ = z < 0 ? -1 : z >= ChunkBounds.Z ? 1 : 0;

            Vector3 neighborChunkPos = Transform.Position + new Vector3(neighborChunkX * VoxelSize * ChunkBounds.X, 0, neighborChunkZ * VoxelSize * ChunkBounds.Z);

            if (!Manager.ChunkEntities.TryGetValue(neighborChunkPos, out Entity neighborChunkEntity))
            {
                return false;
            }

            Chunk neighborChunk = ECSManager.Instance.GetComponent<Chunk>(neighborChunkEntity);
            if (neighborChunk == null)
            {
                return false;
            }
            else
            {
                short localX = (short)((x + ChunkBounds.X) % ChunkBounds.X);
                short localY = (short)((y + ChunkBounds.Y) % ChunkBounds.Y);
                short localZ = (short)((z + ChunkBounds.Z) % ChunkBounds.Z);

                return neighborChunk.chunkData[localX, localY, localZ] == 0;
            }
        }

        public (VertexPositionNormalTexture[], short[]) CreateVoxel(Vector3 position, Vector3 scale, Face face, int texPos, int texSize = 16, int atlasSize = 256)
        {
            float width = scale.X / 2;
            float height = scale.Y / 2;
            float depth = scale.Z / 2;

            var vertices = new VertexPositionNormalTexture[4];
            var indices = new short[] { 0, 2, 3, 0, 1, 2 };

            Vector3 offset1, offset2, offset3, offset4;
            Vector3 normal;
            bool flip = false;

            float texUnit = 1f / atlasSize;
            float uStart = (texPos * texSize  % atlasSize) * texUnit;
            float vStart = (texPos * texSize / atlasSize) * texUnit;
            float uEnd = uStart + texUnit * texSize;
            float vEnd = vStart + texUnit * texSize;

            // UV coordinates
            Vector2 uv1 = new Vector2(uStart, vEnd);
            Vector2 uv2 = new Vector2(uEnd, vEnd);
            Vector2 uv3 = new Vector2(uEnd, vStart);
            Vector2 uv4 = new Vector2(uStart, vStart);

            // Determine the face configuration
            switch (face)
            {
                case Face.Front:
                    normal = Vector3.Backward;
                    offset1 = new Vector3(-width, -height, -depth);
                    offset2 = new Vector3(width, -height, -depth);
                    offset3 = new Vector3(width, height, -depth);
                    offset4 = new Vector3(-width, height, -depth);
                    break;
                case Face.Back:
                    normal = Vector3.Forward;
                    offset1 = new Vector3(-width, -height, depth);
                    offset2 = new Vector3(width, -height, depth);
                    offset3 = new Vector3(width, height, depth);
                    offset4 = new Vector3(-width, height, depth);
                    flip = true;
                    break;
                case Face.Left:
                    normal = Vector3.Left;
                    offset1 = new Vector3(-width, -height, -depth);
                    offset2 = new Vector3(-width, -height, depth);
                    offset3 = new Vector3(-width, height, depth);
                    offset4 = new Vector3(-width, height, -depth);
                    flip = true;
                    break;
                case Face.Right:
                    normal = Vector3.Right;
                    offset1 = new Vector3(width, -height, -depth);
                    offset2 = new Vector3(width, -height, depth);
                    offset3 = new Vector3(width, height, depth);
                    offset4 = new Vector3(width, height, -depth);
                    break;
                case Face.Bottom:
                    normal = Vector3.Down;
                    offset1 = new Vector3(-width, -height, -depth);
                    offset2 = new Vector3(width, -height, -depth);
                    offset3 = new Vector3(width, -height, depth);
                    offset4 = new Vector3(-width, -height, depth);
                    flip = true;
                    break;
                case Face.Top:
                    normal = Vector3.Up;
                    offset1 = new Vector3(-width, height, -depth);
                    offset2 = new Vector3(width, height, -depth);
                    offset3 = new Vector3(width, height, depth);
                    offset4 = new Vector3(-width, height, depth);
                    break;
                default:
                    throw new ArgumentException("Invalid face value", nameof(face));
            }

            // Assign vertices based on flip
            if (flip)
            {
                vertices[0] = new VertexPositionNormalTexture(position + offset4, normal, uv4);
                vertices[1] = new VertexPositionNormalTexture(position + offset3, normal, uv3);
                vertices[2] = new VertexPositionNormalTexture(position + offset2, normal, uv2);
                vertices[3] = new VertexPositionNormalTexture(position + offset1, normal, uv1);
            }
            else
            {
                vertices[0] = new VertexPositionNormalTexture(position + offset1, normal, uv1);
                vertices[1] = new VertexPositionNormalTexture(position + offset2, normal, uv2);
                vertices[2] = new VertexPositionNormalTexture(position + offset3, normal, uv3);
                vertices[3] = new VertexPositionNormalTexture(position + offset4, normal, uv4);
            }

            return (vertices, indices);
        }


        public enum Face
        {
            Front,
            Back,
            Left,
            Right,
            Bottom,
            Top
        }
    }
}
