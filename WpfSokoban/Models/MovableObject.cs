using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;

namespace WpfSokoban.Models
{
    /// <summary>
    /// A movable object which can be the hero or a crate
    /// </summary>
    public partial class MovableObject : ObservableObject
    {
        public MovableObject(MovableObjectType type, int x, int y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        [ObservableProperty]
        private MovableObjectType type;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualX))]
        private int x;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualY))]
        public int y;

        public int ActualX => X * Level.GridSize;

        public int ActualY => Y * Level.GridSize;

        [ObservableProperty]
        private bool isOnStar = false;

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
            // No need for hero to check if it's on the star
            if (Type == MovableObjectType.Hero)
                return;

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
