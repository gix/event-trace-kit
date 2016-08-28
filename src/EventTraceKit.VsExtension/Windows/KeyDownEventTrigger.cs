namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    public class KeyDownEventTrigger : EventTrigger
    {
        public KeyDownEventTrigger() : base("KeyDown")
        {
        }

        public Key Key { get; set; }
        public ModifierKeys? Modifiers { get; set; }

        protected override void OnEvent(EventArgs eventArgs)
        {
            var keyEventArgs = eventArgs as KeyEventArgs;
            if (keyEventArgs != null && Matches(keyEventArgs))
                InvokeActions(eventArgs);
        }

        private bool Matches(KeyEventArgs args)
        {
            return args.Key == Key &&
                   (Modifiers == null || args.KeyboardDevice.Modifiers == Modifiers.Value);
        }
    }
}
