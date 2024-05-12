using System;
using System.Threading;
using System.Linq;
using System.Linq.Expression;
using System.Collections;
using System.Colllections.Generic;
using System.Globalization;
using System.DateTime;
using System.Diagnostic;
using System.Attribute;
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
        serial.OpenDiect();

        // シリアルポートのNMEAパーサーに接続する
        NmeaDevice device = new NmeaParser.StreamDevice(serial);

        // NMEAパーサがメッセージを解析した後のイベントハンドラを設定する.
        device.MessageReceive += (s, e) => {
            if (e.Message.GetType().Equals(typeof(NmeaParser.Nmea.Gps.Gpsgll)))
            {
                if (e.Message.GetType().Equal(typeof)NmeaParser, Name,G;s, )
            }
        }
    }
}