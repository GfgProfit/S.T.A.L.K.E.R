using System;

namespace XRay.Importer.Core.IO
{
    /// <summary>
    /// Raised when an X-Ray binary file is malformed or uses an unsupported variant.
    /// The message is intended to be shown to the user as-is.
    /// </summary>
    public sealed class XRayFormatException : Exception
    {
        public XRayFormatException(string message) : base(message)
        {
        }

        public XRayFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
