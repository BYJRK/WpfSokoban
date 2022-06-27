using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using WpfSokoban.Models;
using System;

namespace WpfSokoban.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public Level Level { get; }

        public int CurrentLevel { get; private set; } = 1;

        public MainWindowViewModel()
        {
            Level = new Level();
            Level.LoadLevel(GetLevel(CurrentLevel));

            var properties = typeof(Resource).GetProperties();

            KeyUpCommand = new RelayCommand<KeyEventArgs>(KeyUpHandler);
            NextLevelCommand = new RelayCommand(() =>
            {
                if (CurrentLevel >= 3)
                    return;
                Level.LoadLevel(GetLevel(CurrentLevel++));
            });
            RestartCommand = new RelayCommand(() => Level.LoadLevel(GetLevel(CurrentLevel)));
        }

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
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        private void KeyUpHandler(KeyEventArgs e)
        {
            if (Level.IsWinning)
                return;

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
            }

            // the movement is legal
            Level.StepCount++;

            Level.Hero.Move(offset);
        }

        public ICommand KeyUpCommand { get; }

        public ICommand NextLevelCommand { get; }

        public ICommand RestartCommand { get; }
    }
}
