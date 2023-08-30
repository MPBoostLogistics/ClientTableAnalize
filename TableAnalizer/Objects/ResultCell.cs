namespace TableAnalizer.Objects
{
    internal struct ResultCell
    {
        public string Name { get; private set; }
        public string Format { get; private set; }

        public ResultCell(string name, string format)
        {
            Name = name;
            Format = format;
        }
    }
}