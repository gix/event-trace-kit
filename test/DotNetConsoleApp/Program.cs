namespace ConsoleApp
{
    using System;
    using System.Threading;

    public class Program
    {
        public static void Main(string[] args)
        {
            for (int i = 0; i < 50; ++i) {
                Console.WriteLine("ConsoleApp {0}", i);
                Thread.Sleep(1000);
            }
        }
    }
}
