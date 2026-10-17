using System;

namespace XRay.Importer.Core
{
    /// <summary>
    /// Double precision unit quaternion (x, y, z, w). Uses the standard Hamilton algebra, which is the
    /// same algebra UnityEngine.Quaternion uses, so components can be copied to Unity 1:1.
    /// </summary>
    public readonly struct XQuaternion
    {
        public static readonly XQuaternion Identity = new XQuaternion(0.0, 0.0, 0.0, 1.0);

        public XQuaternion(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public double W { get; }

        public double Norm => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

        public XQuaternion Conjugate()
        {
            return new XQuaternion(-X, -Y, -Z, W);
        }

        public XQuaternion Negated()
        {
            return new XQuaternion(-X, -Y, -Z, -W);
        }

        public XQuaternion Normalized()
        {
            double norm = Norm;
            if (norm <= 0.0)
            {
                return Identity;
            }

            return new XQuaternion(X / norm, Y / norm, Z / norm, W / norm);
        }

        public static double Dot(XQuaternion a, XQuaternion b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        }

        public static XQuaternion operator *(XQuaternion a, XQuaternion b)
        {
            double x = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
            double y = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
            double z = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;
            double w = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;
            return new XQuaternion(x, y, z, w);
        }

        public static XQuaternion AxisAngle(XVector3 axis, double radians)
        {
            XVector3 unit = axis.Normalized();
            double half = radians * 0.5;
            double s = Math.Sin(half);
            return new XQuaternion(unit.X * s, unit.Y * s, unit.Z * s, Math.Cos(half));
        }

        /// <summary>
        /// Rotation of the X-Ray bind pose: R = Ry(y) * Rx(x) * Rz(z) (column vectors, Z applied first).
        /// This is the native-space equivalent of blender-xray's Euler((-x, -z, -y), 'YXZ') after
        /// undoing its Y/Z axis swap, and coincides with UnityEngine.Quaternion.Euler(x, y, z) (in degrees).
        /// </summary>
        public static XQuaternion FromXRayBindEuler(XVector3 radians)
        {
            XQuaternion qy = AxisAngle(new XVector3(0.0, 1.0, 0.0), radians.Y);
            XQuaternion qx = AxisAngle(new XVector3(1.0, 0.0, 0.0), radians.X);
            XQuaternion qz = AxisAngle(new XVector3(0.0, 0.0, 1.0), radians.Z);
            return qy * qx * qz;
        }

        public override string ToString()
        {
            return $"({X:0.######}, {Y:0.######}, {Z:0.######}, {W:0.######})";
        }
    }
}
