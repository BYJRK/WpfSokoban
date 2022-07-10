namespace WpfSokoban.Models
{
    /// <summary>
    /// A block inside the map
    /// </summary>
    public class Block
    {
        public Block(BlockType type, int x, int y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public BlockType Type { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int ActualX => X * Level.GridSize;
        public int ActualY => Y * Level.GridSize;
    }
}
