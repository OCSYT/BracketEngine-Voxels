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
        private Dictionary<int, MeshRenderer> transparentRenderers = new Dictionary<int, MeshRenderer>();
        public static int TransparentLayers = 2;
        public static DepthStencilState[] DepthMode =
        {
            DepthStencilState.Default,
            DepthStencilState.DepthRead,
        };
        public short[,,] chunkData = new short[(short)ChunkBounds.X, (short)ChunkBounds.Y, (short)ChunkBounds.Z];
        public short[,,] lightDataSun = new short[(short)ChunkBounds.X, (short)ChunkBounds.Y, (short)ChunkBounds.Z];
        public float WaterHeight = 10;
        public short MaxSunLight = 15;
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
            if(BlockID == 9) //Coblestone
            {
                return 16;
            }
            if (BlockID == 10) //Glass
            {
                return 49;
            }
            if(BlockID == 11) //Tall grass
            {
                return 39;
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
            if (BlockID == 10)
            {
                return true;
            }
            if(BlockID == 11)
            {
                return true;
            }
            return false;
        }

        private int GetTransparencyLayer(int BlockID)
        {
            return BlockID switch
            {
                6 => 0,
                8 => 1,
                10 => 0,
                11 => 0,
                _ => 0 
            };
        }


        bool CollisionLookUp(int BlockID)
        {
            if (BlockID == 0)
            {
                return false;
            }
            if (BlockID == 8)
            {
                return false;
            }
            return true;
        }

        bool BillboardLookUp(int BlockID)
        {
            if (BlockID == 11)
            {
                return true;
            }
            return false;
        }

        public bool IsVoxelSolid(Vector3 localPosition)
        {
            if (localPosition.X >= 0 && localPosition.X < ChunkBounds.X && localPosition.Y >= 0 && localPosition.Y < ChunkBounds.Y && localPosition.Z >= 0 && localPosition.Z < ChunkBounds.Z)
            {
                int BlockID = chunkData[(int)localPosition.X, (int)localPosition.Y, (int)localPosition.Z];
                return CollisionLookUp(BlockID);
            }
            return false;
        }
        public override void Awake()
        {
            Texture2D ChunkTex = EngineManager.Instance.Content.Load<Texture2D>("GameContent/Textures/terrain");
            renderer = new MeshRenderer(new StaticMesh(), [new Material { DiffuseTexture = ChunkTex, Lighting = true, VertexColors = true }]);
            ECSManager.Instance.AddComponent(Entity, renderer);
            for (int layer = 0; layer < TransparentLayers; layer++)
            {
                MeshRenderer transparentRenderer = new MeshRenderer(
                    new StaticMesh(),
                    [new Material { DiffuseTexture = ChunkTex, Transparent = true, VertexColors = true, Lighting = true, SortOrder = layer + 2,
                    DepthStencilState = DepthMode[layer] }]
                );
                ECSManager.Instance.AddComponent(Entity, transparentRenderer);

                transparentRenderers.Add(layer, transparentRenderer);
            }
            //Console.WriteLine("Created chunk at position: " + Transform.Position);
        }

        public override void OnDestroy()
        {
            //Console.WriteLine("Destroyed chunk at position: " + Transform.Position);
        }

        public void GenerateSunLighting()
        {
            Parallel.For(0, (int)ChunkBounds.X, x =>
            {
                for (int z = 0; z < ChunkBounds.Z; z++)
                {
                    short PrevSunLight = MaxSunLight;
                    bool Exposed = true;

                    for (int y = (int)ChunkBounds.Y - 1; y >= 0; y--)
                    {
                        if (chunkData[x, y, z] != 0)
                        {
                            Exposed = false;
                        }
                        if (Exposed)
                        {
                            lightDataSun[x, y, z] = MaxSunLight;
                        }
                        else
                        {
                            if (chunkData[x, y, z] != 0)
                            {
                                PrevSunLight = (short)Math.Clamp(PrevSunLight - 1, 0, MaxSunLight);
                                lightDataSun[x, y, z] = PrevSunLight;
                            }
                            else
                            {
                                lightDataSun[x, y, z] = PrevSunLight;
                            }
                        }
                    }
                }
            });
        }

        public float GetColor(int x, int y, int z, Face face)
        {
            Vector3 direction;
            switch (face)
            {
                case Face.Front:
                    direction = new Vector3(0, 0, -1);
                    break;
                case Face.Back:
                    direction = new Vector3(0, 0, 1);
                    break;
                case Face.Left:
                    direction = new Vector3(-1, 0, 0);
                    break;
                case Face.Right:
                    direction = new Vector3(1, 0, 0);
                    break;
                case Face.Top:
                    direction = new Vector3(0, 1, 0);
                    break;
                case Face.Bottom:
                    direction = new Vector3(0, -1, 0);
                    break;
                default:
                    throw new ArgumentException("Invalid face value", nameof(face));
            }

            if (x + direction.X < 0 || x + direction.X >= ChunkBounds.X || y + direction.Y < 0 || y + direction.Y >= ChunkBounds.Y || z + direction.Z < 0 || z + direction.Z >= ChunkBounds.Z)
            {
                // If out of bounds, attempt to fetch light data from the neighbor chunk
                Chunk neighborChunk = GetNeighborChunk(direction);
                if (neighborChunk != null)
                {
                    switch (face)
                    {
                        case Face.Front:
                            return (neighborChunk.lightDataSun[x, y, (int)((z - 1 + ChunkBounds.Z) % ChunkBounds.Z)]) / (float)MaxSunLight;
                        case Face.Back:
                            return (neighborChunk.lightDataSun[x, y, (int)((z + 1) % ChunkBounds.Z)]) / (float)MaxSunLight;
                        case Face.Left:
                            return (neighborChunk.lightDataSun[(int)((x - 1 + ChunkBounds.X) % ChunkBounds.X), y, z]) / (float)MaxSunLight;
                        case Face.Right:
                            return (neighborChunk.lightDataSun[(int)((x + 1) % ChunkBounds.X), y, z]) / (float)MaxSunLight;
                        default:
                            throw new ArgumentException("Invalid face value", nameof(face));
                    }
                }
                else
                {
                    // If no neighboring chunk is found, return 0
                    return lightDataSun[x,y,z];
                }
            }

            float color = 0;

            switch (face)
            {
                case Face.Front:
                    if (z - 1 >= 0)
                    {
                        color = (lightDataSun[x, y, z - 1]) / (float)MaxSunLight;
                    }
                    break;
                case Face.Back:
                    if (z + 1 < ChunkBounds.Z)
                    {
                        color = (lightDataSun[x, y, z + 1]) / (float)MaxSunLight;
                    }
                    break;
                case Face.Left:
                    if (x - 1 >= 0)
                    {
                        color = (lightDataSun[(int)((x - 1 + ChunkBounds.X) % ChunkBounds.X), y, z]) / (float)MaxSunLight;
                    }
                    break;
                case Face.Right:
                    if (x + 1 < ChunkBounds.X)
                    {
                        color = (lightDataSun[(int)((x + 1) % ChunkBounds.X), y, z]) / (float)MaxSunLight;
                    }
                    break;
                case Face.Top:
                    if (y + 1 < ChunkBounds.Y)
                    {
                        color = (lightDataSun[x, y + 1, z]) / (float)MaxSunLight;
                    }
                    break;
                case Face.Bottom:
                    if (y - 1 >= 0)
                    {
                        color = (lightDataSun[x, y - 1, z]) / (float)MaxSunLight;
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid face value", nameof(face));
            }

            // Clamp the color value between 0 and 1
            return Math.Clamp(color, .1f, 1);
        }


        public void GenerateChunk(bool updateNeighbors)
        {
            ChunkUpdate();
            GenerateSunLighting();

            List<Material.VertexPositionNormalTextureColor> verticesOpaque = new List<Material.VertexPositionNormalTextureColor>();
            List<short> indicesOpaque = new List<short>();

            Dictionary<int, List<Material.VertexPositionNormalTextureColor>> layerVertices = new Dictionary<int, List<Material.VertexPositionNormalTextureColor>>();
            Dictionary<int, List<short>> layerIndices = new Dictionary<int, List<short>>();

            foreach (var layer in transparentRenderers.Keys)
            {
                layerVertices[layer] = new List<Material.VertexPositionNormalTextureColor>();
                layerIndices[layer] = new List<short>();
            }

            int totalVertexCountOpaque = 0;
            Dictionary<int, int> layerVertexCounts = new Dictionary<int, int>();

            foreach (var layer in transparentRenderers.Keys)
            {
                layerVertexCounts[layer] = 0;
            }

            for (int x = 0; x < ChunkBounds.X; x++)
            {
                for (int y = 0; y < ChunkBounds.Y; y++)
                {
                    for (int z = 0; z < ChunkBounds.Z; z++)
                    {
                        if (chunkData[x, y, z] != 0)
                        {
                            
                            List<Face> exposedFaces = GetExposedFaces(x, y, z, chunkData[x, y, z]);
                            foreach (var face in exposedFaces)
                            {
                                int texPos = TextureLookUp(chunkData[x, y, z], face);
                                bool isTransparent = TransparentLookUp(chunkData[x, y, z]);
                                bool isBillboard = BillboardLookUp(chunkData[x, y, z]);

                                float VoxelColor = GetColor(x, y, z, face);

                                var (voxelVertices, voxelIndices) = isBillboard == false ? 
                                    CreateVoxel(new Vector3(x, y, z) * VoxelSize, Vector3.One * VoxelSize, face, texPos, new Color(VoxelColor, VoxelColor, VoxelColor, VoxelColor))
                                    : CreateBillboard(new Vector3(x, y, z) * VoxelSize, Vector3.One * VoxelSize, texPos, new Color(VoxelColor, VoxelColor, VoxelColor, VoxelColor));

                                if (isTransparent)
                                {
                                    int layer = GetTransparencyLayer(chunkData[x, y, z]);

                                    if (layerVertices.ContainsKey(layer))
                                    {
                                        layerVertices[layer].AddRange(voxelVertices);
                                        foreach (var idx in voxelIndices)
                                        {
                                            layerIndices[layer].Add((short)(idx + layerVertexCounts[layer]));
                                        }
                                        layerVertexCounts[layer] += voxelVertices.Length;
                                    }
                                }
                                else
                                {
                                    verticesOpaque.AddRange(voxelVertices);
                                    foreach (var idx in voxelIndices)
                                    {
                                        indicesOpaque.Add((short)(idx + totalVertexCountOpaque));
                                    }
                                    totalVertexCountOpaque += voxelVertices.Length;
                                }
                            }
                        }
                    }
                }
            }

            if (verticesOpaque.Count > 0)
            {
                renderer.StaticMesh = PrimitiveModel.CreateStaticMesh(verticesOpaque.ToArray(), indicesOpaque.ToArray());
                renderer.StaticMesh.CalculateBoundingSpheres(EngineManager.Instance.GraphicsDevice);
            }
            else
            {
                renderer.StaticMesh = new StaticMesh();
            }
            foreach (var layer in transparentRenderers.Keys)
            {
                if (layerVertices[layer].Count > 0)
                {
                    transparentRenderers[layer].StaticMesh = PrimitiveModel.CreateStaticMesh(layerVertices[layer].ToArray(), layerIndices[layer].ToArray());
                    transparentRenderers[layer].StaticMesh.CalculateBoundingSpheres(EngineManager.Instance.GraphicsDevice);
                }
                else
                {
                    transparentRenderers[layer].StaticMesh = new StaticMesh();
                }
            }

            if (updateNeighbors)
            {
                UpdateNeighborChunks();
            }
        }

        public (Material.VertexPositionNormalTextureColor[], short[]) CreateBillboard(Vector3 position, Vector3 scale, int texPos, Color color, int texSize = 16, int atlasSize = 256)
        {
            float width = scale.X / 2;
            float height = scale.Y / 2;

            var vertices = new List<Material.VertexPositionNormalTextureColor>();
            var indices = new List<short>();

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

            // Define offsets for two quads
            Vector3[] quad1Front = new[]
            {
        new Vector3(-width, -height, -width), // Bottom-left
        new Vector3(width, -height, width),  // Bottom-right
        new Vector3(width, height, width),   // Top-right
        new Vector3(-width, height, -width)  // Top-left
    };

            Vector3[] quad1Back = new[]
            {
                quad1Front[0], quad1Front[3], quad1Front[2], quad1Front[1]
            };

            Vector3[] quad2Front = new[]
            {
                new Vector3(-width, -height, width), // Bottom-left
                new Vector3(width, -height, -width), // Bottom-right
                new Vector3(width, height, -width),  // Top-right
                new Vector3(-width, height, width)   // Top-left
            };

            Vector3[] quad2Back = new[]
            {
                quad2Front[0], quad2Front[3], quad2Front[2], quad2Front[1]
            };

            Vector3 normal1 = -Vector3.Forward;
            Vector3 normal2 = -Vector3.Forward;
            Vector3 normalBack1 = Vector3.Forward;
            Vector3 normalBack2 = Vector3.Forward;


            AddBillboardQuad(vertices, indices, position, quad1Front, uv1, uv2, uv3, uv4, normal1, color);
            AddBillboardQuad(vertices, indices, position, quad1Back, uv2, uv3, uv4, uv1, normalBack1, color);
            AddBillboardQuad(vertices, indices, position, quad2Front, uv1, uv2, uv3, uv4, normal2, color);
            AddBillboardQuad(vertices, indices, position, quad2Back, uv2, uv3, uv4, uv1, normalBack2, color);


            return (vertices.ToArray(), indices.ToArray());
        }

        private void AddBillboardQuad(List<Material.VertexPositionNormalTextureColor> vertices, List<short> indices, Vector3 position, Vector3[] offsets, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, Vector3 normal, Color color)
        {
            short startIndex = (short)vertices.Count;

            vertices.Add(new Material.VertexPositionNormalTextureColor(position + offsets[0], normal, uv1, color));
            vertices.Add(new Material.VertexPositionNormalTextureColor(position + offsets[1], normal, uv2, color));
            vertices.Add(new Material.VertexPositionNormalTextureColor(position + offsets[2], normal, uv3, color));
            vertices.Add(new Material.VertexPositionNormalTextureColor(position + offsets[3], normal, uv4, color));

            indices.AddRange(new short[]
            {
        (short)(startIndex + 0), (short)(startIndex + 2), (short)(startIndex + 3),
        (short)(startIndex + 0), (short)(startIndex + 1), (short)(startIndex + 2)
            });
        }



        public (Material.VertexPositionNormalTextureColor[], short[]) CreateVoxel(Vector3 position, Vector3 scale, Face face, int texPos, Color color, int texSize = 16, int atlasSize = 256)
        {
            float width = scale.X / 2;
            float height = scale.Y / 2;
            float depth = scale.Z / 2;

            var vertices = new Material.VertexPositionNormalTextureColor[4];
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
                vertices[0] = new Material.VertexPositionNormalTextureColor(position + offset4, normal, uv4, color);
                vertices[1] = new Material.VertexPositionNormalTextureColor(position + offset3, normal, uv3, color);
                vertices[2] = new Material.VertexPositionNormalTextureColor(position + offset2, normal, uv2, color);
                vertices[3] = new Material.VertexPositionNormalTextureColor(position + offset1, normal, uv1, color);
            }
            else
            {
                vertices[0] = new Material.VertexPositionNormalTextureColor(position + offset1, normal, uv1, color);
                vertices[1] = new Material.VertexPositionNormalTextureColor(position + offset2, normal, uv2, color);
                vertices[2] = new Material.VertexPositionNormalTextureColor(position + offset3, normal, uv3, color);
                vertices[3] = new Material.VertexPositionNormalTextureColor(position + offset4, normal, uv4, color);
            }

            return (vertices, indices);
        }


        private List<Face> GetExposedFaces(int x, int y, int z, short BlockID)
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
                if (IsVisible(x, y - 1, z, BlockID, IsTransparent)) exposedFaces.Add(Face.Bottom);
                if (IsVisible(x, y + 1, z, BlockID, IsTransparent)) exposedFaces.Add(Face.Top);
            }

            if (IsVisible(x + 1, y, z, BlockID, IsTransparent)) exposedFaces.Add(Face.Right);
            if (IsVisible(x - 1, y, z, BlockID, IsTransparent)) exposedFaces.Add(Face.Left);
            if (IsVisible(x, y, z + 1, BlockID, IsTransparent)) exposedFaces.Add(Face.Back);
            if (IsVisible(x, y, z - 1, BlockID, IsTransparent)) exposedFaces.Add(Face.Front);

            return exposedFaces;
        }

        private bool IsVisible(int x, int y, int z, short blockID, bool isTransparent)
        {
            if (x >= 0 && x < ChunkBounds.X && y >= 0 && y < ChunkBounds.Y && z >= 0 && z < ChunkBounds.Z)
            {
                short neighborBlockID = chunkData[x, y, z];

                return neighborBlockID == 0 ||
                       (TransparentLookUp(neighborBlockID) && (neighborBlockID != blockID || !isTransparent));
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

                    short neighborBlockID = neighborChunk.chunkData[localX, localY, localZ];

                    return neighborBlockID == 0 ||
                           (TransparentLookUp(neighborBlockID) && (neighborBlockID != blockID || !isTransparent));
                }
            }

            return false;
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

        public void GenerateTerrain()
        {
            Noise simplexNoise = new Noise();
            int Seed = (int)Transform.Position.X + (int)Transform.Position.Z;

            Task.Run(() =>
            {
                float noiseScale = 0.005f;
                float noiseHeight = 0.1f;
                float WaterLevel = (ChunkBounds.Y / 2) + WaterHeight;
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
                                if (y > height - 1)
                                {
                                    if (y <= WaterLevel)
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
                            if (chunkData[x, y, z] == 1)
                            {
                                if(random.NextDouble() > .9f)
                                {
                                    if(y + 1  < ChunkBounds.Y)
                                    {
                                        chunkData[x, y+1, z] = 11; //Grass
                                    }
                                }
                                else
                                {
                                    PlaceTree(x, y, z, random);
                                }
                            }
                        }
                    }
                }
            });

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

        public void ChunkUpdate()
        {
            float WaterLevel = (ChunkBounds.Y / 2) + WaterHeight;

            Parallel.For(0, (int)ChunkBounds.X, x =>
            {
                for (int z = 0; z < ChunkBounds.Z; z++)
                {
                    for (int y = 0; y < ChunkBounds.Y; y++)
                    {
                        if (chunkData[x, y, z] == 0)
                        {
                            bool hasWaterNeighbor = CheckForWaterNeighbor(x, y, z, WaterLevel);

                            if (hasWaterNeighbor && y <= WaterLevel)
                            {
                                chunkData[x, y, z] = 8;
                            }
                        }
                    }
                }
            });
            Parallel.For(0, (int)ChunkBounds.X, i =>
            {
                int x = (int)ChunkBounds.X - 1 - i;
                for (int z = 0; z < ChunkBounds.Z; z++)
                {
                    for (int y = 0; y < ChunkBounds.Y; y++)
                    {
                        if (chunkData[x, y, z] == 0)
                        {
                            bool hasWaterNeighbor = CheckForWaterNeighbor(x, y, z, WaterLevel);

                            if (hasWaterNeighbor && y <= WaterLevel)
                            {
                                chunkData[x, y, z] = 8;
                            }
                        }
                    }
                }
            });
        }
        private bool CheckForWaterNeighbor(int x, int y, int z, float WaterLevel)
        {
            Vector3[] directions =
            {
                Vector3.Left,
                Vector3.Right,
                Vector3.Down,
                Vector3.Up,
                Vector3.Backward,
                Vector3.Forward
            };

            var waterBlock = 8;
            var chunkSize = ChunkBounds.X;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 direction = directions[i];

                int neighborX = (int)(x + direction.X);
                int neighborY = (int)(y + direction.Y);
                int neighborZ = (int)(z + direction.Z);

                if (neighborX >= 0 && neighborX < chunkSize &&
                    neighborY >= 0 && neighborY < ChunkBounds.Y &&
                    neighborZ >= 0 && neighborZ < chunkSize)
                {
                    if (chunkData[neighborX, neighborY, neighborZ] == waterBlock)
                    {
                        return true;
                    }
                }
                else
                {
                    int wrappedNeighborX = (int)((neighborX + chunkSize) % chunkSize);
                    int wrappedNeighborZ = (int)((neighborZ + chunkSize) % chunkSize);

                    Chunk neighborChunk = GetNeighborChunk(new Vector3(direction.X, direction.Y, direction.Z));
                    if (neighborChunk != null)
                    {
                        if (neighborChunk.chunkData[wrappedNeighborX, neighborY, wrappedNeighborZ] == waterBlock)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private Chunk GetNeighborChunk(Vector3 direction)
        {
            Vector3 neighborChunkPos = Transform.Position + (direction * VoxelSize * ChunkBounds.X);
            if (Manager.ChunkEntities.TryGetValue(neighborChunkPos, out Entity neighborChunkEntity))
            {
                Chunk neighborChunk = ECSManager.Instance.GetComponent<Chunk>(neighborChunkEntity);
                return neighborChunk;
            }

            return null;
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


    }
}
