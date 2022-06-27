using Microsoft.Toolkit.Mvvm.ComponentModel;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WpfSokoban.Models
{
    public class Level : ObservableObject
    {
        public static int GridSize = 50;

        public ObservableCollection<Block> Map { get; } = new();

        public MovableObject Hero { get; private set; }

        public ObservableCollection<MovableObject> Crates { get; } = new();

        public int Width { get; private set; }
        public int Height { get; private set; }

        [AlsoNotifyFor(nameof(IsWinning))]
        public int StepCount { get; set; } = 0;

        public void LoadLevel(string text)
        {
            Map.Clear();
            Crates.Clear();

            StepCount = 0;
            Width = Height = 0;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                for (int j = 0; j < line.Length; j++)
                {
                    var ch = line[j];

                    int x = j, y = i;

                    // Get grid size to draw the canvas
                    if (x > Width)
                        Width = x;
                    if (y > Height)
                        Height = y;

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

            Width = GridSize * (Width + 1);
            Height = GridSize * (Height + 1);

            OnPropertyChanged(nameof(IsWinning));
        }

        public bool HasWallAt(int x, int y)
        {
            foreach (var block in Map.Where(b => b.Type == BlockType.Wall))
            {
                if (block.X == x && block.Y == y)
                    return true;
            }
            return false;
        }

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

        public MovableObject HasCrateAt(int x, int y)
        {
            foreach (var crate in Crates)
            {
                if (crate.X == x && crate.Y == y)
                    return crate;
            }
            return null;
        }
    }
}
