using PhilLibX;
using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Greyhound.Logic
{
    public static class XAnimFileHelper
    {
        static void LoadRotationsDivide(BinaryReader reader, AnimationTemp.Bone bone, bool flipQuat, bool simpleQuat, ushort frameCount)
        {
            var rotationCount = reader.ReadUInt16();

            // No rotations, no point in continuing
            if (rotationCount == 0)
                return;

            ushort[] frames = null;

            // If we only have 1 rotation, or it's the same as frame count, build a range
            if (rotationCount == 1 || rotationCount == frameCount)
                frames = Enumerable.Range(0, rotationCount).Select(x => (ushort)x).ToArray();
            else if (frameCount > 0xFF)
                frames = reader.ReadArray<ushort>(rotationCount);
            else
                frames = reader.ReadArray<byte>(rotationCount).Select(x => (ushort)x).ToArray();

            for (ushort i = 0; i < rotationCount; i++)
            {
                if (simpleQuat)
                {
                    var x = 0.0f;
                    var y = 0.0f;
                    var z = reader.ReadInt16() / 32767.0f;
                    var w = 1 - x * x - y * y - z * z;

                    if (w <= 0)
                        w = 0;
                    else
                        w = (float)Math.Sqrt(w);

                    bone.Rotations.Add(frames[i], new Quaternion(
                        x,
                        y,
                        z,
                        w));
                }
                else
                {
                    var x = reader.ReadInt16() / 32767.0f;
                    var y = reader.ReadInt16() / 32767.0f;
                    var z = reader.ReadInt16() / 32767.0f;
                    var w = 1 - x * x - y * y - z * z;

                    if (w <= 0)
                        w = 0;
                    else
                        w = (float)Math.Sqrt(w);

                    bone.Rotations.Add(frames[i], new Quaternion(
                        x,
                        y,
                        z,
                        w));
                }
            }
        }

        static void LoadTranslationsUncompressed(BinaryReader reader, AnimationTemp.Bone bone, ushort frameCount)
        {
            var translationCount = reader.ReadUInt16();

            // No translation, no point in continuing
            if (translationCount == 0)
                return;

            // One translation, so we read direct from the file
            if (translationCount == 1)
            {
                bone.Translations.Add(0, reader.ReadStruct<Vector3>());
                return;
            }

            ushort[] frames = null;

            // If it's the same as frame count, build a range
            if (translationCount == frameCount)
                frames = Enumerable.Range(0, translationCount).Select(x => (ushort)x).ToArray();
            else if (frameCount > 0xFF)
                frames = reader.ReadArray<ushort>(translationCount);
            else
                frames = reader.ReadArray<byte>(translationCount).Select(x => (ushort)x).ToArray();

            for (ushort i = 0; i < translationCount; i++)
            {
                bone.Translations.Add(frames[i], reader.ReadStruct<Vector3>());
            }
        }

        public static void ConvertV14(BinaryReader reader, AnimAsset anim)
        {
            var frameCount = reader.ReadUInt16();
            var boneCount  = reader.ReadUInt16();
            var flags      = reader.ReadByte();
            var frameRate  = reader.ReadUInt16();

            // TODO: Set animation type based off flags
            anim.Anim = new AnimationTemp(AnimationTemp.DataType.Relative);

            // Delta
            if ((flags & 0x2) != 0)
            {
                anim.Anim.Bones["tag_origin"] = new AnimationTemp.Bone("tag_origin");
                LoadRotationsDivide(reader, anim.Anim.Bones["tag_origin"], false, true, frameCount);
                LoadTranslationsUncompressed(reader, anim.Anim.Bones["tag_origin"], frameCount);
            }
            // Looping
            if ((flags & 0x1) != 0)
            {
                frameCount += 1;
            }

            var boneFlagsSize = ((boneCount - 1) >> 3) + 1;

            var flipFlags   = reader.ReadBytes(boneFlagsSize);
            var simpleFlags = reader.ReadBytes(boneFlagsSize);

            var boneNames = new string[boneCount];


            for(int i = 0; i < boneCount; i++)
            {
                var name = reader.ReadNullTerminatedString().Replace(' ', '_');

                anim.Anim.Bones[name] = new AnimationTemp.Bone(name);
                boneNames[i] = name;
            }

            for(int i = 0; i < boneCount; i++)
            {
                bool flipQuat   = ((1 << (i & 7)) & flipFlags[i >> 3]) != 0;
                bool simpleQuat = ((1 << (i & 7)) & simpleFlags[i >> 3]) != 0;

                LoadRotationsDivide(reader, anim.Anim.Bones[boneNames[i]], flipQuat, simpleQuat, frameCount);
                LoadTranslationsUncompressed(reader, anim.Anim.Bones[boneNames[i]], frameCount);
            }
        }

        public static void Convert(byte[] buffer, AnimAsset anim)
        {
            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                var version = reader.ReadUInt16();

                switch (version)
                {
                    case 0xE:
                        ConvertV14(reader, anim);
                        return;
                }
            }
        }
    }
}
