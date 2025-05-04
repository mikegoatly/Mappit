using System;

namespace Mappit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MappitAttribute : Attribute
    {
    }
}
