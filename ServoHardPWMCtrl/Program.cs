using System;
using System.Runtime.InteropServices;

// http://kazuki-room.com/run_servomotor_mg996r_with_hardware_pwm_of_wiringpi/
// https://logikara.blog/rc_servo/

namespace ServoHardPWMCtrl
{
    class DS3218Pro270Program
    {
        // Pi Params
        public const int PI_PWM_MODE_MS = 0;
        public const int PI_PWM_OUTPUT = 2;
        public const int PI_HARD_PWM_GPIO = 18;
        public const float PI_HARD_PWM_FREQ = 19.2f; // 19.2MHz

        // ServoMotor Params
        // Name : DS3218Pro (270°)
        // Rot-Range [-45° - 0° - 90° - 180° - 225°]
        // Operating Frequency : 50-330Hz
        // Freq : 50Hz (20ms), 100Hz (10ms), 200Hz (5ms), ...
        // DeadBand : 3μs
        // MinPulseWidth : 0.5ms
        // MaxPulseWidth : 2.4ms
        // BitNum : ? 1０
        public const int DS3218Pro270_BitNum = 10;
        public const float DS3218Pro270_MinPulseWidth = 0.5f; // 0.5ms
        public const float DS3218Pro270_MaxPulseWidth = 2.4f; // 2.4ms
        public const float DS3218Pro270_RotRange = 270.0f; // [0°,270°]
        public const float DS3218Pro270_OpeFreq = 50.0f; // 50Hz
        public const int DS3218Pro270_DutyBias = 2;
        public const float DS3218Pro270_0deg_duty = 27.0f;//25.6f; //204.8f;
        public const float DS3218Pro270_135deg_duty = 76.8f; //614.4f;
        public const float DS3218Pro270_270deg_duty = 127.0f;//122.88f; //1024f;

        // PWM Params
        // PWM-Interval : 2.5ms
        // PWM-Ope-Range : [0.5ms(bottom) - 1.5ms(middle) - 2.5ms(top)]
        public const int PWM_MAX_RANGE = 1024;

        public static float getPwmClock(float srvMtrFrq)
        {
            float pwmClock = (PI_HARD_PWM_FREQ * 1000000.0f) / (PWM_MAX_RANGE * srvMtrFrq);
            return pwmClock;
        }

        public static float getPwmDuty(float degrees)
        {
            if (degrees < -45.0f) degrees = -45.0f;
            if (degrees > 225.0f) degrees = 225.0f;
            degrees += 45.0f;

            // 0.5ms : 0°
            // 2.4ms : 270°
            float resolution = (float)Math.Pow(2, DS3218Pro270_BitNum);
            float period = 1000.0f / DS3218Pro270_OpeFreq;

            /* Duty値は分解能(2^*)で決まる */

            // float dutyMin = 27.0f;
            // float dutyMax = 127.0f;
            float dutyMin = (resolution / period) * DS3218Pro270_MinPulseWidth;
            float dutyMax = (resolution / period) * DS3218Pro270_MaxPulseWidth;

            float dutyActive = dutyMax - dutyMin;
            float duty = (degrees * (dutyActive / DS3218Pro270_RotRange)) + dutyMin;

            if (duty <= dutyMin) duty = dutyMin + 1;
            if (duty > dutyMax) duty = dutyMax;

            return duty + DS3218Pro270_DutyBias;
        }

        [DllImport("wiringPi")]
        extern static int wiringPiSetupGpio();

        [DllImport("wiringPi")]
        extern static void pinMode(int pin, int mode);

        [DllImport("wiringPi")]
        extern static void pwmSetMode(int mode);

        [DllImport("wiringPi")]
        extern static void pwmSetRange(uint range);

        [DllImport("wiringPi")]
        extern static void pwmSetClock(int divisor);

        [DllImport("wiringPi")]
        extern static void pwmWrite(int pin, int value);

        

        static void Main(string[] args)
        {
            // wiringPiのセットアップ
            wiringPiSetupGpio();
            Console.WriteLine("[Done] wiringPiSetupGpio");

            // GPIO18をPI_PWM_OUTPUT(2)に設定する
            pinMode(PI_HARD_PWM_GPIO, PI_PWM_OUTPUT);
            Console.WriteLine($"[Done] pinMode({PI_HARD_PWM_GPIO}, {PI_PWM_OUTPUT})");

            // PWMのモードをmark-spaceに設定する
            pwmSetMode(PI_PWM_MODE_MS);
            Console.WriteLine($"[Done] pwmSetMode({PI_PWM_MODE_MS})");

            // PWMのレンジを[0,1024]に設定する
            pwmSetRange(PWM_MAX_RANGE);
            Console.WriteLine($"[Done] pwmSetRange({PWM_MAX_RANGE})");

            // PWMクロックを375Hzに設定する.
            float pwmClock = getPwmClock(DS3218Pro270_OpeFreq);
            pwmSetClock((int)pwmClock);
            Console.WriteLine($"[Done] pwmSetClock({(int)pwmClock})");


            /****** モーターを回転させる ******/
            // pwmWrite(PI_HARD_PWM_GPIO, (int)DS3218Pro270_0deg_duty); // 0deg
            // Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)DS3218Pro270_0deg_duty})");

            // System.Threading.Thread.Sleep(2000);

            // pwmWrite(PI_HARD_PWM_GPIO, (int)DS3218Pro270_135deg_duty); // 135deg
            // Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)DS3218Pro270_135deg_duty})");


            // System.Threading.Thread.Sleep(1000); // 1s

            // pwmWrite(PI_HARD_PWM_GPIO, (int)DS3218Pro270_270deg_duty); // 270deg
            // Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)DS3218Pro270_270deg_duty})");

            float targetDegrees;
            float duty;

            targetDegrees = 0;
            duty = getPwmDuty(targetDegrees);
            pwmWrite(PI_HARD_PWM_GPIO, (int)duty);
            Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)duty}) at {targetDegrees}[deg]");

            System.Threading.Thread.Sleep(2000);

            targetDegrees = 180;
            duty = getPwmDuty(targetDegrees);
            pwmWrite(PI_HARD_PWM_GPIO, (int)duty);
            Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)duty}) at {targetDegrees}[deg]");

            System.Threading.Thread.Sleep(2000);

            targetDegrees = 90;
            duty = getPwmDuty(targetDegrees);
            pwmWrite(PI_HARD_PWM_GPIO, (int)duty);
            Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)duty}) at {targetDegrees}[deg]");

            System.Threading.Thread.Sleep(2000);

            targetDegrees = -45;
            duty = getPwmDuty(targetDegrees);
            pwmWrite(PI_HARD_PWM_GPIO, (int)duty);
            Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)duty}) at {targetDegrees}[deg]");

            System.Threading.Thread.Sleep(2000);

            targetDegrees = 225;
            duty = getPwmDuty(targetDegrees);
            pwmWrite(PI_HARD_PWM_GPIO, (int)duty);
            Console.WriteLine($"[Done] pwmWrite({PI_HARD_PWM_GPIO}, {(int)duty}) at {targetDegrees}[deg]");
        }
    }
}