using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using WpfSokoban.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WpfSokoban.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// The level model
        /// </summary>
        public Level Level { get; }

        /// <summary>
        /// Indicates the index of the current level
        /// </summary>
        public int CurrentLevel { get; private set; } = 1;

        /// <summary>
        /// A temporary flag to indicate whether there are more levels to play
        /// </summary>
        public bool HasMoreLevels => CurrentLevel < 5;

        public MainWindowViewModel()
        {
            Level = new Level();
            Level.LoadLevel(GetLevel(CurrentLevel));

            var properties = typeof(Resource).GetProperties();

            KeyUpCommand = new RelayCommand<KeyEventArgs>(KeyUpHandler);
            NextLevelCommand = new RelayCommand(() =>
            {
                try
                {
                    string res = GetLevel(CurrentLevel + 1);
                    Level.LoadLevel(res);
                    CurrentLevel += 1;
                }
                catch (Exception)
                {
                    // There is no more levels to play
                }
            });
            RestartCommand = new RelayCommand(() => Level.LoadLevel(GetLevel(CurrentLevel)));
            UndoCommand = new RelayCommand(Level.Undo);
        }

        /// <summary>
        /// Convert int to actual level resource
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        private string GetLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return Resource.Level1;
                case 2:
                    return Resource.Level2;
                case 3:
                    return Resource.Level3;
                case 4:
                    return Resource.Level4;
                case 5:
                    return Resource.Level5;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        /// <summary>
        /// Handle key press event to control the hero
        /// </summary>
        /// <param name="e"></param>
        private void KeyUpHandler(KeyEventArgs e)
        {
            if (Level.IsWinning)
            {
                if (e.Key == Key.Enter && HasMoreLevels)
                    NextLevelCommand.Execute(null);
                else
                    return;
            }

            var x = Level.Hero.X;
            var y = Level.Hero.Y;

            (int x, int y) offset = (0, 0);

            if (e.Key == Key.Up)
                offset = (0, -1);
            else if (e.Key == Key.Down)
                offset = (0, 1);
            else if (e.Key == Key.Left)
                offset = (-1, 0);
            else if (e.Key == Key.Right)
                offset = (1, 0);
            else return;

            x += offset.x;
            y += offset.y;

            // hero hits the wall
            bool canHeroMove = !Level.HasWallAt(x, y);

            if (!canHeroMove)
                return;

            // hero hits the crate
            var hitCrate = Level.HasCrateAt(x, y);

            if (hitCrate is not null)
            {
                // check if the crate can be pushed
                var cx = hitCrate.X + offset.x;
                var cy = hitCrate.Y + offset.y;

                bool canCrateMove = !Level.HasWallAt(cx, cy) && Level.HasCrateAt(cx, cy) == null;

                if (!canCrateMove)
                    return;

                // move the crate
                hitCrate.Move(offset);
                hitCrate.CheckOnStar(Level);

                Level.History.Push((hitCrate, offset));
            }

            // the movement is legal

            Level.Hero.Move(offset);
            Level.History.Push((Level.Hero, offset));

            Level.StepCount++;
        }

        /// <summary>
        /// Handle the key press event of the window
        /// </summary>
        public ICommand KeyUpCommand { get; }

        /// <summary>
        /// Load the next level
        /// </summary>
        public ICommand NextLevelCommand { get; }

        /// <summary>
        /// Restart the current level
        /// </summary>
        public ICommand RestartCommand { get; }

        /// <summary>
        /// Undo one step
        /// </summary>
        public ICommand UndoCommand { get; }
    }
}
