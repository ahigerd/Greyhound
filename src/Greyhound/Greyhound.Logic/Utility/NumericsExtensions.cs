using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Greyhound.Logic
{
    public static class NumericsExtensions
    {
        public static Vector3 TransformVector(this Quaternion quat, Vector3 vec)
        {
            var a = new Vector3(
                quat.Y * vec.Z - quat.Z * vec.Y + vec.X * quat.W,
                quat.Z * vec.X - quat.X * vec.Z + vec.Y * quat.W,
                quat.X * vec.Y - quat.Y * vec.X + vec.Z * quat.W);
            var b = new Vector3(
                quat.Y * a.Z - quat.Z * a.Y, 
                quat.Z * a.X - quat.X * a.Z,
                quat.X * a.Y - quat.Y * a.X);
            return new Vector3(vec.X + b.X + b.X, vec.Y + b.Y + b.Y, vec.Z + b.Z + b.Z);
        }
    }
}
