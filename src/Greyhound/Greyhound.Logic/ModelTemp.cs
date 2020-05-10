// ------------------------------------------------------------------------
// PhilLibX - My Utility Library
// Copyright(c) 2018 Philip/Scobalula
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// ------------------------------------------------------------------------
// File: Model.cs
// Author: Philip/Scobalula
// Description: A class to hold a 3-D Model and perform operations such as export, etc. on it
using Greyhound.Logic;
using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

// TODO: Move over to new utility library

namespace PhilLibX
{
    /// <summary>
    /// A class to hold a 3-D Model
    /// </summary>
    public class ModelTemp
    {
        /// <summary>
        /// A class to hold a bone transform
        /// </summary>
        public class BoneTransform
        {
            /// <summary>
            /// Gets or Sets the Position of the Bone
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// Gets or Sets the Rotation of the Bone
            /// </summary>
            public Quaternion Rotation { get; set; }

            /// <summary>
            /// Gets or Sets the Scale of the Bone
            /// </summary>
            public Vector3 Scale { get; set; }

            /// <summary>
            /// Creates a Bone Transform with default values
            /// </summary>
            public BoneTransform()
            {
                Position = Vector3.Zero;
                Rotation = Quaternion.Identity;
                Scale    = Vector3.One;
            }

            /// <summary>
            /// Creates a Bone Transform with the given position and rotation
            /// </summary>
            /// <param name="pos">Position of the bone</param>
            /// <param name="rot">Rotation of the bone</param>
            public BoneTransform(Vector3 pos, Quaternion rot)
            {
                Position = pos;
                Rotation = rot;
                Scale    = Vector3.One;
            }

            /// <summary>
            /// Creates a Bone Transform with the given position and rotation and scale
            /// </summary>
            /// <param name="pos">Position of the bone</param>
            /// <param name="rot">Rotation of the bone</param>
            /// <param name="scale">Scale of the bone</param>
            public BoneTransform(Vector3 pos, Quaternion rot, Vector3 scale)
            {
                Position = pos;
                Rotation = rot;
                Scale    = scale;
            }
        }

        /// <summary>
        /// A class to hold a Bone
        /// </summary>
        public class Bone
        {
            /// <summary>
            /// Gets or Sets the name of the bone
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or Sets the index of the parent bone
            /// </summary>
            public int ParentIndex { get; set; }

            /// <summary>
            /// Gets or Sets the transform relative to the parent
            /// </summary>
            public BoneTransform LocalTransform { get; set; }

            /// <summary>
            /// Gets or Sets the transform relative to the parent
            /// </summary>
            public BoneTransform WorldTransform { get; set; }

            /// <summary>
            /// Creates a new Bone with the given data
            /// </summary>
            /// <param name="name">Bone Name</param>
            public Bone(string name)
            {
                Name = name;
                ParentIndex = -1;
                LocalTransform = new BoneTransform();
                WorldTransform = new BoneTransform();
            }

            /// <summary>
            /// Creates a new Bone with the given data
            /// </summary>
            /// <param name="name">Bone Name</param>
            /// <param name="parent">Bone Parent</param>
            /// <param name="localTransform">Local Transform</param>
            public Bone(string name, int parent, BoneTransform localTransform)
            {
                Name = name;
                ParentIndex = parent;
                LocalTransform = new BoneTransform(localTransform.Position, localTransform.Rotation, localTransform.Scale);
                WorldTransform = new BoneTransform();
            }

            /// <summary>
            /// Creates a new Bone with the given data
            /// </summary>
            /// <param name="name">Bone Name</param>
            /// <param name="parent">Bone Parent</param>
            /// <param name="localTransform">Local Transform</param>
            /// <param name="worldTransform">World Transform</param>
            public Bone(string name, int parent, BoneTransform localTransform, BoneTransform worldTransform)
            {
                Name = name;
                ParentIndex = parent;
                LocalTransform = new BoneTransform(localTransform.Position, localTransform.Rotation, localTransform.Scale);
                WorldTransform = new BoneTransform(worldTransform.Position, worldTransform.Rotation, worldTransform.Scale);
            }

