using System;
using System.Runtime.InteropServices;


// WiringPI 
// Official URL : http://wiringpi.com/reference/core-functions/
// Note that only wiringPi pin 1 (BCM_GPIO 18) supports PWM output 
// and only wiringPi pin 7 (BCM_GPIO 4) supports CLOCK output modes.

class Program
{
    // PI
    public const int PI_PWM_MODE_MS = 0;
    public const int PI_PIN_OUTPUT = 1;
    public const int PI_DGTL_GPIO_1 = 23;
    public const int PI_DGTL_GPIO_2 = 24;
    public const int PI_PWM_GPIO_CH0 = 12; // 18;
    public const int PI_PWM_OUTPUT = 2;

    // Motor
    public const int MOTOR_INPUT = 0;
    public const int MOTOR_OUTPUT = 1;

    // PWM Params
    // PWM-Interval : 2.5ms
    // PWM-Ope-Range : [0.5ms(bottom) - 1.5ms(middle) - 2.5ms(top)]
    public const int PWM_MAX_RANGE = 1024;

    [DllImport("wiringPi")]
    extern static int wiringPiSetupGpio();

    [DllImport("wiringPi")]
    extern static void pinMode(int pin, int mode);
    // INPUT, OUTPUT, PWM_OUTPUT or GPIO_CLOCK

    [DllImport("wiringPi")]
    extern static void digitalWrite(int pin, int mode);
    // Writes the value HIGH or LOW (1 or 0) to the given pin 
    // which must have been previously set as an output.

    [DllImport("wiringPi")]
    extern static void pwmSetMode(int mode);

    [DllImport("wiringPi")]
    extern static void pwmSetRange(uint range);

    [DllImport("wiringPi")]
    extern static void pwmSetClock(int divisor);

    [DllImport("wiringPi")]
    extern static void pwmWrite(int pin, int value);
    // Writes the value to the PWM register for the given pin. 
    // The Raspberry Pi has one on-board PWM pin, pin 1 (BMC_GPIO 18, Phys 12) 
    // and the range is 0-1024. Other PWM devices may have other PWM ranges.
    // This function is not able to control the Pi’s on-board PWM when in Sys mode.

    public static void testMotorCWCCW()
    {
        // GPIO23をOUTPUT(1)に設定する
        pinMode(PI_DGTL_GPIO_1, PI_PIN_OUTPUT);

        // GPIO24をOUTPUT(1)に設定する
        pinMode(PI_DGTL_GPIO_2, PI_PIN_OUTPUT);

        // 初期状態としてモーターを停止する(惰性)
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_INPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_INPUT);

        // モータを正転させる
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_OUTPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_INPUT);

        // 3秒待機
        Thread.Sleep(3000);

        // モーターのブレーキ
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_OUTPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_OUTPUT);

        // モータを逆転させる
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_INPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_OUTPUT);

        // 3秒待機
        Thread.Sleep(3000);

        // モーターを停止する(惰性)
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_INPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_INPUT);

    }

    public static float getPwmDuty(float percentage)
    {
        if (percentage < 0.0f) percentage = 0.0f;
        if (percentage > 100.0f) percentage = 100.0f;

        percentage /= 100.0f;

        float duty = percentage * PWM_MAX_RANGE;
        return duty;
    }

    public static void testMotorPWM()
    {
        // GPIO23をOUTPUT(1)に設定する
        pinMode(PI_DGTL_GPIO_1, PI_PIN_OUTPUT);

        // GPIO24をOUTPUT(1)に設定する
        pinMode(PI_DGTL_GPIO_2, PI_PIN_OUTPUT);

        // GPIO12をPWM_OUTPUT(2)に設定する
        pinMode(PI_PWM_GPIO_CH0, PI_PWM_OUTPUT);

        // 初期状態としてモーターを停止する(惰性)
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_INPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_INPUT);

        // モータを正転させる
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_OUTPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_INPUT);

        float percentage = 0.0f;
        float duty = 0.0f;

        // 回転数を25%にする
        percentage = 25;
        duty = getPwmDuty(percentage);
        pwmWrite(PI_PWM_GPIO_CH0, (int)duty);
        Console.WriteLine($"[Done] pwmWrite({PI_PWM_GPIO_CH0}, {(int)duty}) at {percentage}[%]");

        // 3秒待機
        Thread.Sleep(3000);

        // 回転数を50%にする
        percentage = 50;
        duty = getPwmDuty(percentage);
        pwmWrite(PI_PWM_GPIO_CH0, (int)duty);
        Console.WriteLine($"[Done] pwmWrite({PI_PWM_GPIO_CH0}, {(int)duty}) at {percentage}[%]");

        // 3秒待機
        Thread.Sleep(3000);

        // 回転数を75%にする
        percentage = 75;
        duty = getPwmDuty(percentage);
        pwmWrite(PI_PWM_GPIO_CH0, (int)duty);
        Console.WriteLine($"[Done] pwmWrite({PI_PWM_GPIO_CH0}, {(int)duty}) at {percentage}[%]");

        // 3秒待機
        Thread.Sleep(3000);

        // 回転数を100%にする
        percentage = 100;
        duty = getPwmDuty(percentage);
        pwmWrite(PI_PWM_GPIO_CH0, (int)duty);
        Console.WriteLine($"[Done] pwmWrite({PI_PWM_GPIO_CH0}, {(int)duty}) at {percentage}[%]");

        // 3秒待機
        Thread.Sleep(3000);

        // モーターのブレーキ
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_OUTPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_OUTPUT);

         // 3秒待機
        Thread.Sleep(3000);

        // モーターを停止する(惰性)
        digitalWrite(PI_DGTL_GPIO_1, MOTOR_INPUT);
        digitalWrite(PI_DGTL_GPIO_2, MOTOR_INPUT);
    }


    static void Main(string[] args)
    {
        // wiringPiのセットアップ
        wiringPiSetupGpio();
        Console.WriteLine("[Done] wiringPiSetupGpio");


        /* モータ : 停止 -> 正転 -> ブレーキ -> 逆転 -> 停止 */
        // Console.WriteLine("[Start] testMotorCWCCW()");
        // testMotorCWCCW();
        // Console.WriteLine("[End] testMotorCWCCW()");

        /* モータ : PWM -> 25% -> 50% -> 100% -> ブレーキ -> 停止 */
        Console.WriteLine("[Start] testMotorPWM()");
        testMotorPWM();
        Console.WriteLine("[End] testMotorPWM()");

    }
}