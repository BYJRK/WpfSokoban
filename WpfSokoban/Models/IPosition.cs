namespace WpfSokoban.Models
{
    public interface IPosition
    {
        /// <summary>
        /// the x coordinate in the grid
        /// </summary>
        int X { get; set; }
        /// <summary>
        /// the y coordinate in the grid
        /// </summary>
        int Y { get; set; }
        /// <summary>
        /// the actual horizontal position on the canvas
        /// </summary>
        int ActualX { get; }
        /// <summary>
        /// the actual vertical position on the canvas
        /// </summary>
        int ActualY { get; }
    }
}
