using Microsoft.Toolkit.Mvvm.ComponentModel;
using PropertyChanged;
using System.Linq;

namespace WpfSokoban.Models
{
    /// <summary>
    /// A movable object which can be the hero or a crate
    /// </summary>
    public class MovableObject : ObservableObject, IPosition
    {
        public MovableObject(MovableObjectType type, int x, int y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public MovableObjectType Type { get; }
        [AlsoNotifyFor(nameof(ActualX))]
        public int X { get; set; }
        [AlsoNotifyFor(nameof(ActualY))]
        public int Y { get; set; }
        public int ActualX => X * Level.GridSize;
        public int ActualY => Y * Level.GridSize;
        public bool IsOnStar { get; private set; } = false;

        private void Move(int x, int y)
        {
            X += x;
            Y += y;
        }
        /// <summary>
        /// Move the hero or the crate
        /// </summary>
        public void Move((int x, int y) offset)
        {
            Move(offset.x, offset.y);
        }
        /// <summary>
        /// Reverse the movement to achieve undo function
        /// </summary>
        public void Reverse((int x, int y) offset)
        {
            Move(-offset.x, -offset.y);
        }

        public void CheckOnStar(Level level)
        {
            foreach (var block in level.Map.Where(b => b.Type == BlockType.Star))
            {
                if (block.X == X && block.Y == Y)
                {
                    IsOnStar = true;
                    return;
                }
            }
            IsOnStar = false;
        }
    }
}
