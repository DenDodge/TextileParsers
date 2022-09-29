namespace NTextileCore.Blocks
{
    public class NoTextileBlockModifier : BlockModifier
    {
        public override string ModifyLine(string line)
        {
            line = NoTextileEncoder.EncodeNoTextileZones(line, @"(?<=^|\s)<notextile>", @"</notextile>(?=(\s|$)?)");
            line = NoTextileEncoder.EncodeNoTextileZones(line, @"==", @"==");
            return line;
        }

        public override string Conclude(string line)
        {
            // Recode "x"... we can safely replace all occurences because there's no reason
            // we should leave it encoded.
            line = line.Replace("&#120;", "x");
            // Same with parenthesis
            line = line.Replace("&#40;", "(");
            line = line.Replace("&#41;", ")");
            // And same with period.
            line = line.Replace("&#46;", ".");
            return line;
        }
    }
}