using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to indicate that an additional mapping method should be generated with 
    /// a mapping in the reverse direction.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ReverseMapAttribute : Attribute
    {
    }
}