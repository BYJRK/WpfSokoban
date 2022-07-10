using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using WpfSokoban.Messages;

namespace WpfSokoban.Models
{
    public partial class Level : ObservableObject
    {
        /// <summary>
        /// The grid size of the canvas to draw the controls
        /// </summary>
        public static int GridSize = 50;

        public static int LevelCount = 5;

        /// <summary>
        /// Indicates the index of the current level
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMoreLevels))]
        private int currentLevel = 1;

        /// <summary>
        /// A temporary flag to indicate whether there are more levels to play
        /// </summary>
        public bool HasMoreLevels => CurrentLevel < LevelCount;

        /// <summary>
        /// The map of the current level (including walls, spaces and goals)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Block> map = new();

        /// <summary>
        /// The player controllable object
        /// </summary>
        [ObservableProperty]
        private MovableObject hero;

        /// <summary>
        /// All crates in the current level
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<MovableObject> crates = new();

        /// <summary>
        /// A stack to record the player actions so as to undo actions later
        /// </summary>
        [ObservableProperty]
        private Stack<(MovableObject obj, (int, int) offset)> history = new();

        [ObservableProperty]
        private int width;

        [ObservableProperty]
        private int height;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWinning), nameof(History))]
        private int stepCount = 0;

        partial void OnStepCountChanged(int value)
        {
            SendNotifyUndoAvailabilityMessage();
        }

        public void LoadLevel(string text)
        {
            Init();

            StepCount = 0;
            (Width, Height) = ParseLevelString(text);

            Width = GridSize * (Width + 1);
            Height = GridSize * (Height + 1);

            OnPropertyChanged(nameof(IsWinning));

            SendNotifyUndoAvailabilityMessage();
        }

        public void LoadLevel(int? level = null)
        {
            if (level == null)
                level = CurrentLevel;
            LoadLevel(GetLevel(level.Value));
        }

        /// <summary>
        /// Try to load the next level (if there is one)
        /// </summary>
        /// <returns></returns>
        public bool TryLoadNextLevel()
        {
            try
            {
                var str = GetLevel(CurrentLevel + 1);
                LoadLevel(str);
                CurrentLevel++;
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        private void Init()
        {
            Map.Clear();
            Crates.Clear();
            History.Clear();
        }

        private void SendNotifyUndoAvailabilityMessage()
        {
            WeakReferenceMessenger.Default.Send(new NotifyUndoAvailabilityMessage(null));
        }

        /// <summary>
        /// Convert int to actual level resource
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public string GetLevel(int level)
        {
            if (level >= 1 && level <= LevelCount)
            {
                var prop = typeof(Resource).GetProperty($"Level{level}", BindingFlags.Static | BindingFlags.NonPublic);
                return prop.GetValue(null) as string;
            }
            throw new IndexOutOfRangeException();
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


        /// <summary>
        /// Undo one step according to the history
        /// </summary>
        public void Undo()
        {
            Debug.Assert(History.Count > 0);

            // the last step must be hero's
            (var hero, var offset) = History.Pop();
            StepCount -= 1;
            hero.Reverse(offset);

            // check if the previous step is a movement of a crate
            if (History.TryPeek(out var move) && move.obj.Type == MovableObjectType.Crate)
            {
                History.Pop();
                move.obj.Reverse(move.offset);
                move.obj.CheckOnStar(this);
            }

            OnPropertyChanged(nameof(History));
        }
    }
}
