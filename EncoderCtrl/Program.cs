using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

// WiringPI 
// Official URL : http://wiringpi.com/reference/core-functions/
// Note that only wiringPi pin 1 (BCM_GPIO 18) supports PWM output 
// and only wiringPi pin 7 (BCM_GPIO 4) supports CLOCK output modes.

namespace EncoderCtrl
{
    // 増分式ロータリーエンコーダーの読み取り方法
    // A相,B相をINPUTに設定
    // 
    // CC 分解能      | 1 | 2 | / | 4 |
    //    割り込みPIN | A↑| A↓| B↑| B↓|
    //    デジタルPIN | BL| BH| AH| AL|
    //CCW 分解能      | 1 | 2 | / | 4 |
    //    割り込みPIN | A↑| A↓| B↑| B↓|
    //    デジタルPIN | BH| BL| AL| AH|
    //
    // 基本は、A相ピンのRaise割り込みで読み取って、B相の状態(H/L)で回転方向を決定する
    // 回転速度は, 制御周期の前後の絶対値の差分で確認する.
    // 分解能を上げたい場合は、A相のDown割り込みも使う.
    // 4倍分解能は, B相ピンも割り込みを入れる.

    class Program
    {
        // PI
        public const int PI_DGTL_INPUT = 0;
        public const int PI_DGTL_OUTPUT = 1;

        public const int PI_ENC_EDGE_FALLING = 1;
        public const int PI_ENC_EDGE_RISING = 2;

        public const int PI_ENC_A_PHASE_PIN = 5; // GPIO5
        public const int PI_ENC_B_PHASE_PIN = 6; // GPIO6

        // Logic level
        public const int PI_LOGIC_LOW = 0;
        public const int PI_LOGIC_HIGH = 1;

        // Motor & Encoder
        public const int MOTOR_ENC_PPR = 11; // 11パルス/回転
        

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
        extern static int digitalRead(int pin);

        [DllImport("wiringPi")]
        extern static int wiringPiISR(int pin, int edgeType, CallbackFunc func);
        // ピン割り込み

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CallbackFunc();

        // GPIOピンの初期設定をする
        public static void initGPIOPin(int gpio, int direction)
        {
            if (Directory.Exists("/sys/class/gpio/gpio" + gpio) == false)
            {
                // /sys/class/gpio/export にGPIOピン番号を書き込む
                using (var writer = new StreamWriter("/sys/class/gpio/export"))
                {
                    writer.WriteLine(gpio.ToString());
                }

                Thread.Sleep(100);

                // 入力or出力
                // /sys/class/gpio/gpioX/direction
                using (var writer = new StreamWriter("/sys/class/gpio/gpio" + gpio + "/direction"))
                {
                    if (direction == 0)
                    {
                        writer.WriteLine("in");
                        Console.WriteLine("GPIO{0} set to {1}", gpio, "in");
                    }
                    else if (direction == 1)
                    {
                        writer.WriteLine("out");
                        Console.WriteLine("GPIO{1} set to {1}", gpio, "out");
                    }

                     writer.Close();
                }
            }
        }

        // 分解能1のエンコーダ読み取り
        public static void testRev1()
        {
            int ret = 0;

            initGPIOPin(PI_ENC_A_PHASE_PIN, PI_DGTL_INPUT);
            Console.WriteLine("[Done] initGPIOPin");

            // wiringPiのセットアップ
            wiringPiSetupGpio();
            Console.WriteLine("[Done] wiringPiSetupGpio");

            // GPIO5,GPIO6をINPUTに設定
            pinMode(PI_ENC_A_PHASE_PIN, PI_DGTL_INPUT);
            pinMode(PI_ENC_B_PHASE_PIN, PI_DGTL_INPUT);

            int pulseCount = 0;
            int resolve = 1;

            // A相パルスのRaise時のデリゲート
            CallbackFunc cbAPhaseRaise = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_B_PHASE_PIN) == PI_LOGIC_LOW)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // GPIO5がONになったらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_A_PHASE_PIN, PI_ENC_EDGE_RISING, cbAPhaseRaise);

