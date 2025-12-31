using System;
using System.IO;
using Spectre.Console;
using Fdp.Examples.Showcase.Core;

namespace Fdp.Examples.Showcase
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup Console
            try 
            { 
                if (!Console.IsOutputRedirected) Console.Clear(); 
            } catch (IOException) { /* Ignore if no console */ }
            
            AnsiConsole.Write(new FigletText("FDP Military").Color(Color.Green));

            try
            {
                var game = new ShowcaseGame();
                game.Initialize();
                game.RunLoop();
                game.Cleanup();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR: " + ex.ToString());
            }
        }
    }
}