            /// <summary>
            /// Sorts the bone by hierarchy
            /// </summary>
            /// <param name="sorted">List sorted by parents</param>
            /// <param name="source">Original list</param>
            /// <param name="results">Output list with sorted bones</param>
            public void HierarchicalSort(List<Bone> sorted, List<Bone> source, List<Bone> results)
            {
                results.Add(this);

                foreach (var bone in sorted)
                {
                    if (bone.ParentIndex > -1)
                    {
                        if (source[bone.ParentIndex] == this)
                        {
                            bone.HierarchicalSort(sorted, source, results);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A class to hold a vertex
        /// </summary>
        public class Vertex
        {
            /// <summary>
            /// A class to hold a vertex weight
            /// </summary>
            public class Weight
            {
                /// <summary>
                /// Gets or Sets the bone index
                /// </summary>
                public int BoneIndex { get; set; }

                /// <summary>
                /// Gets or Sets the Influence
                /// </summary>
                public float Influence { get; set; }

                public Weight(int boneIndex)
                {
                    BoneIndex = boneIndex;
                    Influence = 1.0f;
                }

                public Weight(int boneIndex, float influence)
                {
                    BoneIndex = boneIndex;
                    Influence = influence;
                }
            }

            /// <summary>
            /// Gets or Sets the vertex position
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// Gets or Sets the vertex normal
            /// </summary>
            public Vector3 Normal { get; set; }

            /// <summary>
            /// Gets or Sets the vertex tangent
            /// </summary>
            public Vector3 Tangent { get; set; }

            /// <summary>
            /// Gets or Sets the vertex color
            /// </summary>
            public Vector4 Color { get; set; }

            /// <summary>
            /// Gets or Sets the vertex uv sets
            /// </summary>
            public List<Vector2> UVs { get; set; }

            /// <summary>
            /// Gets or Sets the vertex weights
            /// </summary>
            public List<Weight> Weights { get; set; }

            /// <summary>
            /// Creates a new vertex
            /// </summary>
            public Vertex()
            {
                Position = new Vector3(0.0f, 0.0f, 0.0f);
                Normal = new Vector3(0.0f, 0.0f, 0.0f);
                Tangent = new Vector3(0.0f, 0.0f, 0.0f);
                Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                Weights = new List<Weight>(8);
                UVs = new List<Vector2>(4);
            }

            /// <summary>
            /// Creates a new vertex
            /// </summary>
            /// <param name="position">Position</param>
            public Vertex(Vector3 position)
            {
                Position = position;
                Normal = new Vector3(0.0f, 0.0f, 0.0f);
                Tangent = new Vector3(0.0f, 0.0f, 0.0f);
                Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                Weights = new List<Weight>(8);
                UVs = new List<Vector2>(4);
            }

            /// <summary>
            /// Creates a new vertex
            /// </summary>
            /// <param name="position">Position</param>
            /// <param name="normal">Normal</param>
            public Vertex(Vector3 position, Vector3 normal)
            {
                Position = position;
                Normal   = normal;
                Tangent  = new Vector3(0.0f, 0.0f, 0.0f);
                Color    = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                Weights  = new List<Weight>(8);
                UVs      = new List<Vector2>(8);
            }

            /// <summary>
            /// Creates a new vertex
            /// </summary>
            /// <param name="position">Position</param>
            /// <param name="normal">Normal</param>
            /// <param name="tangent">Tangent</param>
            public Vertex(Vector3 position, Vector3 normal, Vector3 tangent)
            {
                Position = position;
                Normal = normal;
                Tangent = tangent;
                Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                Weights = new List<Weight>(8);
                UVs = new List<Vector2>(8);
            }

            /// <summary>
            /// Creates a new vertex
            /// </summary>
            /// <param name="position">Position</param>
            /// <param name="normal">Normal</param>
            /// <param name="tangent">Tangent</param>
            public Vertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 uv)
            {
                Position = position;
                Normal = normal;
                Tangent = tangent;
                Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                Weights = new List<Weight>(8);
                UVs = new List<Vector2>(8)
                {
                    uv
                };
            }

            public void NormalizeWeights()
            {
                var weightSum = 0.0f;

                foreach (var weight in Weights)
                    weightSum += weight.Influence;

                var multiplier = 1.0f / weightSum;

                foreach (var weight in Weights)
                    weightSum *= multiplier;
            }
        }

        /// <summary>
        /// A class to hold a face with multiple vertex indices
        /// </summary>
        public class Face
        {
            /// <summary>
            /// Gets or Sets the face indices
            /// </summary>
            public int[] Indices { get; set; }

            /// <summary>
            /// Creates a new face with the given number of vertices
            /// </summary>
            /// <param name="vertexCount">Vertex count</param>
            public Face(int vertexCount)
            {
                Indices = new int[vertexCount];
            }

            /// <summary>
            /// Creates a new polygon face with the given vertex indices
            /// </summary>
            /// <param name="v1">Index 1</param>
            /// <param name="v2">Index 2</param>
            /// <param name="v3">Index 3</param>
            public Face(int v1, int v2, int v3)
            {
                Indices = new int[3];

                Indices[0] = v1;
                Indices[1] = v2;
                Indices[2] = v3;
            }
        }

        /// <summary>
        /// A class to hold a mesh
        /// </summary>
        public class Mesh
        {
            /// <summary>
            /// Gets or Sets the vertices
            /// </summary>
            public List<Vertex> Vertices { get; set; }

            /// <summary>
            /// Gets or Sets the faces
            /// </summary>
            public List<Face> Faces { get; set; }

            /// <summary>
            /// Gets or Sets the material indices
            /// </summary>
            public List<int> MaterialIndices { get; set; }

            /// <summary>
            /// Creates a new mesh
            /// </summary>
            public Mesh()
            {
                Vertices = new List<Vertex>();
                Faces = new List<Face>();
                MaterialIndices = new List<int>();
            }

            /// <summary>
            /// Creates a new mesh and preallocates the data counts
            /// </summary>
            /// <param name="vertexCount">Number of vertices</param>
            /// <param name="faceCount">Number of faces</param>
            public Mesh(int vertexCount, int faceCount)
            {
                Vertices = new List<Vertex>(vertexCount);
                Faces = new List<Face>(faceCount);
                MaterialIndices = new List<int>();
            }
        }

        /// <summary>
        /// A class to hold a material
        /// </summary>
        public class Material
        {
            /// <summary>
            /// Gets or Sets the name of the material
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or Sets the material images
            /// </summary>
            public Dictionary<string, string> Images { get; set; }

            /// <summary>
            /// Gets or Sets the material settings
            /// </summary>
            public Dictionary<string, object> Settings { get; set; }

            /// <summary>
            /// Creates a new material with the given name
            /// </summary>
            /// <param name="name">Material name</param>
            public Material(string name)
            {
                Name = name;
                Images = new Dictionary<string, string>();
                Settings = new Dictionary<string, object>();
            }

            /// <summary>
            /// Gets the image of the given type
            /// </summary>
            /// <param name="key">Image Key/Type</param>
            /// <returns>Resulting Images</returns>
            public string GetImage(string key)
            {
                return Images.TryGetValue(key, out var image) ? image : "";
            }

            /// <summary>
            /// Gets the name of the material as a string representation of it
            /// </summary>
            /// <returns>Material name</returns>
            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Gets or Sets the Model Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the bones/joints
        /// </summary>
        public List<Bone> Bones { get; set; }

        /// <summary>
        /// Gets or Sets the meshes
        /// </summary>
        public List<Mesh> Meshes { get; set; }

        /// <summary>
        /// Gets or Sets the materials
        /// </summary>
        public List<Material> Materials { get; set; }

        /// <summary>
        /// Creates a new Model
        /// </summary>
        public ModelTemp()
        {
            Bones = new List<Bone>();
            Meshes = new List<Mesh>();
            Materials = new List<Material>();
        }

        /// <summary>
        /// Creates a new Model
        /// </summary>
        /// <param name="name">Model Name</param>
        public ModelTemp(string name)
        {
            Name = name;
            Bones = new List<Bone>();
            Meshes = new List<Mesh>();
            Materials = new List<Material>();
        }

        /// <summary>
        /// 
        /// </summary>
        public void HierarchicalSort()
        {
            if (Bones.Count > 0)
            {
                // We have to again sort by bone parents so we're processing parents before children
                var sorted = new List<Bone>(Bones.Count);
                var input = Bones.OrderBy(x => x.ParentIndex).ToList();

                // Loop and find parent bones
                foreach (var bone in input)
                {
                    if (bone.ParentIndex != -1)
                        break;

                    bone.HierarchicalSort(input, Bones, sorted);
                }


                // Remap parents
                foreach (var bone in sorted)
                    if (bone.ParentIndex > -1)
                        bone.ParentIndex = sorted.IndexOf(Bones[bone.ParentIndex]);

                // Remap weights
                foreach (var mesh in Meshes)
                    foreach (var vtx in mesh.Vertices)
                        foreach (var weight in vtx.Weights)
                            weight.BoneIndex = sorted.IndexOf(Bones[weight.BoneIndex]);

                Bones = sorted;
            }
        }

        /// <summary>
        /// Generates global positions and rotations
        /// </summary>
        public void GenerateGlobalBoneData(bool requiresSort = false)
        {
            // We must sort by hierarchy to ensure parents are processed before children
            if (requiresSort)
                HierarchicalSort();

            foreach (var bone in Bones)
            {
                if (bone.ParentIndex > -1)
                {
                    bone.WorldTransform.Position = Bones[bone.ParentIndex].WorldTransform.Position + Bones[bone.ParentIndex].WorldTransform.Rotation.TransformVector(bone.LocalTransform.Position);
                    bone.WorldTransform.Rotation = Bones[bone.ParentIndex].WorldTransform.Rotation * bone.LocalTransform.Rotation;
                }
                else
                {
                    bone.WorldTransform.Position = bone.LocalTransform.Position;
                    bone.WorldTransform.Rotation = bone.LocalTransform.Rotation;
                }
            }
        }

        /// <summary>
        /// Generates local positions and rotations
        /// </summary>
        public void GenerateLocalBoneData(bool requiresSort = false)
        {
            // We must sort by hierarchy to ensure parents are processed before children
            if (requiresSort)
                HierarchicalSort();

            foreach (var bone in Bones)
            {
                if (bone.ParentIndex > -1)
                {
                    bone.LocalTransform.Position = Quaternion.Conjugate(Bones[bone.ParentIndex].WorldTransform.Rotation).TransformVector(bone.WorldTransform.Position - Bones[bone.ParentIndex].WorldTransform.Position);
                    bone.LocalTransform.Rotation = Quaternion.Conjugate(Bones[bone.ParentIndex].WorldTransform.Rotation) * bone.WorldTransform.Rotation;
                }
                else
                {
                    bone.LocalTransform.Position = bone.WorldTransform.Position;
                    bone.LocalTransform.Rotation = bone.WorldTransform.Rotation;
                }
            }
        }

        /// <summary>
        /// Scales the model by the given value
        /// </summary>
        /// <param name="value">Value to scale the model by</param>
        public void Scale(float value)
        {
            if (value == 1.0f)
                return;

            foreach (var bone in Bones)
            {
                bone.LocalTransform.Position *= value;
                bone.WorldTransform.Position *= value;
            }

            foreach (var mesh in Meshes)
            {
                foreach (var vertex in mesh.Vertices)
                {
                    vertex.Position *= value;
                }
            }
        }

        /// <summary>
        /// Checks if the model contains a bone
        /// </summary>
        /// <param name="bone">Bone to locate</param>
        /// <returns>True if the bone exists, otherwise false</returns>
        public bool HasBone(string bone)
        {
            return Bones.Find(x => x.Name == bone) != null;
        }
    }
}