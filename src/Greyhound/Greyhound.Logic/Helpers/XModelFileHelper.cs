using PhilLibX;
using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Greyhound.Logic
{
    public static class XModelFileHelper
    {
        /// <summary>
        /// Loads surfaces from a version 20 XModel
        /// </summary>
        static void LoadXModelSurfsV20(ModelTemp model, byte[] buffer)
        {
            // Buffer is null if the file wasn't found, etc.
            // If so, we drop back
            if (buffer == null)
                return;

            // To transform the vertices, and for some model formats
            // We need the global bone info
            model.GenerateGlobalBoneData();

            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                // Kill if version doesn't match, must match XModel and Parts
                if (reader.ReadUInt16() != 0x14)
                    throw new Exception("Invalid XModel Surface Version");

                var meshCount = reader.ReadUInt16();

                for (int i = 0; i < meshCount; i++)
                {
                    var tileMode = reader.ReadByte();
                    var vertCount = reader.ReadUInt16();
                    var faceCount = reader.ReadUInt16();
                    var vertListCount = reader.ReadInt16();

                    var defaultBone = 0;

                    if (vertListCount == -1)
                        reader.ReadUInt16();
                    else
                        defaultBone = vertListCount;

                    // Add this index as the material
                    var nMesh = new ModelTemp.Mesh(vertCount, faceCount);
                    nMesh.MaterialIndices.Add(i);

                    for (ushort j = 0; j < vertCount; j++)
                    {
                        var blendCount = 0;
                        var bone = defaultBone;

                        var normal = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle());
                        var colour = new Vector4(
                            reader.ReadByte() / 255.0f,
                            reader.ReadByte() / 255.0f,
                            reader.ReadByte() / 255.0f,
                            reader.ReadByte() / 255.0f);
                        var uv = new Vector2(
                            reader.ReadSingle(),
                            reader.ReadSingle());
                        var biNormal = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle());
                        var tangent = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle());

                        if (vertListCount == -1)
                        {
                            blendCount = reader.ReadByte();
                            bone = reader.ReadUInt16();
                        }

                        var position = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle());

                        var nVertex = new ModelTemp.Vertex(position, normal, tangent);

                        // Add first bone, at 1.0 influence
                        nVertex.Weights.Add(new ModelTemp.Vertex.Weight(bone));

                        if (blendCount > 0)
                        {
                            var unk01 = reader.ReadByte();

                            for (int k = 0; k < blendCount; k++)
                            {
                                var blendBone = reader.ReadUInt16();

                                // Not sure what these are, not used when I tried
                                // zeroing and importing models into CoD 2's Radiant
                                // and the results were the same
                                reader.ReadSingle();
                                reader.ReadSingle();
                                reader.ReadSingle();

                                var weight = reader.ReadUInt16() / 65536.0f;

                                // Subtract from weight 0
                                nVertex.Weights[0].Influence -= weight;
                                nVertex.Weights.Add(new ModelTemp.Vertex.Weight(blendBone, weight));

                            }
                        }

                        // Transform Vertices
                        position = model.Bones[bone].WorldTransform.Rotation.TransformVector(position);
                        normal   = model.Bones[bone].WorldTransform.Rotation.TransformVector(normal);
                        tangent  = model.Bones[bone].WorldTransform.Rotation.TransformVector(tangent);
                        // Move Position
                        position.X += (model.Bones[bone].WorldTransform.Position.X);
                        position.Y += (model.Bones[bone].WorldTransform.Position.Y);
                        position.Z += (model.Bones[bone].WorldTransform.Position.Z);

                        nVertex.Position = position;
                        nVertex.Normal = normal;

                        nVertex.UVs.Add(uv);

                        nMesh.Vertices.Add(nVertex);
                    }

                    for (ushort j = 0; j < faceCount; j++)
                    {
                        // We need to change winding order, 0/2/1
                        var v1 = reader.ReadUInt16();
                        var v2 = reader.ReadUInt16();
                        var v3 = reader.ReadUInt16();

                        nMesh.Faces.Add(new ModelTemp.Face(v1, v3, v2));
                    }

                    model.Meshes.Add(nMesh);
                }
            }
        }

        /// <summary>
        /// Loads joints from a version 20 XModel
        /// </summary>
        static void LoadXModelPartsV20(ModelTemp model, byte[] buffer, Dictionary<string, Vector3> table)
        {
            if (buffer == null)
                return;

            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                var version = reader.ReadUInt16();
                var childBones = reader.ReadUInt16();
                var rootBones = reader.ReadUInt16();
                var boneCount = childBones + rootBones;

                var quats = new Quaternion[boneCount];
                var trans = new Vector3[boneCount];
                var parents = new int[boneCount];

                // Fill with defaults
                for (int i = 0; i < rootBones; i++)
                {
                    quats[i].X = 0.0f;
                    quats[i].Y = 0.0f;
                    quats[i].Z = 0.0f;
                    quats[i].W = 1.0f;

                    trans[i].X = 0.0f;
                    trans[i].Y = 0.0f;
                    trans[i].Z = 0.0f;

                    parents[i] = -1;
                }

                // Fill with defaults
                for (int i = 0; i < childBones; i++)
                {
                    parents[i + rootBones] = reader.ReadByte();

                    trans[i + rootBones].X = reader.ReadSingle();
                    trans[i + rootBones].Y = reader.ReadSingle();
                    trans[i + rootBones].Z = reader.ReadSingle();

                    quats[i + rootBones].X = (reader.ReadInt16() / 32768.0f);
                    quats[i + rootBones].Y = (reader.ReadInt16() / 32768.0f);
                    quats[i + rootBones].Z = (reader.ReadInt16() / 32768.0f);
                    quats[i + rootBones].W = (float)Math.Sqrt(1 -
                        (quats[i + rootBones].X * quats[i + rootBones].X) -
                        (quats[i + rootBones].Y * quats[i + rootBones].Y) -
                        (quats[i + rootBones].Z * quats[i + rootBones].Z));
                }

                for (int i = 0; i < boneCount; i++)
                {
                    var nBone = new ModelTemp.Bone(
                        reader.ReadNullTerminatedString(),
                        parents[i],
                        new ModelTemp.BoneTransform(trans[i], quats[i]),
                        new ModelTemp.BoneTransform(trans[i], quats[i]));

                    if (table.TryGetValue(nBone.Name, out var vec))
                        nBone.LocalTransform.Position = vec / 2.54f;

                    if (string.IsNullOrWhiteSpace(nBone.Name))
                        nBone.Name = "tag_" + i.ToString();

                    model.Bones.Add(nBone);
                }
            }
        }

        /// <summary>
        /// Loads joints from a version 14 XModel
        /// </summary>
        static void LoadXModelPartsV14(ModelTemp model, byte[] buffer, Dictionary<string, Vector3> table)
        {
            if (buffer == null)
                return;

            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                var version = reader.ReadUInt16();
                var childBones = reader.ReadUInt16();
                var rootBones = reader.ReadUInt16();
                var boneCount = childBones + rootBones;

                var quats = new Quaternion[boneCount];
                var trans = new Vector3[boneCount];
                var parents = new int[boneCount];

                // Fill with defaults
                for (int i = 0; i < rootBones; i++)
                {
                    quats[i].X = 0.0f;
                    quats[i].Y = 0.0f;
                    quats[i].Z = 0.0f;
                    quats[i].W = 1.0f;

                    trans[i].X = 0.0f;
                    trans[i].Y = 0.0f;
                    trans[i].Z = 0.0f;

                    parents[i] = -1;
                }

                // Fill with defaults
                for (int i = 0; i < childBones; i++)
                {
                    parents[i + rootBones] = reader.ReadByte();

                    trans[i + rootBones].X = reader.ReadSingle();
                    trans[i + rootBones].Y = reader.ReadSingle();
                    trans[i + rootBones].Z = reader.ReadSingle();

                    quats[i + rootBones].X = (reader.ReadInt16() / 32768.0f);
                    quats[i + rootBones].Y = (reader.ReadInt16() / 32768.0f);
                    quats[i + rootBones].Z = (reader.ReadInt16() / 32768.0f);
                    quats[i + rootBones].W = (float)Math.Sqrt(1 -
                        (quats[i + rootBones].X * quats[i + rootBones].X) -
                        (quats[i + rootBones].Y * quats[i + rootBones].Y) -
                        (quats[i + rootBones].Z * quats[i + rootBones].Z));
                }

                for (int i = 0; i < boneCount; i++)
                {
                    var nBone = new ModelTemp.Bone(
                        reader.ReadNullTerminatedString(),
                        parents[i],
                        new ModelTemp.BoneTransform(trans[i], quats[i]),
                        new ModelTemp.BoneTransform(trans[i], quats[i]));

                    if (table.TryGetValue(nBone.Name, out var vec))
                        nBone.LocalTransform.Position = vec / 2.54f;

                    if (string.IsNullOrWhiteSpace(nBone.Name))
                        nBone.Name = "tag_" + i.ToString();
                    reader.BaseStream.Position += 24;

                    model.Bones.Add(nBone);
                }
            }
        }

        /// <summary>
        /// Loads faces from a version 14 XModel
        /// </summary>
        static void LoadFacesV14(ModelTemp.Mesh mesh, BinaryReader reader, int faceCount)
        {
            while (true)
            {
                var indexCount = reader.ReadByte();

                var index1 = reader.ReadUInt16();
                var index2 = reader.ReadUInt16();
                var index3 = reader.ReadUInt16();

                if (index1 != index2 && index1 != index3 && index2 != index3)
                {
                    mesh.Faces.Add(new ModelTemp.Face(index1, index3, index2));
                }

                int v11;
                for (int i = 3; i < indexCount; i = v11 + 1)
                {
                    var index4 = index3;
                    var index5 = reader.ReadUInt16();

                    if (index4 != index2 && index4 != index5 && index2 != index5)
                    {
                        mesh.Faces.Add(new ModelTemp.Face(index4, index5, index2));
                    }

                    v11 = i + 1;
                    if (v11 >= indexCount)
                        break;

                    index2 = index5;
                    index3 = reader.ReadUInt16();

                    if (index4 != index2 && index4 != index3 && index2 != index3)
                    {
                        mesh.Faces.Add(new ModelTemp.Face(index4, index3, index2));
                    }
                }


                if (mesh.Faces.Count >= faceCount)
                    break;
            }
        }

        /// <summary>
        /// Loads surfaces from a version 20 XModel
        /// </summary>
        static void LoadXModelSurfsV14(ModelTemp model, byte[] buffer)
        {
            if (buffer == null)
                return;

            model.GenerateGlobalBoneData();

            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                var version = reader.ReadUInt16();
                var meshCount = reader.ReadUInt16();

                for (int i = 0; i < meshCount; i++)
                {
                    var tileMode = reader.ReadByte();
                    var vertCount = reader.ReadInt16();
                    var faceCount = reader.ReadUInt16();
                    var unk01 = reader.ReadUInt16();
                    var vertListCount = reader.ReadInt16();
                    var defaultBone = 0;
                    var weightCount = 0;

                    if (vertListCount == -1)
                    {
                        weightCount = reader.ReadUInt16();
                        reader.ReadUInt16();
                    }
                    else
                    {
                        defaultBone = vertListCount;
                    }

                    var boneCounts = new int[vertCount];

                    var nMesh = new ModelTemp.Mesh(vertCount, faceCount);

                    nMesh.MaterialIndices.Add(i);

                    LoadFacesV14(nMesh, reader, faceCount);

                    for (ushort j = 0; j < vertCount; j++)
                    {
                        // Get Counts
                        var blendCount = 0;
                        var bone = defaultBone;

                        var normal = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle());
                        var uv = new Vector2(
                            reader.ReadSingle(),
                            reader.ReadSingle());

                        // Check if we have skin weights
                        if (vertListCount == -1)
                        {
                            blendCount = reader.ReadUInt16();
                            bone = reader.ReadUInt16();
                        }

                        var position = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle());

                        if (blendCount != 0)
                            reader.ReadSingle();

                        // Add to blend counts, since they are stored after the vertices
                        boneCounts[j] = blendCount;

                        // Create and add weight
                        var vertex = new ModelTemp.Vertex(position, normal, Vector3.Zero, uv);

                        // Get base bone
                        var baseBone = model.Bones[bone];

                        // Transform the vertex back into its original position
                        vertex.Position    = baseBone.WorldTransform.Rotation.TransformVector(position);
                        vertex.Normal      = baseBone.WorldTransform.Rotation.TransformVector(normal);
                        vertex.Position   += model.Bones[bone].WorldTransform.Position;

                        // Add data
                        vertex.Weights.Add(new ModelTemp.Vertex.Weight(bone));
                        nMesh.Vertices.Add(vertex);
                    }

                    for (ushort j = 0; j < vertCount; j++)
                    {
                        for (int k = 0; k < boneCounts[j]; k++)
                        {
                            var blendBone = reader.ReadUInt16();
                            reader.BaseStream.Position += 12;
                            var weight = reader.ReadSingle();

                            // Subtract from this weight
                            //nMesh.Vertices[j].Weights[0].Influence -= weight;
                            //nMesh.Vertices[j].Weights.Add(new ModelT.Vertex.Weight(blendBone, weight));
                        }
                    }

                    model.Meshes.Add(nMesh);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="model"></param>
        static void ConvertV14(BinaryReader reader, ModelAsset model)
        {
            // Skip min/max
            var minMax = reader.ReadBytes(24);

            model.LODs = new List<ModelTemp>();
            model.Materials = new Dictionary<string, MaterialAsset>();

            string partsName = "";

            // XModels store 4 slots, regardless if used
            for(int i = 0; i < 3; i++)
            {
                var distance = reader.ReadSingle();
                var name = reader.ReadNullTerminatedString();

                // Check
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                // Only take parts from first lod
                if (i == 0)
                    partsName = "xmodelparts/" + name;

                var surfs = "xmodelsurfs/" + name;

                var lod = new ModelTemp();

                LoadXModelPartsV14(lod, Instance.ExtractPackageEntry(partsName, -1), new Dictionary<string, Vector3>());
                LoadXModelSurfsV14(lod, Instance.ExtractPackageEntry("xmodelsurfs/" + name, -1));

                model.LODs.Add(lod);
            }

            // Skip col info
            var v1 = reader.ReadUInt32();
            var v2 = reader.ReadUInt32();

            // Read materials, each mesh must have the same amount
            // For CoD 1, there is only a color map, nothing else
            var mtlCount = reader.ReadUInt16();

            for(int i = 0; i < mtlCount; i++)
            {
                var name = reader.ReadNullTerminatedString();

                var mtl = new MaterialAsset()
                {
                    Name        = Path.GetFileNameWithoutExtension(name).Replace("@", ""),
                    Information = string.Format("N/A"),
                    LoadMethod  = null,
                    Data        = name,
                };

                // Add default image
                mtl.ImageSlots["colorMap"] = new ImageAsset()
                {
                    Name = name,
                    Type = "sound",
                    Information = "N/A",
                    LoadMethod = IWDPackage.LoadIWDImage,
                    Data = Instance.FindPackageEntry(name)
                };

                model.Materials[mtl.Name] = mtl;

                // Add to lods
                foreach (var lod in model.LODs)
                    lod.Materials.Add(new ModelTemp.Material(mtl.Name));
            }
        }

        static void ConvertV20(BinaryReader reader, ModelAsset model)
        {
            // Skip min/max
            var flags = reader.ReadByte();
            var minMax = reader.ReadBytes(24);

            model.LODs = new List<ModelTemp>();
            model.Materials = new Dictionary<string, MaterialAsset>();

            // XModels store 4 slots, regardless if used
            for (int i = 0; i < 4; i++)
            {
                var distance = reader.ReadSingle();
                var name = reader.ReadNullTerminatedString().ToLower();

                // Check
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                model.LODs.Add(new ModelTemp(name));

                // Use first lod for parts, everything else "copies" from it
                // For CoD 1/2 we also need to use a table for viewhands, since like newer cods, viewmodel translates are at 0
                // but for CoD 1/2, not base mat table is stored, and we need the translations to transform the vertices
                // As far as I know, if we don't have this table, the expected result is correct, as it is identical to importing viewhands into Radiant
                LoadXModelPartsV20(model.LODs[i], Instance.ExtractPackageEntry("xmodelparts/" + model.LODs[0].Name, -1), name.Contains("viewmodel_hands") ? new Dictionary<string, Vector3>()
                {
                    { "tag_view",               new Vector3(0f, 0f, 0f) },
                    { "tag_torso",              new Vector3(-11.76486f, 0f, -3.497466f) },
                    { "j_shoulder_le",          new Vector3(2.859542f, 20.16072f, -4.597286f) },
                    { "j_elbow_le",             new Vector3(30.7185f, -8E-06f, 3E-06f) },
                    { "j_wrist_le",             new Vector3(29.3906f, 1.9E-05f, -3E-06f) },
                    { "j_thumb_le_0",           new Vector3(2.786345f, 2.245192f, 0.85161f) },
                    { "j_thumb_le_1",           new Vector3(4.806596f, -1E-06f, 3E-06f) },
                    { "j_thumb_le_2",           new Vector3(2.433519f, -2E-06f, 1E-06f) },
                    { "j_thumb_le_3",           new Vector3(3f, -1E-06f, -1E-06f) },
                    { "j_flesh_le",             new Vector3(4.822557f, 1.176307f, -0.110341f) },
                    { "j_index_le_0",           new Vector3(10.53435f, 2.786251f, -3E-06f) },
                    { "j_index_le_1",           new Vector3(4.563f, -3E-06f, 1E-06f) },
                    { "j_index_le_2",           new Vector3(2.870304f, 3E-06f, -2E-06f) },
                    { "j_index_le_3",           new Vector3(2.999999f, 4E-06f, 1E-06f) },
                    { "j_mid_le_0",             new Vector3(10.71768f, 0.362385f, -0.38647f) },
                    { "j_mid_le_1",             new Vector3(4.842623f, -1E-06f, -1E-06f) },
                    { "j_mid_le_2",             new Vector3(2.957112f, -1E-06f, -1E-06f) },
                    { "j_mid_le_3",             new Vector3(3.000005f, 4E-06f, 0f) },
                    { "j_ring_le_0",            new Vector3(9.843364f, -1.747671f, -0.401116f) },
                    { "j_ring_le_1",            new Vector3(4.842618f, 4E-06f, -3E-06f) },
                    { "j_ring_le_2",            new Vector3(2.755294f, -2E-06f, 5E-06f) },
                    { "j_ring_le_3",            new Vector3(2.999998f, -2E-06f, -4E-06f) },
                    { "j_pinky_le_0",           new Vector3(8.613766f, -3.707476f, 0.16818f) },
                    { "j_pinky_le_1",           new Vector3(3.942609f, 1E-06f, 1E-06f) },
                    { "j_pinky_le_2",           new Vector3(1.794117f, 3E-06f, -3E-06f) },
                    { "j_pinky_le_3",           new Vector3(2.83939f, -1E-06f, 4E-06f) },
                    { "j_wristtwist_le",        new Vector3(21.60379f, 1.2E-05f, -3E-06f) },
                    { "j_shoulder_ri",          new Vector3(2.859542f, -20.16072f, -4.597286f) },
                    { "j_elbow_ri",             new Vector3(-30.71852f, 4E-06f, -2.4E-05f) },
                    { "j_wrist_ri",             new Vector3(-29.39067f, 4.4E-05f, 2.2E-05f) },
                    { "j_thumb_ri_0",           new Vector3(-2.786155f, -2.245166f, -0.851634f) },
                    { "j_thumb_ri_1",           new Vector3(-4.806832f, -6.6E-05f, 0.000141f) },
                    { "j_thumb_ri_2",           new Vector3(-2.433458f, -3.8E-05f, -5.3E-05f) },
                    { "j_thumb_ri_3",           new Vector3(-3.000123f, 0.00016f, 2.5E-05f) },
                    { "j_flesh_ri",             new Vector3(-4.822577f, -1.176315f, 0.110318f) },
                    { "j_index_ri_0",           new Vector3(-10.53432f, -2.786281f, -7E-06f) },
                    { "j_index_ri_1",           new Vector3(-4.562927f, -5.8E-05f, 5.4E-05f) },
                    { "j_index_ri_2",           new Vector3(-2.870313f, -6.5E-05f, 0.0001f) },
                    { "j_index_ri_3",           new Vector3(-2.999938f, 0.000165f, -6.5E-05f) },
                    { "j_mid_ri_0",             new Vector3(-10.71752f, -0.362501f, 0.386463f) },
                    { "j_mid_ri_1",             new Vector3(-4.842728f, 0.000151f, 2.8E-05f) },
                    { "j_mid_ri_2",             new Vector3(-2.957152f, -8.7E-05f, -2.2E-05f) },
                    { "j_mid_ri_3",             new Vector3(-3.00006f, -6.8E-05f, -1.9E-05f) },
                    { "j_ring_ri_0",            new Vector3(-9.843175f, 1.747613f, 0.401109f) },
                    { "j_ring_ri_1",            new Vector3(-4.842774f, 0.000176f, -6.3E-05f) },
                    { "j_ring_ri_2",            new Vector3(-2.755269f, -1.1E-05f, 0.000149f) },
                    { "j_ring_ri_3",            new Vector3(-3.000048f, -4.1E-05f, -4.9E-05f) },
                    { "j_pinky_ri_0",           new Vector3(-8.613756f, 3.707438f, -0.168202f) },
                    { "j_pinky_ri_1",           new Vector3(-3.942537f, -0.000117f, -6.5E-05f) },
                    { "j_pinky_ri_2",           new Vector3(-1.794038f, 0.000134f, 0.000215f) },
                    { "j_pinky_ri_3",           new Vector3(-2.839375f, 5.6E-05f, -0.000115f) },
                    { "j_wristtwist_ri",        new Vector3(-21.60388f, 9.7E-05f, 8E-06f) },
                    { "tag_weapon",             new Vector3(38.5059f, 0f, -17.15191f) },
                    { "tag_cambone",            new Vector3(0f, 0f, 0f) },
                    { "tag_camera",             new Vector3(0f, 0f, 0f) },
                } : new Dictionary<string, Vector3>());
                LoadXModelSurfsV20(model.LODs[i], Instance.ExtractPackageEntry("xmodelsurfs/" + name, -1));
            }

            // Skip col info
            var v1 = reader.ReadUInt32();
            var v2 = reader.ReadUInt32();

            // Read materials, each mesh must have the same amount
            // For CoD 1, there is only a color map, nothing else
            var mtlCount = reader.ReadUInt16();

            for (int i = 0; i < mtlCount; i++)
            {
                var name = reader.ReadNullTerminatedString();

                var mtl = new MaterialAsset()
                {
                    Name = Path.GetFileNameWithoutExtension(name).Replace("@", ""),
                    Information = string.Format("N/A"),
                    LoadMethod = IWDPackage.LoadIWDMaterial,
                    Data = Instance.FindPackageEntry(name),
                };

                model.Materials[mtl.Name] = mtl;

                // Add to lods
                foreach (var lod in model.LODs)
                    lod.Materials.Add(new ModelTemp.Material(mtl.Name));
            }
        }

        public static void Convert(byte[] buffer, ModelAsset model)
        {
            using(var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                var version = reader.ReadUInt16();

                switch(version)
                {
                    case 0xE:
                        ConvertV14(reader, model);
                        return;
                    case 0x14:
                        ConvertV20(reader, model);
                        return;
                    default:
                        throw new Exception("Invalid XModel File Version");
                }
            }
        }
    }
}
