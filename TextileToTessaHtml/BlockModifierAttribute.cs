using System;

namespace TextileToTessaHtml
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BlockModifierAttribute : Attribute
    {
        public BlockModifierAttribute()
        {
        }
    }
}