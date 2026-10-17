using System;

namespace XRay.Importer.Core
{
    /// <summary>Double precision 3D vector used by the format layer and the reference math.</summary>
    public readonly struct XVector3
    {
        public static readonly XVector3 Zero = new XVector3(0.0, 0.0, 0.0);

        public XVector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public XVector3 Normalized()
        {
            double length = Length;
            if (length <= 0.0)
            {
                return Zero;
            }

            return new XVector3(X / length, Y / length, Z / length);
        }

        public static double Dot(XVector3 a, XVector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static XVector3 Cross(XVector3 a, XVector3 b)
        {
            return new XVector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        public static XVector3 Min(XVector3 a, XVector3 b)
        {
            return new XVector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        public static XVector3 Max(XVector3 a, XVector3 b)
        {
            return new XVector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }

        public static XVector3 operator +(XVector3 a, XVector3 b)
        {
            return new XVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static XVector3 operator -(XVector3 a, XVector3 b)
        {
            return new XVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static XVector3 operator *(XVector3 a, double s)
        {
            return new XVector3(a.X * s, a.Y * s, a.Z * s);
        }

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
                        return Z;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public override string ToString()
        {
            return $"({X:0.######}, {Y:0.######}, {Z:0.######})";
        }
    }
}
