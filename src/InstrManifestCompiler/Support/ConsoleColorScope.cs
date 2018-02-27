namespace InstrManifestCompiler.Support
{
    using System;

    internal sealed class ConsoleColorScope : IDisposable
    {
        private readonly ConsoleColor oldForegroundColor;
        private readonly ConsoleColor oldBackgroundColor;

        public ConsoleColorScope()
        {
            oldForegroundColor = Console.ForegroundColor;
            oldBackgroundColor = Console.BackgroundColor;
        }

        public void Reset()
        {
            Console.ForegroundColor = oldForegroundColor;
            Console.BackgroundColor = oldBackgroundColor;
        }

        void IDisposable.Dispose()
        {
            Reset();
        }
    }
}
