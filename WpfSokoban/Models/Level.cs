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

        public static int LevelCount;

        #region Observable Properties

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
        private ObservableCollection<MovableObject> crates = new();

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

        #endregion

        public Level()
        {
            GetLevelCount();
        }

        partial void OnStepCountChanged(int value)
        {
            SendNotifyUndoAvailabilityMessage();
        }

        public void LoadLevel(string text)
        {
            Init();
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

        /// <summary>
        /// Initialize objects on the level and reset <see cref="StepCount"/> to 0
        /// </summary>
        private void Init()
        {
            Map.Clear();
            Crates.Clear();
            History.Clear();

            StepCount = 0;
        }

        /// <summary>
        /// Get level count by simply look through the resource
        /// </summary>
        private void GetLevelCount()
        {
            int level = 1;

            while (true)
            {
                var prop = typeof(Resource).GetProperty($"Level{level}", BindingFlags.Static | BindingFlags.NonPublic);
                if (prop != null)
                    level++;
                else
                    break;
            }

            LevelCount = level - 1;
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
            if (level < 1 || level > LevelCount) throw new IndexOutOfRangeException();

            var prop = typeof(Resource).GetProperty($"Level{level}", BindingFlags.Static | BindingFlags.NonPublic);
            return prop!.GetValue(null) as string;
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
                     * @: Hero
                     * _: Space
                     * .: Goal
                     * $: Crate
                     * *: Box on Goal
                     * +: Hero on Goal
                     */

                    switch (ch)
                    {
                        case '#':
                            Map.Add(new Block(BlockType.Wall, x, y));
                            break;
                        case '.':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            break;
                        case '@':
                            Map.Add(new Block(BlockType.Space, x, y));
                            Hero = new MovableObject(MovableObjectType.Hero, x, y);
                            break;
                        case '$':
                            Map.Add(new Block(BlockType.Space, x, y));
                            Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                            break;
                        case '*':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            Crates.Add(new MovableObject(MovableObjectType.Crate, x, y));
                            break;
                        case '+':
                            Map.Add(new Block(BlockType.Goal, x, y));
                            Hero = new MovableObject(MovableObjectType.Hero, x, y);
                            break;
                    }
                }
            }

            return (width, height);
        }

        public bool HasWallAt(int x, int y)
        {
            return Map.Where(b => b.Type == BlockType.Wall).Any(block => block.X == x && block.Y == y);
        }

        public bool IsWinning
        {
            get
            {
                return Crates.All(crate => crate.IsOnGoal);
            }
        }

        public MovableObject HasCrateAt(int x, int y)
        {
            return Crates.FirstOrDefault(crate => crate.X == x && crate.Y == y);
        }


        /// <summary>
        /// Undo one step according to the history
        /// </summary>
        public void Undo()
        {
            Debug.Assert(History.Count > 0);

            // the last step must be hero's
            var (hero, offset) = History.Pop();
            StepCount -= 1;
            hero.Reverse(offset);

            // check if the previous step is a movement of a crate
            // if it is, also revert the crate's last movement
            if (History.TryPeek(out var move) && move.obj.Type == MovableObjectType.Crate)
            {
                History.Pop();
                move.obj.Reverse(move.offset);
                move.obj.CheckOnGoal(this);
            }

            OnPropertyChanged(nameof(History));
        }
    }
}
