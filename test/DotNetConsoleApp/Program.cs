namespace ConsoleApp
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Threading;

    public class Program
    {
        public static void Main(string[] args)
        {
            var es = new MinimalEventSource();
            for (int i = 0; i < 5; ++i) {
                es.EventWrite(i);
                Console.WriteLine("DotNetConsoleApp {0}", i);
                Thread.Sleep(1000);
            }
        }
    }

    [EventSource(Guid = "{5AB0948E-C045-411A-AC12-AC455AFA8DF2}")]
    internal sealed class MinimalEventSource : EventSource
    {
        [Event(1)]
        public void EventWrite(int id)
        {
            WriteEvent(1, id);
        }
    }
}