            // 無限に待機
            Thread.Sleep(Int32.MaxValue);
        }

        // 分解能2のエンコーダ読み取り
        public static void testRev2()
        {
            int ret = 0;

            initGPIOPin(PI_ENC_A_PHASE_PIN, PI_DGTL_INPUT);
            Console.WriteLine("[Done] initGPIOPin");

            // wiringPiのセットアップ
            wiringPiSetupGpio();
            Console.WriteLine("[Done] wiringPiSetupGpio");

            // GPIO5,GPIO6をINPUTに設定
            pinMode(PI_ENC_A_PHASE_PIN, PI_DGTL_INPUT);
            pinMode(PI_ENC_B_PHASE_PIN, PI_DGTL_INPUT);

            int pulseCount = 0;
            int resolve = 2;

            // A相パルスのRaise時のデリゲート
            CallbackFunc cbAPhaseRaise = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_B_PHASE_PIN) == PI_LOGIC_LOW)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // A相パルスのFall時のデリゲート
            CallbackFunc cbAPhaseFall = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_B_PHASE_PIN) == PI_LOGIC_HIGH)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // GPIO5がRaiseしたらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_A_PHASE_PIN, PI_ENC_EDGE_RISING, cbAPhaseRaise);
            // GPIO5がFallしたらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_A_PHASE_PIN, PI_ENC_EDGE_FALLING, cbAPhaseFall);

            // 無限に待機
            Thread.Sleep(Int32.MaxValue);
        }

        // 分解能4のエンコーダ読み取り
        public static void testRev4()
        {
            int ret = 0;

            initGPIOPin(PI_ENC_A_PHASE_PIN, PI_DGTL_INPUT);
            Console.WriteLine("[Done] initGPIOPin");

            // wiringPiのセットアップ
            wiringPiSetupGpio();
            Console.WriteLine("[Done] wiringPiSetupGpio");

            // GPIO5,GPIO6をINPUTに設定
            pinMode(PI_ENC_A_PHASE_PIN, PI_DGTL_INPUT);
            pinMode(PI_ENC_B_PHASE_PIN, PI_DGTL_INPUT);

            int pulseCount = 0;
            int resolve = 4;

            // A相パルスのRaise時のデリゲート
            CallbackFunc cbAPhaseRaise = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_B_PHASE_PIN) == PI_LOGIC_LOW)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // A相パルスのFall時のデリゲート
            CallbackFunc cbAPhaseFall = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_B_PHASE_PIN) == PI_LOGIC_HIGH)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // B相パルスのRaise時のデリゲート
            CallbackFunc cbBPhaseRaise = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_A_PHASE_PIN) == PI_LOGIC_HIGH)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // B相パルスのFall時のデリゲート
            CallbackFunc cbBPhaseFall = delegate () {
                // パルスを受けたらカウンタを加算する
                pulseCount++;

                // 11回転で1回転
                if (pulseCount % MOTOR_ENC_PPR == 0) 
                {
                    string direction;

                    // B相の状態でCC/CCWを判定
                    if (digitalRead(PI_ENC_A_PHASE_PIN) == PI_LOGIC_LOW)
                    {
                        // CC
                        direction = "CC";
                    }
                    else
                    {
                        // CCW
                        direction = "CCW";
                    }

                    int rotation = pulseCount / (MOTOR_ENC_PPR * resolve);
                    Console.WriteLine("方向 : {0} 回転数 : {1}", direction, rotation);
                }
            };

            // GPIO5がRaiseしたらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_A_PHASE_PIN, PI_ENC_EDGE_RISING, cbAPhaseRaise);
            // GPIO5がFallしたらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_A_PHASE_PIN, PI_ENC_EDGE_FALLING, cbAPhaseFall);
            // GPIO6がRaiseしたらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_B_PHASE_PIN, PI_ENC_EDGE_RISING, cbBPhaseRaise);
            // GPIO6がFallしたらコールバックを呼ぶ
            ret = wiringPiISR(PI_ENC_B_PHASE_PIN, PI_ENC_EDGE_FALLING, cbBPhaseFall);

            // 無限に待機
            Thread.Sleep(Int32.MaxValue);
        }

        static void Main(string[] args)
        {
            // Console.WriteLine("[testRev1]");
            // testRev1();
            // Console.WriteLine("[testRev2]");
            // testRev2();
            Console.WriteLine("[testRev4]");
            testRev4();
            
        }
    }
}
