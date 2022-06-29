using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using System.Windows.Input;
using WpfSokoban.Messages;
using WpfSokoban.Models;

namespace WpfSokoban.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// The level model
        /// </summary>
        public Level Level { get; }


        public MainWindowViewModel()
        {
            Level = new Level();
            Level.LoadLevel();

            var properties = typeof(Resource).GetProperties();

            KeyUpCommand = new RelayCommand<KeyEventArgs>(KeyUpHandler);
            NextLevelCommand = new RelayCommand(() =>
            {
                Level.TryLoadNextLevel();
                NextLevelCommand.NotifyCanExecuteChanged();

            }, () => Level.HasMoreLevels);
            RestartCommand = new RelayCommand(() => Level.RestartLevel());
            UndoCommand = new RelayCommand(() =>
            {
                Level.Undo();
                UndoCommand.NotifyCanExecuteChanged();
            }, () => Level.History.Count > 0);

            WeakReferenceMessenger.Default.Register<NotifyUndoAvailabilityMessage>(this, (r, m) =>
            {
                UndoCommand.NotifyCanExecuteChanged();
            });
        }

        /// <summary>
        /// Handle key press event to control the hero
        /// </summary>
        /// <param name="e"></param>
        private void KeyUpHandler(KeyEventArgs e)
        {
            if (Level.IsWinning)
            {
                if (e.Key == Key.Enter && Level.HasMoreLevels)
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
        public RelayCommand<KeyEventArgs> KeyUpCommand { get; }

        /// <summary>
        /// Load the next level
        /// </summary>
        public RelayCommand NextLevelCommand { get; }

        /// <summary>
        /// Restart the current level
        /// </summary>
        public RelayCommand RestartCommand { get; }

        /// <summary>
        /// Undo one step
        /// </summary>
        public RelayCommand UndoCommand { get; }
    }
}
