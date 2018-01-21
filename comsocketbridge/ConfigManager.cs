using System;
using System.ComponentModel;
using System.Configuration;
using System.IO.Ports;

namespace ComSocketBridge
{
    static class ConfigManager
    {
        // HIS IS A GPL V3 LICENCE. SO PLEASE DO NOT REMOVE or CHANGE.
        public const string Header = "  COM Socket Bridge { Lucky Mallari(__YEAR__) }\n   https://github.com/LuckyMallari/com-socket-bridge\n";

        private static readonly int _localport;
        private static readonly string _greeting;
        private static readonly string _logfilefolder;
        private static readonly bool _isRunAsService;
        private static readonly int _retryOnComFailms;
        private static readonly string _comport;
        private static readonly int _combaudrate = 115200;
        private static readonly StopBits _comstopbits = StopBits.One;
        private static readonly int _comdatabits = 8;
        private static readonly Handshake _handshake = Handshake.None;
        private static readonly Parity _parity = Parity.None;

        public static int LocalPort => _localport;
        public static string Greeting => _greeting;
        public static string LogFileFolder => _logfilefolder;
        public static bool IsLog { get; set; }
        public static bool IsRunAsService => _isRunAsService;
        public static int RetryInterval => _retryOnComFailms;

        public static string ComPort => _comport;
        public static int ComBaudRate => _combaudrate;
        public static StopBits ComStopBits => _comstopbits;
        public static int ComDataBits => _comdatabits;
        public static Handshake ComHandShake => _handshake;
        public static Parity ComParity => _parity;


        static ConfigManager()
        {
            IsLog = true;

            _logfilefolder = GetConfig<string>("logfilefolder");
            if (string.IsNullOrEmpty(_logfilefolder))
                IsLog = false;

            _isRunAsService = GetConfig<bool>("isRunAsService");
            _localport = GetConfig<int>("tcpport");
            _greeting = GetConfig<string>("greet");
            _comport = GetConfig<string>("comport");
            _retryOnComFailms = GetConfig<int>("retryOnComFailms");

            _combaudrate = GetConfig<int>("combaudrate");
            _comstopbits = GetConfig<StopBits>("comstopbits");
            _comdatabits = GetConfig<int>("comdatabits");
            _handshake = GetConfig<Handshake>("comhandshake");
            _parity = GetConfig<Parity>("comparity");
        }

        private static T GetConfig<T>(string key)
        {
            string value = null;
            try
            {
                value = ConfigurationManager.AppSettings[key];
            }
            catch
            {
                return default(T);
            }

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter?.ConvertFromString(value);
            }
            catch (Exception e)
            {
                return default(T);
            }
        }
    }
}
