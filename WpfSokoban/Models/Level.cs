using Microsoft.Toolkit.Mvvm.ComponentModel;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace WpfSokoban.Models
{
    public class Level : ObservableObject
    {
        /// <summary>
        /// The grid size inside the level used to draw the controls
        /// </summary>
        public static int GridSize = 50;

        /// <summary>
        /// The map of the level (including walls, spaces and goals)
        /// </summary>
        public ObservableCollection<Block> Map { get; } = new();

        /// <summary>
        /// The player movable object
        /// </summary>
        public MovableObject Hero { get; private set; }

        /// <summary>
        /// All crates on the map
        /// </summary>
        public ObservableCollection<MovableObject> Crates { get; } = new();

        /// <summary>
        /// Record the player actions so as to achieve undo function
        /// </summary>
        public Stack<(MovableObject obj, (int, int) offset)> History { get; private set; } = new();

        public int Width { get; private set; }
        public int Height { get; private set; }

        [AlsoNotifyFor(nameof(IsWinning), nameof(History))]
        public int StepCount { get; set; } = 0;

        public void LoadLevel(string text)
        {
            Reset();

            StepCount = 0;

            (Width, Height) = ParseLevelString(text);

            Width = GridSize * (Width + 1);
            Height = GridSize * (Height + 1);

            OnPropertyChanged(nameof(IsWinning));
        }

        private (int width, int height) ParseLevelString(string text)
        {
            int width = 0, height = 0;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                for (int j = 0; j < line.Length; j++)
                {
                    var ch = line[j];

                    int x = j, y = i;

                    // Get grid size to draw the canvas
                    if (x > width)
                        width = x;
                    if (y > height)
                        height = y;

                    /**
                     * #: Wall
                     * _: Space
                     * *: Star
                     * @: Hero
                     * $: Crate
                     */

                    if (ch == '#')
                        Map.Add(new Block(BlockType.Wall, x, y));
                    else if (ch == '*')
                        Map.Add(new Block(BlockType.Star, x, y));
                    else
                    {
                        Map.Add(new Block(BlockType.Space, x, y));
                        if (ch == '@')
                            Hero = new MovableObject(MovableObjectType.Hero, x, y);
                        else if (ch == '$')
                            Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                    }
                }
            }

            return (width, height);
        }

        private void Reset()
        {
            Map.Clear();
            Crates.Clear();
            History.Clear();
        }

        /// <summary>
        /// Check if there is a wall at a specific position
        /// </summary>
        public bool HasWallAt(int x, int y)
        {
            foreach (var block in Map.Where(b => b.Type == BlockType.Wall))
            {
                if (block.X == x && block.Y == y)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if all crates are on the goal block
        /// </summary>
        public bool IsWinning
        {
            get
            {
                foreach (var crate in Crates)
                {
                    if (!crate.IsOnStar)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Check if there is a crate at a specific position
        /// </summary>
        public MovableObject HasCrateAt(int x, int y)
        {
            foreach (var crate in Crates)
            {
                if (crate.X == x && crate.Y == y)
                    return crate;
            }
            return null;
        }

        /// <summary>
        /// Undo one step according to the history
        /// </summary>
        public void Undo()
        {
            Debug.Assert(History.Count > 0);

            // the last step must be hero's
            (var hero, var offset) = History.Pop();
            StepCount -= 1;
            hero.MoveBack(offset);

            // check if the previous step is a movement of a crate
            if (History.TryPeek(out var move) && move.obj.Type == MovableObjectType.Crate)
            {
                History.Pop();
                move.obj.MoveBack(move.offset);
                move.obj.CheckOnStar(this);
            }

            OnPropertyChanged(nameof(History));
        }
    }
}
