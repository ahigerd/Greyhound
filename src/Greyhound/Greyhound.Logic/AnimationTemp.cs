using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

// TODO: Use new library from new PhilLibX

namespace PhilLibX
{
    /// <summary>
    /// A class to hold a 3-D Animation
    /// </summary>
    public class AnimationTemp
    {
        /// <summary>
        /// Animation/Bone Data Types
        /// </summary>
        public enum DataType
        {
            /// <summary>
            /// Animation Data is same as other bones (for bones that will match the other bones)
            /// </summary>
            None,

            /// <summary>
            /// Animation Data is relative to zero
            /// </summary>
            Absolute,

            /// <summary>
            /// Animation Data is relative to parent bind pose
            /// </summary>
            Relative,

            /// <summary>
            /// Animation Data is applied to existing animation data in the scene
            /// </summary>
            Additive,
        }

        /// <summary>
        /// A class to hold an Animation Bone
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
            /// Gets or Sets the data type for this bone
            /// </summary>
            public DataType Type { get; set; }

            /// <summary>
            /// Gets or Sets the translation keys
            /// </summary>
            public SortedDictionary<int, Vector3> Translations { get; set; }

            /// <summary>
            /// Gets or Sets the rotation keys
            /// </summary>
            public SortedDictionary<int, Quaternion> Rotations { get; set; }

            /// <summary>
            /// Gets or Sets the rotation keys
            /// </summary>
            public SortedDictionary<int, Vector3> Scales { get; set; }

            /// <summary>
            /// Creates a new animation bone
            /// </summary>
            /// <param name="name">Name of the bone</param>
            public Bone(string name)
            {
                Name = name;
                ParentIndex = -2; // < -1 to indicate no parent data to halt formats that need it
                Type = DataType.None;

                Translations = new SortedDictionary<int, Vector3>();
                Rotations = new SortedDictionary<int, Quaternion>();
                Scales = new SortedDictionary<int, Vector3>();
            }

            /// <summary>
            /// Creates a new animation bone
            /// </summary>
            /// <param name="name">Name of the bone</param>
            /// <param name="parentIndex">Parent index</param>
            public Bone(string name, int parentIndex)
            {
                Name = name;
                ParentIndex = parentIndex;
                Type = DataType.None;

                Translations = new SortedDictionary<int, Vector3>();
                Rotations = new SortedDictionary<int, Quaternion>();
                Scales = new SortedDictionary<int, Vector3>();
            }

            /// <summary>
            /// Creates a new animation bone
            /// </summary>
            /// <param name="name">Name of the bone</param>
            /// <param name="type">Bone data type</param>
            public Bone(string name, DataType type)
            {
                Name = name;
                ParentIndex = -2; // < -1 to indicate no parent data to halt formats that need it
                Type = type;

                Translations = new SortedDictionary<int, Vector3>();
                Rotations = new SortedDictionary<int, Quaternion>();
                Scales = new SortedDictionary<int, Vector3>();
            }

            /// <summary>
            /// Creates a new animation bone
            /// </summary>
            /// <param name="name">Name of the bone</param>
            /// <param name="parentIndex">Parent index</param>
            /// <param name="type">Bone data type</param>
            public Bone(string name, int parentIndex, DataType type)
            {
                Name = name;
                ParentIndex = parentIndex;
                Type = type;

                Translations = new SortedDictionary<int, Vector3>();
                Rotations = new SortedDictionary<int, Quaternion>();
                Scales = new SortedDictionary<int, Vector3>();
            }

            /// <summary>
            /// Gets the name of the bone as a string representation of it
            /// </summary>
            /// <returns>Name</returns>
            public override string ToString() => Name;
        }

        /// <summary>
        /// A class to hold an Animation Note
        /// </summary>
        public class Note
        {
            /// <summary>
            /// Gets or Sets the note name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or Sets the frames the note occurs on
            /// </summary>
            public List<int> Frames { get; set; }

            /// <summary>
            /// Creates a new animation notetrack
            /// </summary>
            /// <param name="name">note name</param>
            public Note(string name)
            {
                Name = name;
                Frames = new List<int>();
            }
        }

        /// <summary>
        /// Gets or Sets the animation bones
        /// </summary>
        public Dictionary<string, Bone> Bones { get; set; }

        /// <summary>
        /// Gets or Sets the animation notes
        /// </summary>
        public Dictionary<string, Note> Notes { get; set; }

        /// <summary>
        /// Gets or Sets the data type for this animation
        /// </summary>
        public DataType Type { get; set; }

        /// <summary>
        /// Gets or Sets the framerate
        /// </summary>
        public float Framerate { get; set; }

