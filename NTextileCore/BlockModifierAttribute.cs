using System;

namespace NTextileCore
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BlockModifierAttribute : Attribute
    {
        public BlockModifierAttribute()
        {
        }
    }
}