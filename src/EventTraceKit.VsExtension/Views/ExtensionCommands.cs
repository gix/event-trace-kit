namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Windows.Input;

    public static class ExtensionCommands
    {
        private static readonly RoutedUICommand[] Commands = new RoutedUICommand[(int)CommandId.Count];

        public static RoutedUICommand Toggle => EnsureCommand(CommandId.Toggle);
        public static RoutedUICommand Rename => EnsureCommand(CommandId.Rename);

        private static RoutedUICommand EnsureCommand(CommandId idCommand)
        {
            if (idCommand < CommandId.Toggle || idCommand >= CommandId.Count)
                return null;

            var index = (int)idCommand;
            lock (Commands.SyncRoot) {
                if (Commands[index] == null)
                    Commands[index] = CreateCommand(idCommand);
            }

            return Commands[index];
        }

        private static RoutedUICommand CreateCommand(CommandId idCommand)
        {
            return idCommand switch
            {
                CommandId.Toggle => new RoutedUICommand("Toggle", "Toggle", typeof(ExtensionCommands)) {
                    InputGestures = {
                            new KeyGesture(Key.T, ModifierKeys.Control)
                        }
                },

                CommandId.Rename => new RoutedUICommand("Rename", "Rename", typeof(ExtensionCommands)) {
                    InputGestures = {
                            new KeyGesture(Key.F2)
                        }
                },

                _ => throw new ArgumentOutOfRangeException(nameof(idCommand), idCommand, null),
            };
        }

        private enum CommandId : byte
        {
            Toggle,
            Rename,
            Count
        }
    }
}
