// See https://aka.ms/new-console-template for more information

using System;
using System.IO;
using System.Threading;

namespace GPIOCtrlLED
{
   class Program
   {
       // GPIOピンの初期設定を行う.
       public static void initGPIOPin(int gpio)
       {
           if (Directory.Exists("/sys/class/gpio/gpio" + gpio) == false)
	        {
               // /sys/class/gpio/exportにGPIOピン番号を書き込む.
                using (var writer = new StreamWriter("/sys/class/gpio/export"))
                {
                    writer.WriteLine(gpio.ToString());
                    writer.Close();
                }
            }

            System.Threading.Thread.Sleep(100);

            // /sys/class/gpio/gpioX/direction
            using (var writer = new StreamWriter("/sys/class/gpio/gpio" + gpio + "/direction"))
            {
                writer.WriteLine("out");
                writer.Close();
            }
       }

        // GPIOのON/OFF制御をする.
        public static void switchGPIO(int gpio, int level)
        {
            // /sys/class/gpio/gpioX/valueの内容が1/0でON/OFFが決まる.
            using (var writer = new StreamWriter("/sys/class/gpio/gpio" + gpio + "/value"))
            {
                writer.WriteLine(level.ToString());
                writer.Close();
            }
        }

        public const int GPIO_4 = 4;

        static void Main(string[] args)
        {
            // GPIO4を初期化する
            initGPIOPin(GPIO_4);

            while (true)
            {
                // LEDを点灯する(GPIOをON)
                switchGPIO(GPIO_4, 1);

                Thread.Sleep(1000);

                // LEDを消灯する(GPIOをOFF)
                switchGPIO(GPIO_4, 0);

                Thread.Sleep(1000);
            }
        }


   } // Program
}
