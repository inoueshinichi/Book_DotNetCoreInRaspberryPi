// See https://aka.ms/new-console-template for more information

using System;
using System.Runtime.InteropServices;

// WiringPI 
// Official URL : http://wiringpi.com/reference/core-functions/
// Note that only wiringPi pin 1 (BCM_GPIO 18) supports PWM output 
// and only wiringPi pin 7 (BCM_GPIO 4) supports CLOCK output modes.


namespace SoftPWMCtrlLED
{
    class Program
    {
        public const int PWM_MODE_MS = 0;
        public const int PWM_OUTPUT = 4;

        [DllImport("wiringPi")]
        extern static int wiringPiSetupGpio();

        [DllImport("wiringPi")]
        extern static void softPwmCreate(int pin, int value, int range);

        [DllImport("wiringPi")]
        extern static void softPwmWrite(int pin, int value);

        static void Main(string[] args)
        {
            // wiringPiのセットアップ
            wiringPiSetupGpio();

            // ソフトPWMの作成
            softPwmCreate(PWM_OUTPUT, 0, 1024); // gpio: 2, init: 0, range: 1024

            // 512dutyで発光させる
            softPwmWrite(PWM_OUTPUT, 512);

            System.Threading.Thread.Sleep(3000);

            // 256dutyで発光させる
            softPwmWrite(PWM_OUTPUT, 256);

            System.Threading.Thread.Sleep(3000);

            // 128dutyで発光させる
            softPwmWrite(PWM_OUTPUT, 128);

            System.Threading.Thread.Sleep(3000);

            // 64dutyで発光させる
            softPwmWrite(PWM_OUTPUT, 64);

            System.Threading.Thread.Sleep(3000);

        }
    }
}
