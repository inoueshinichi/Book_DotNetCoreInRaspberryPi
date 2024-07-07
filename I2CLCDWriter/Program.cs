using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Threading;

using System.Diagnostics;

namespace I2CLCDWriter
{
    class Program
    {
        // openシステムコール
        [DllImport("libc")]
        extern static int open(string path, int flags);

        // ioctrlシステムコール
        [DllImport("libc")]
        extern static int ioctl(int fd, uint request, int arg1);

        // writeシステムコール
        [DllImport("libc")]
        extern static int write(int fd, byte[] buf, int count);

        // closeシステムコール
        [DllImport("libc")]
        extern static int close(int fd);

        // O_RDWR定数
        const int O_RDWR = 0x0002;

        // I2C_SLAVE定数
        const int I2C_SLAVE = 0x0703;

        // SSD1306 I2Cスレーブアドレス
        const int I2C_SSD1306_ADDRESS = 0x3c;
        const int SSD1306_OLED_HEIGHT = 32;
        const int SSD1306_OLED_WIDTH = 128;

        // byte型配列の内容を16進数表示する
        static void PrintHexBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                Console.Write(bytes[i].ToString("X") + " ");
            }
            Console.WriteLine("");
        }

        // 指定アドレスのI2Cスレーブにデータを転送
        static int I2CWrite(
            /* I2Cチャネル */ int bus, 
            /* I2Cアドレス */ int slave,
            /* Command or Data */ byte reg,
            /* Payload */ byte[] payload
            )
        {
            int ret = 0;
            string dev = string.Format("/dev/i2c-{0}", bus);

            // I2Cバスをopenする
            int fd = open(dev, O_RDWR);
            if (fd < 0)
            {
                return fd;
            }

            // fdをI2Cスレーブアドレスにセットする
            ret = ioctl(fd, I2C_SLAVE, slave);
            if (ret < 0)
            {
                close(fd);
                return ret;
            }

            // 書き込みバッファの生成(regで命令orデータを区別する)
            byte[] buffer = new byte[1 + payload.Length];
            buffer[0] = reg; // 使用対象のI2Cスレーブによって命令とデータは様々.
            System.Array.Copy(payload, 0, buffer, 1, payload.Length);

            // 書き込み
            ret = write(fd, buffer, buffer.Length);
            if (ret < 0)
            {
                close(fd);
                return ret;
            }

            // ファイルをcloseする
            close(fd);

            return ret;
        }

        // 1命令系の命令送信
        static int CommandOneShotToOLEDSSD1306(
            /* I2Cバス */ int bus,
            /* I2Cアドレス */ int slave,
            /* 命令バイト */ byte command)
        {
            byte oneShot = 0x80;
            byte[] commands = { command };
            return I2CWrite(bus, slave, oneShot, commands);
        }

        // 複数命令系の命令送信
        static int CommandMultiShotToOLEDSSD1306(
            /* I2Cバス */ int bus,
            /* I2Cアドレス */ int slave,
            /* 命令バイト長 */ byte[] commands)
        {
            byte multiShot = 0x00;
            return I2CWrite(bus, slave, multiShot, commands);
        }

        // データ送信
        static int SendDataToOLEDSSD1306(
            /* I2Cバス */ int bus,
            /* I2Cアドレス */ int slave,
            /* データ */ byte[] data)
        {
            byte payload = 0x40;
            return I2CWrite(bus, slave, payload, data);
        }

        static void ClearOLEDSSD1306(
            /* I2Cバス */ int bus,
            /* I2Cアドレス */ int slave,
            /* 縦解像度 */ int height,
            /* 横解像度 */ int width)
        {
            // 32 : 0x1F, 64 : 0x3F

            // PAGE0
            //  S S                           S
            //  E E                           E
            //  G G                           G
            //  0 1                           32 or 64
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |     ~     | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            //  0 0 0                         0    0
            //  x x x                         x or x
            //  0 0 0                         1    3
            //  0 1 2                         F    F 

            // PAGE1
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |     ~     | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            //  0 0 0                         0    0
            //  x x x                         x or x
            //  0 0 0                         1    3
            //  0 1 2                         F    F 

            // ~

            // PAGE[3|7]
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |     ~     | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            // | | | | | |           | | | | | |
            //  0 0 0                         0    0
            //  x x x                         x or x
            //  0 0 0                         1    3
            //  0 1 2                         F    F 
 
            // 初期画面の生成(黒)
            int page = (int)(height / 8); // 32/8->4, 64/8->8
            byte endPage = (byte)(page - 1); // 32: 0x03, 64: 0x07

            // ページ指定
            byte[] multi3Cmds = {
                0x00, 0x00, 0x00
            };

            // ページ毎に初期化
            for (int p = 0; p < page; p++)
            {
                // 初期化するページ指定(height:32の場合, PAGE0-PAGE4, height:64の場合, PAGE0-PAGE8)
                byte pageIndex = (byte)(0xB0 | (p & endPage));
                multi3Cmds[0] = pageIndex; multi3Cmds[1] = 0x10; multi3Cmds[2] = 0x00;
                CommandMultiShotToOLEDSSD1306(bus, slave, multi3Cmds);
                
                // SEG単位で一気に初期化(黒=0x00)
                for (int w = 0; w < width; w++)
                {
                    // データ転送(1SEG=1byte)
                    byte[] black = { 0x00 };
                    SendDataToOLEDSSD1306(bus, slave, black); 
                }
            }

            // PAGE0に移動
            multi3Cmds[0] = 0xB0; multi3Cmds[1] = 0x10; multi3Cmds[2] = 0x00;
            CommandMultiShotToOLEDSSD1306(bus, slave, multi3Cmds);
        }

        static void SetupOLEDSSD1306(
            /* I2Cバス */ int bus,
            /* I2Cアドレス */ int slave,
            /* 縦解像度 */ int height,
            /* 横解像度 */ int width,
            /* 描画モード */ bool sequencial = true,
            /* 描画タイミング */ bool ignoreRAM = false,
            /* 反転モード */ bool inverse = false)
        {
            byte displayMode = (byte)(sequencial ? 0x12 : 0x02);
            byte lineMode = (byte)(height % 64 != 0 ? /*32ライン*/0x1F : /*64ライン*/0x3F);
            byte inverseMode = (byte)(!inverse ? /*通常*/0xA6 : /*反転*/0xA7);
            byte[] setupCommands = {
                0xC0, // 上下表示方向 : ↑ : 0xC0, ↓ : 0xC8
                0xA0, // 左右表示方向 : → : 0xA0, ← : 0xA1
                0xA8, lineMode, // 縦解像度 : 32ライン : 0x1F, 64ライン : 0x3F
                0xDA, displayMode, // 表示方式 : Sequential : 0x12, Alternative : 0x02
                0xD3, 0x00, // 表示縦オフセット : 0x00 - 0x1F(32ライン) or 0x3F(64ライン)
                0x40, // 描画開始ライン : 0x40 - 0x7F
                inverseMode, // 表示方法 : 通常表示 : 0xA6, 反転表示 : 0xA7
                0x8D, 0x14 // OLEDパネルの共有電圧と思われる14hに設定しないとだめらしい.
            };
            CommandMultiShotToOLEDSSD1306(bus, slave, setupCommands);

            // 画面非表示
            CommandOneShotToOLEDSSD1306(bus, slave, 0xAE);

            if (!ignoreRAM)
            {
                // RAMデータを待って描画するモード
                CommandOneShotToOLEDSSD1306(bus, slave, 0xA4);
            }
            else
            {
                // RAMデータを待たずに描画するモード
                CommandOneShotToOLEDSSD1306(bus, slave, 0xA5);
            }
 
            // 画面を初期化
            ClearOLEDSSD1306(bus, slave, height, width);

            // 画面表示
            CommandOneShotToOLEDSSD1306(bus, slave, 0xAF);
        }

        

        static void Main(string[] args)
        {
            int ret = 0;
            if (args.Length < 3)
            {
                Console.WriteLine(
                    "Usage : <I2C-bus> <I2C-slave> <LCD-output-string>"
                );
                Console.WriteLine("e.g. : 1 3c hello");
                return;
            }

            // I2Cバス番号を取得
            int bus = int.Parse(args[0]);

            // I2Cスレーブアドレスを取得
            int slave = int.Parse(args[1], NumberStyles.HexNumber);

            // LCDに出力する文字列を取得
            string text = args[2];

            // LCDの初期化
            SetupOLEDSSD1306(bus, slave, SSD1306_OLED_HEIGHT, SSD1306_OLED_WIDTH, false);

            // 出力データの生成
            byte[] multi3Cmds = { 0x00, 0x00, 0x00 };
            
            // PAGE1を指定して描画
            multi3Cmds[0] = (0xB0 | 0x00);
            multi3Cmds[1] = 0x10;
            multi3Cmds[2] = 0x00;
            CommandMultiShotToOLEDSSD1306(bus, slave, multi3Cmds);
            
            byte[] page1Bytes = {
                0x0F,
                0x0F,
                0xF0,
                0xF0,
                0x0F,
                0x0F,
                0xF0,
                0xF0,
            };
            SendDataToOLEDSSD1306(bus, slave, page1Bytes);

            // PAGE2を指定して描画
            multi3Cmds[0] = (0xB0 | 0x01);
            multi3Cmds[1] = 0x10;
            multi3Cmds[2] = 0x00;
            CommandMultiShotToOLEDSSD1306(bus, slave, multi3Cmds);

            byte[] page2Bytes = {
                0x00, // seg0
                0x00, // seg1
                0x00, // seg2
                0x00, // seg3
                0x00, // seg4
                0x00, // seg5
                0x00, // seg6
                0x00, // seg7
                0x0F, // seg8
                0x0F, // seg9
                0xF0, // seg10
                0xF0, // seg11
                0x0F, // seg12
                0x0F, // seg13
                0xF0, // seg14
                0xF0, // seg15
            };
            SendDataToOLEDSSD1306(bus, slave, page2Bytes);

            // PAGE3を指定して描画
            multi3Cmds[0] = (0xB0 | 0x02);
            multi3Cmds[1] = 0x10;
            multi3Cmds[2] = 0x00;
            CommandMultiShotToOLEDSSD1306(bus, slave, multi3Cmds);

            byte[] page3Bytes = {
                0x00, // seg+
                0x00, // seg+
                0x00, // seg+
                0x00, // seg+
                0x00, // seg+
                0x00, // seg+
                0x00, // seg+
                0x00, // seg+
                0x00, // seg0
                0x00, // seg1
                0x00, // seg2
                0x00, // seg3
                0x00, // seg4
                0x00, // seg5
                0x00, // seg6
                0x00, // seg7
                0x0F, // seg8
                0x0F, // seg9
                0xF0, // seg10
                0xF0, // seg11
                0x0F, // seg12
                0x0F, // seg13
                0xF0, // seg14
                0xF0, // seg15
            };
            SendDataToOLEDSSD1306(bus, slave, page3Bytes);

            // 画面非表示
            // CommandOneShotToOLEDSSD1306(bus, slave, 0xAE);

            // byte[] outBytes = Encoding.ASCII.GetBytes(text);

            // 出力した文字列をコンソールに表示
            Console.WriteLine(text);
            // PrintHexBytes(outBytes);
        }
    }
}
