using System;

namespace XRay.Importer.Core
{
    /// <summary>
    /// Double precision affine 4x4 matrix, column-vector convention (p' = M * p), element [row, column].
    /// Same convention as UnityEngine.Matrix4x4 and Blender mathutils.Matrix.
    /// </summary>
    public readonly struct XMatrix4
    {
        private readonly double[] _m;

        private XMatrix4(double[] m)
        {
            _m = m;
        }

        public static XMatrix4 Identity => FromRows(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public double this[int row, int column] => _m[row * 4 + column];

        public static XMatrix4 FromRows(double m00, double m01, double m02, double m03, double m10, double m11, double m12, double m13, double m20, double m21, double m22, double m23, double m30, double m31, double m32, double m33)
        {
            return new XMatrix4(new[] { m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33 });
        }

        public static XMatrix4 FromArray(double[] rowMajor)
        {
            if (rowMajor == null || rowMajor.Length != 16)
            {
                throw new ArgumentException("Expected 16 row-major elements.", nameof(rowMajor));
            }

            return new XMatrix4((double[])rowMajor.Clone());
        }

        public static XMatrix4 Translation(XVector3 t)
        {
            return FromRows(1, 0, 0, t.X, 0, 1, 0, t.Y, 0, 0, 1, t.Z, 0, 0, 0, 1);
        }

        public static XMatrix4 Rotation(XQuaternion q)
        {
            XQuaternion n = q.Normalized();
            double x = n.X;
            double y = n.Y;
            double z = n.Z;
            double w = n.W;
            return new XMatrix4(new[]
            {
                1 - 2 * (y * y + z * z), 2 * (x * y - w * z), 2 * (x * z + w * y), 0,
                2 * (x * y + w * z), 1 - 2 * (x * x + z * z), 2 * (y * z - w * x), 0,
                2 * (x * z - w * y), 2 * (y * z + w * x), 1 - 2 * (x * x + y * y), 0,
                0, 0, 0, 1.0
            });
        }

        public static XMatrix4 TranslationRotation(XVector3 t, XQuaternion q)
        {
            return Translation(t) * Rotation(q);
        }

        public XVector3 GetTranslation()
        {
            return new XVector3(_m[3], _m[7], _m[11]);
        }

        public XVector3 TransformPoint(XVector3 p)
        {
            double x = _m[0] * p.X + _m[1] * p.Y + _m[2] * p.Z + _m[3];
            double y = _m[4] * p.X + _m[5] * p.Y + _m[6] * p.Z + _m[7];
            double z = _m[8] * p.X + _m[9] * p.Y + _m[10] * p.Z + _m[11];
            return new XVector3(x, y, z);
        }

        public XVector3 TransformVector(XVector3 v)
        {
            double x = _m[0] * v.X + _m[1] * v.Y + _m[2] * v.Z;
            double y = _m[4] * v.X + _m[5] * v.Y + _m[6] * v.Z;
            double z = _m[8] * v.X + _m[9] * v.Y + _m[10] * v.Z;
            return new XVector3(x, y, z);
        }

        public static XMatrix4 operator *(XMatrix4 a, XMatrix4 b)
        {
            var r = new double[16];
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < 4; k++)
                    {
                        sum += a._m[row * 4 + k] * b._m[k * 4 + column];
                    }

                    r[row * 4 + column] = sum;
                }
            }

            return new XMatrix4(r);
        }

        /// <summary>Inverse of an affine matrix whose upper 3x3 is invertible (rotation and/or scale).</summary>
        public XMatrix4 InverseAffine()
        {
            double a = _m[0], b = _m[1], c = _m[2];
            double d = _m[4], e = _m[5], f = _m[6];
            double g = _m[8], h = _m[9], i = _m[10];
            double det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
            if (Math.Abs(det) < 1e-300)
            {
                throw new InvalidOperationException("Matrix is singular.");
            }

            double inv = 1.0 / det;
            double r00 = (e * i - f * h) * inv;
            double r01 = (c * h - b * i) * inv;
            double r02 = (b * f - c * e) * inv;
            double r10 = (f * g - d * i) * inv;
            double r11 = (a * i - c * g) * inv;
            double r12 = (c * d - a * f) * inv;
            double r20 = (d * h - e * g) * inv;
            double r21 = (b * g - a * h) * inv;
            double r22 = (a * e - b * d) * inv;
            double tx = _m[3], ty = _m[7], tz = _m[11];
            return new XMatrix4(new[]
            {
                r00, r01, r02, -(r00 * tx + r01 * ty + r02 * tz),
                r10, r11, r12, -(r10 * tx + r11 * ty + r12 * tz),
                r20, r21, r22, -(r20 * tx + r21 * ty + r22 * tz),
                0, 0, 0, 1.0
            });
        }

        /// <summary>Extracts the rotation of an orthonormal 3x3 block (Shepperd's method).</summary>
        public XQuaternion GetRotation()
        {
            double m00 = _m[0], m01 = _m[1], m02 = _m[2];
            double m10 = _m[4], m11 = _m[5], m12 = _m[6];
            double m20 = _m[8], m21 = _m[9], m22 = _m[10];
            double trace = m00 + m11 + m22;
            double x, y, z, w;
            if (trace > 0.0)
            {
                double s = Math.Sqrt(trace + 1.0) * 2.0;
                w = 0.25 * s;
                x = (m21 - m12) / s;
                y = (m02 - m20) / s;
                z = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                double s = Math.Sqrt(1.0 + m00 - m11 - m22) * 2.0;
                w = (m21 - m12) / s;
                x = 0.25 * s;
                y = (m01 + m10) / s;
                z = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                double s = Math.Sqrt(1.0 + m11 - m00 - m22) * 2.0;
                w = (m02 - m20) / s;
                x = (m01 + m10) / s;
                y = 0.25 * s;
                z = (m12 + m21) / s;
            }
            else
            {
                double s = Math.Sqrt(1.0 + m22 - m00 - m11) * 2.0;
                w = (m10 - m01) / s;
                x = (m02 + m20) / s;
                y = (m12 + m21) / s;
                z = 0.25 * s;
            }

            return new XQuaternion(x, y, z, w).Normalized();
        }

        public static double MaxAbsDifference(XMatrix4 a, XMatrix4 b)
        {
            double max = 0.0;
            for (int i = 0; i < 16; i++)
            {
                max = Math.Max(max, Math.Abs(a._m[i] - b._m[i]));
            }

            return max;
        }

        public override string ToString()
        {
            return $"[{_m[0]:0.#####} {_m[1]:0.#####} {_m[2]:0.#####} {_m[3]:0.#####}; {_m[4]:0.#####} {_m[5]:0.#####} {_m[6]:0.#####} {_m[7]:0.#####}; {_m[8]:0.#####} {_m[9]:0.#####} {_m[10]:0.#####} {_m[11]:0.#####}]";
        }
    }
}
