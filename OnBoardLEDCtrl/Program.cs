// See https://aka.ms/new-console-template for more information


using System;
using System.IO;
using System.Text;
using System.Threading;

namespace OnBoardLEDCtrl
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // LEDのモードをnoneに設定する
            var writer = new System.IO.StreamWriter("/sys/class/leds/led0/trigger");
            writer.WriteLine("none");
            writer.Close();

            // LEDを点灯/消灯する
            int count = 0
            while (true)
            {
                writer = new System.IO.StreamWriter("/sys/class/leds/led0/brightness");
                writer.WriteLine((count % 2).ToString());
                writer.Close();

                Console.Write((count % 2).ToString());
                count++;
                Thread.Sleep(500);
            }
        }
    }
}