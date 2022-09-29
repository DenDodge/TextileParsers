namespace NTextileCore
{
    public abstract class BlockModifier
    {
        public bool IsEnabled { get; set; } = true;
        public GenericFormatter Formatter { get; set; }
        protected bool UseRestrictedMode => Formatter?.UseRestrictedMode ?? false;

        public abstract string ModifyLine(string line);

        public virtual string Conclude(string line)
        {
            return line;
        }
    }
}