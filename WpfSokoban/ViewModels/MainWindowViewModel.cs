using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using WpfSokoban.Messages;
using WpfSokoban.Models;

namespace WpfSokoban.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// The level model
        /// </summary>
        [ObservableProperty]
        private Level level = new();

        public MainWindowViewModel()
        {
            Level.LoadLevel();

            var properties = typeof(Resource).GetProperties();

            WeakReferenceMessenger.Default.Register<NotifyUndoAvailabilityMessage>(this, (r, m) =>
            {
                UndoCommand.NotifyCanExecuteChanged();
            });
        }

        /// <summary>
        /// Handle the key press event of the window
        /// </summary>
        [RelayCommand]
        private void WindowKeyUp(KeyEventArgs e)
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
        /// Load the next level
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanNextLevelExecute))]
        private void NextLevel()
        {
            Level.TryLoadNextLevel();
            NextLevelCommand.NotifyCanExecuteChanged();
        }

        private bool CanNextLevelExecute() => Level.HasMoreLevels;

        /// <summary>
        /// Restart the current level
        /// </summary>
        [RelayCommand]
        private void Restart() => Level.RestartLevel();

        /// <summary>
        /// Undo one step
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndoExecute))]
        private void Undo()
        {
            Level.Undo();
            UndoCommand.NotifyCanExecuteChanged();
        }

        private bool CanUndoExecute() => Level.History.Count > 0;
    }
}
