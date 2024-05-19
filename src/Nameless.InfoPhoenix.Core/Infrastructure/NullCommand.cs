using System.Windows.Input;

namespace Nameless.InfoPhoenix.Infrastructure {
    public sealed class NullCommand : ICommand {
        #region Public Static Read-Only Properties

        public static ICommand Instance = new NullCommand();

        #endregion

        #region MyRegion

        static NullCommand() { }

        #endregion

        #region Private Constructors

        private NullCommand() { }

        #endregion

        #region ICommand Members

        public bool CanExecute(object? parameter)
            => false;

        public void Execute(object? parameter) { }

        public event EventHandler? CanExecuteChanged {
            add { }
            remove { }
        }

        #endregion
    }
}