        /// <summary>
        /// Gets whether the animation contains translation keys in any of the bones
        /// </summary>
        public bool ContainsTranslationKeys
        {
            get
            {
                foreach (var bone in Bones)
                    if (bone.Value.Translations.Count > 0)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Gets whether the animation contains rotation keys in any of the bones
        /// </summary>
        public bool ContainsRotationKeys
        {
            get
            {
                foreach (var bone in Bones)
                    if (bone.Value.Rotations.Count > 0)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Gets whether the animation contains scale keys in any of the bones
        /// </summary>
        public bool ContainsScaleKeys
        {
            get
            {
                foreach (var bone in Bones)
                    if (bone.Value.Scales.Count > 0)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Gets the number of frames (highest frame across bones/notes)
        /// </summary>
        public int FrameCount
        {
            get
            {
                int result = 0;

                foreach (var bone in Bones)
                {
                    if (bone.Value.Translations.Count > 0)
                    {
                        var lastFrame = bone.Value.Translations.LastOrDefault().Key;

                        if (lastFrame > result)
                            result = lastFrame;
                    }

                    if (bone.Value.Rotations.Count > 0)
                    {
                        var lastFrame = bone.Value.Rotations.LastOrDefault().Key;

                        if (lastFrame > result)
                            result = lastFrame;
                    }

                    if (bone.Value.Scales.Count > 0)
                    {
                        var lastFrame = bone.Value.Scales.LastOrDefault().Key;

                        if (lastFrame > result)
                            result = lastFrame;
                    }
                }

                foreach (var note in Notes)
                    foreach (var frame in note.Value.Frames)
                        if (frame > result)
                            result = frame;

                return result + 1;
            }
        }

        /// <summary>
        /// Gets whether the animation contains notes
        /// </summary>
        public bool ContainsNotes
        {
            get => Notes.Count > 0;
        }

        /// <summary>
        /// Creates a new animation
        /// </summary>
        public AnimationTemp()
        {
            Type = DataType.Absolute;
            Framerate = 30.0f;
            Bones = new Dictionary<string, Bone>();
            Notes = new Dictionary<string, Note>();
        }

        /// <summary>
        /// Creates a new animation
        /// </summary>
        /// <param name="type">Animation data type</param>
        public AnimationTemp(DataType type)
        {
            Type = type;
            Framerate = 30.0f;
            Bones = new Dictionary<string, Bone>();
            Notes = new Dictionary<string, Note>();
        }

        /// <summary>
        /// Clears all loaded data
        /// </summary>
        public void Clear()
        {
            Type = DataType.Absolute;
            Framerate = 30.0f;
            Bones.Clear();
            Notes.Clear();
        }

        /// <summary>
        /// Adds a translation frame for the given bone at the given frame
        /// </summary>
        /// <param name="bone">Bone to add the keyframe too, if the bone doesn't exist, it will be created and added to the animation</param>
        /// <param name="frame">Frame to set the data on</param>
        /// <param name="data">Translation data to add at the frame</param>
        public void SetDataType(string bone, int frame, DataType data)
        {
            if (Bones.TryGetValue(bone, out var val))
            {
                val.Type = data;
            }
            else
            {
                var nBone = new Bone(bone, -1, data);
                Bones[bone] = nBone;
            }
        }

        /// <summary>
        /// Adds a translation frame for the given bone at the given frame
        /// </summary>
        /// <param name="bone">Bone to add the keyframe too, if the bone doesn't exist, it will be created and added to the animation</param>
        /// <param name="frame">Frame to set the data on</param>
        /// <param name="data">Translation data to add at the frame</param>
        public void AddTranslation(string bone, int frame, Vector3 data)
        {
            if (Bones.TryGetValue(bone, out var val))
            {
                val.Translations[frame] = data * 2.54f;
            }
            else
            {
                var nBone = new Bone(bone, -1, Type);
                nBone.Translations[frame] = data * 2.54f;
                Bones[bone] = nBone;
            }
        }

        /// <summary>
        /// Adds a translation frame for the given bone at the given frame
        /// </summary>
        /// <param name="bone">Bone to add the keyframe too, if the bone doesn't exist, it will be created and added to the animation</param>
        /// <param name="frame">Frame to set the data on</param>
        /// <param name="data">Translation data to add at the frame</param>
        public void AddScale(string bone, int frame, Vector3 data)
        {
            if (Bones.TryGetValue(bone, out var val))
            {
                val.Scales[frame] = data;
            }
            else
            {
                var nBone = new Bone(bone, -1, Type);
                nBone.Scales[frame] = data;
                Bones[bone] = nBone;
            }
        }

        /// <summary>
        /// Adds a rotation frame for the given bone at the given frame
        /// </summary>
        /// <param name="bone">Bone to add the keyframe too, if the bone doesn't exist, it will be created and added to the animation</param>
        /// <param name="frame">Frame to set the data on</param>
        /// <param name="data">Rotation data to add at the frame</param>
        public void AddRotation(string bone, int frame, Quaternion data)
        {
            if (Bones.TryGetValue(bone, out var val))
            {
                val.Rotations[frame] = data;
            }
            else
            {
                var nBone = new Bone(bone, -1, Type);
                nBone.Rotations[frame] = data;
                Bones[bone] = nBone;
            }
        }

        /// <summary>
        /// Scales the animation by the given value
        /// </summary>
        /// <param name="value">Value to scale the animation by</param>
        public void Scale(float value)
        {
            if (value == 1.0f)
                return;

            foreach (var bone in Bones)
            {
                var translationKeys = bone.Value.Translations.Keys.ToArray();

                foreach (var key in translationKeys)
                {
                    bone.Value.Translations[key] *= value;
                }
            }
        }
    }
}
