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

        /// <summary>
        /// The type of this movable object (hero or crate)
        /// </summary>
        [ObservableProperty]
        private MovableObjectType type;

        /// <summary>
        /// The x coordinate of this object
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualX))]
        private int x;

        /// <summary>
        /// The y coordinate of this object
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualY))]
        private int y;

        /// <summary>
        /// The actual Canvas.Left to draw the object on the canvas
        /// </summary>
        public int ActualX => X * Level.GridSize;

        /// <summary>
        /// The actual Canvas.Top to draw the object on the canvas
        /// </summary>
        public int ActualY => Y * Level.GridSize;

        /// <summary>
        /// Whether this crate is on the goal
        /// </summary>
        [ObservableProperty]
        private bool isOnGoal = false;

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

        /// <summary>
        /// Check if this crate is on goal 
        /// </summary>
        /// <param name="level"></param>
        public void CheckOnGoal(Level level)
        {
            // No need for hero to check if it's on the star
            if (Type == MovableObjectType.Hero)
                return;

            if (level.Map.Where(b => b.Type == BlockType.Goal).Any(block => block.X == X && block.Y == Y))
            {
                IsOnGoal = true;
                return;
            }
            IsOnGoal = false;
        }
    }
}
