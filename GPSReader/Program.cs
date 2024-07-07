using System;
using System.Threading;
using System.Linq;
//using System.Linq.Expression;
//using System.Collections;
//using System.Colllections.Generic;
using System.Globalization;
//using System.DateTime;
//using System.Diagnostic;
//using System.Attribute;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using RJCP.IO.Ports;
using NmeaParser;

class Program
{
    static void Main(string[] args)
    {
        // GPSモジュールにシリアル接続する.
        SerialPortStream serial = new SerialPortStream("/dev/serial0", 
                                                       9600, 
                                                       8,
                                                       Parity.None,
                                                       StopBits.One);
        serial.OpenDirect();
        //serial.Open();

        // シリアルポートのNMEAパーサーに接続する
        NmeaDevice device = new NmeaParser.StreamDevice(serial);

        // NMEAパーサがメッセージを解析した後のイベントハンドラを設定する.
        device.MessageReceived += (object sender, NmeaMessageReceivedEventArgs args) => {
            //if (e.Message.GetType().Equals(typeof(NmeaParser.Message.Gll)))
            //{
            //    NmeaParser.Message.Gll gll = (NmeaParser.Message.Gll)e.Message;
            //    Console.WriteLine("TimeStamp:{0}, Latitude:{1}, Longitude:{2}", gll.FixTime, gll.Latitude, gll.Longitude);
            //}
            if (args.Message is NmeaParser.Messages.Rmc rmc)
            {
                Console.WriteLine("TimeStamp: {0}, Latitude:{1}, Longitude:{2}",
                                  rmc.FixTime, rmc.Latitude, rmc.Longitude);
            }
        };

        // Connect NMEA parser
        device.OpenAsync();

        // Sleep main thread for waiting
        System.Threading.Thread.Sleep(Int32.MaxValue);

        //serial.Close();
    }
}

