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
        private MeshRenderer rendererTransparent;
        public short[,,] chunkData = new short[(short)ChunkBounds.X, (short)ChunkBounds.Y, (short)ChunkBounds.Z];

        int TextureLookUp(int BlockID, Face Face)
        {
            if (BlockID == 1) // Grass
            {
                return Face switch
                {
                    Face.Top => 0,
                    Face.Bottom => 2,
                    _ => 3,
                };
            }
            if(BlockID == 2) // Dirt
            {
                return 2;
            }
            if(BlockID == 3) //Stone
            {
                return 1;
            }
            if(BlockID == 4) //Sand
            {
                return 18;
            }
            if(BlockID == 5) //Log
            {
                return Face switch
                {
                    Face.Top => 21,
                    Face.Bottom => 21,
                    _ => 20,
                };
            }
            if (BlockID == 6) //Leaves
            {
                return 52;
            }
            if(BlockID == 7) //Oak Planks
            {
                return 4;
            }
            if(BlockID == 8) //Water
            {
                return 223;
            }
            return BlockID;
        }
        bool TransparentLookUp(int BlockID)
        {
            if(BlockID == 6)
            {
                return true;
            }
            if(BlockID == 8)
            {
                return true;
            }
            return false;
        }

        public override void Start()
        {
            Texture2D ChunkTex = EngineManager.Instance.Content.Load<Texture2D>("GameContent/Textures/terrain");
            renderer = new MeshRenderer(new StaticMesh(), [new Material { DiffuseTexture = ChunkTex }]);
            rendererTransparent = new MeshRenderer(new StaticMesh(), [new Material { DiffuseTexture = ChunkTex, Transparent = true }]);
            ECSManager.Instance.AddComponent(Entity, renderer);
            ECSManager.Instance.AddComponent(Entity, rendererTransparent);
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
            int Seed = (int)Transform.Position.X + (int)Transform.Position.Z;


            float noiseScale = 0.005f;
            float noiseHeight = 0.1f;
            float WaterLevel = (ChunkBounds.Y / 2) + 10;
            for (int x = 0; x < ChunkBounds.X; x++)
            {
                for (int z = 0; z < ChunkBounds.Z; z++)
                {
                    float worldVoxelPositionX = (Transform.Position.X + x * VoxelSize);
                    float worldVoxelPositionZ = (Transform.Position.Z + z * VoxelSize);

                    float height = ChunkBounds.Y / 2 + simplexNoise.CalcPixel2D((short)worldVoxelPositionX, (short)worldVoxelPositionZ, noiseScale) * noiseHeight;

                    height = Math.Clamp(height, 0, ChunkBounds.Y - 1);

                    for (int y = 0; y < ChunkBounds.Y; y++)
                    {

                        if (y <= height)
                        {
                            if(y > height-1)
                            {
                                if(y <= WaterLevel)
                                {
                                    chunkData[x, y, z] = 4;
                                }
                                else
                                {
                                    chunkData[x, y, z] = 1;
                                }
                            }
                            else
                            {
                                chunkData[x, y, z] = 2;
                            }
                        }
                        else if (y <= WaterLevel)
                        {
                            chunkData[x, y, z] = 8;
                        }
                        else
                        {
                            chunkData[x, y, z] = 0;
                        }
                    }
                }
            }

            Random random = new Random(Seed);
            for (int x = 0; x < ChunkBounds.X; x++)
            {
                for (int z = 0; z < ChunkBounds.Z; z++)
                {
                    for (int y = 0; y < ChunkBounds.Y; y++)
                    {
                        if(chunkData[x, y, z] == 1)
                        {
                            PlaceTree(x,y,z, random);
                        }
                    }
                }
            }

            GenerateChunk(true);
        }
        private void PlaceTree(int x, int y, int z, Random random)
        {
            int treeHeight = 7;
            int trunkHeight = 3;
            int treeWidth = 2;
            int chunkSize = (int)ChunkBounds.X;
            int chunkSizeY = (int)ChunkBounds.Y;

            if (y + treeHeight < chunkSizeY &&
                x + treeWidth < chunkSize && z + treeWidth < chunkSize &&
                x - treeWidth > 0 && z - treeWidth > 0 &&
                random.NextDouble() > 0.99)
            {
                for (int i = y + 1; i < y + 1 + trunkHeight; i++)
                {
                    if (chunkData[x, i, z] == 0)
                    {
                        chunkData[x, i, z] = 5;
                    }
                }

                for (int offsetY = y + trunkHeight + 1; offsetY < y + treeHeight; offsetY++)
                {
                    int currentRadius = Math.Clamp(treeWidth - (offsetY - (y + trunkHeight + 1)), 1, chunkSize);
                    for (int offsetX = -currentRadius; offsetX <= currentRadius; offsetX++)
                    {
                        for (int offsetZ = -currentRadius; offsetZ <= currentRadius; offsetZ++)
                        {
                            bool isCornerOrEdge = Math.Abs(offsetX) == currentRadius && Math.Abs(offsetZ) == currentRadius;

                            if (!isCornerOrEdge && chunkData[x + offsetX, offsetY, z + offsetZ] == 0)
                            {
                                chunkData[x + offsetX, offsetY, z + offsetZ] = 6;
                            }
                        }
                    }
                }
            }
        }


        public void GenerateChunk(bool updateNeighbors)
        {
            List<VertexPositionNormalTexture> verticesOpaque = new List<VertexPositionNormalTexture>();
            List<short> indicesOpaque = new List<short>();
            List<VertexPositionNormalTexture> verticesTransparent = new List<VertexPositionNormalTexture>();
            List<short> indicesTransparent = new List<short>();

            int totalVertexCount = 0;

            int totalVertexCountTransparent = 0;


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
                                int texPos = TextureLookUp(chunkData[x, y, z], face);
                                bool isTransparent = TransparentLookUp(chunkData[x, y, z]);

                                var (voxelVertices, voxelIndices) = CreateVoxel(new Vector3(x, y, z) * VoxelSize, Vector3.One * VoxelSize, face, texPos);
                                if (isTransparent)
                                {
                                    verticesTransparent.AddRange(voxelVertices);
                                    foreach (var idx in voxelIndices)
                                    {
                                        indicesTransparent.Add((short)(idx + totalVertexCountTransparent));
                                    }
                                    totalVertexCountTransparent += voxelVertices.Length;
                                }
                                else
                                {
                                    verticesOpaque.AddRange(voxelVertices);
                                    foreach (var idx in voxelIndices)
                                    {
                                        indicesOpaque.Add((short)(idx + totalVertexCount));
                                    }
                                    totalVertexCount += voxelVertices.Length;
                                }
                            }
                        }
                    }
                }
            }

            if (verticesOpaque.Count > 0)
            {
                renderer.StaticMesh = PrimitiveModel.CreateStaticMesh(verticesOpaque.ToArray(), indicesOpaque.ToArray());
            }

            if (verticesTransparent.Count > 0)
            {
                rendererTransparent.StaticMesh = PrimitiveModel.CreateStaticMesh(verticesTransparent.ToArray(), indicesTransparent.ToArray());
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
            bool IsTransparent = TransparentLookUp(chunkData[x, y, z]);
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
                if (IsVisible(x, y - 1, z, IsTransparent)) exposedFaces.Add(Face.Bottom);
                if (IsVisible(x, y + 1, z, IsTransparent)) exposedFaces.Add(Face.Top);
            }

            if (IsVisible(x + 1, y, z, IsTransparent)) exposedFaces.Add(Face.Right);
            if (IsVisible(x - 1, y, z, IsTransparent)) exposedFaces.Add(Face.Left);
            if (IsVisible(x, y, z + 1, IsTransparent)) exposedFaces.Add(Face.Back);
            if (IsVisible(x, y, z - 1, IsTransparent)) exposedFaces.Add(Face.Front);

            return exposedFaces;
        }

        private bool IsVisible(int x, int y, int z, bool IsTransparent)
        {
            if (x >= 0 && x < ChunkBounds.X && y >= 0 && y < ChunkBounds.Y && z >= 0 && z < ChunkBounds.Z)
            {
                return chunkData[x, y, z] == 0 || (TransparentLookUp(chunkData[x, y, z]) && !IsTransparent);
            }

            // Handle neighboring chunks
            int neighborChunkX = x < 0 ? -1 : x >= ChunkBounds.X ? 1 : 0;
            int neighborChunkZ = z < 0 ? -1 : z >= ChunkBounds.Z ? 1 : 0;

            Vector3 neighborChunkPos = Transform.Position + new Vector3(neighborChunkX * VoxelSize * ChunkBounds.X, 0, neighborChunkZ * VoxelSize * ChunkBounds.Z);

            if (Manager.ChunkEntities.TryGetValue(neighborChunkPos, out Entity neighborChunkEntity))
            {
                Chunk neighborChunk = ECSManager.Instance.GetComponent<Chunk>(neighborChunkEntity);
                if (neighborChunk != null)
                {
                    short localX = (short)((x + ChunkBounds.X) % ChunkBounds.X);
                    short localY = (short)((y + ChunkBounds.Y) % ChunkBounds.Y);
                    short localZ = (short)((z + ChunkBounds.Z) % ChunkBounds.Z);

                    return neighborChunk.chunkData[localX, localY, localZ] == 0 || 
                        TransparentLookUp(neighborChunk.chunkData[localX, localY, localZ])
                        && !IsTransparent;
                }
            }

            return false;
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

            int column = (texPos % (atlasSize / texSize));
            int row = (texPos / (atlasSize / texSize));
            float texUnit = 1f / atlasSize;
            float uStart = column * texSize * texUnit;
            float vStart = row * texSize * texUnit;
            float uEnd = uStart + texSize * texUnit;
            float vEnd = vStart + texSize * texUnit;


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
