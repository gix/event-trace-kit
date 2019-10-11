namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;

    public class KeyDownEventTrigger : EventTrigger
    {
        public KeyDownEventTrigger() : base("KeyDown")
        {
        }

        public Key Key { get; set; }
        public ModifierKeys? Modifiers { get; set; }

        protected override void OnEvent(EventArgs eventArgs)
        {
            if (eventArgs is KeyEventArgs keyEventArgs && Matches(keyEventArgs))
                InvokeActions(eventArgs);
        }

        private bool Matches(KeyEventArgs args)
        {
            return args.Key == Key &&
                   (Modifiers == null || args.KeyboardDevice.Modifiers == Modifiers.Value);
        }
    }
}
