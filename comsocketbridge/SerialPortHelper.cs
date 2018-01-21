using System;
using System.IO.Ports;

namespace ComSocketBridge
{
    class SerialPortHelper
    {
        private SerialPort _serialPort;
        private string _buffer = "";
        private LogDelegate _logDelegate;
        public event SerialDataReceivedDelegate OnSerialDataRecieved;
        public event SerialConnectFailedHandler OnSerialConnectFail;
        public event SerialConnectSuccessHandler OnSerialConnectSuccess;
        public string ComPort;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        public SerialPortHelper(string comport, LogDelegate logDelegate)
        {
            ComPort = comport;
            _logDelegate = logDelegate;
        }

        public void DisconnectSerial()
        {
            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                Log($"Serial Port ({ComPort}) disconnected.");
            }
        }

        public bool Connect()
        {
            // Create Serial
            _serialPort = new SerialPort(ComPort)
            {
                BaudRate = ConfigManager.ComBaudRate,
                Parity = ConfigManager.ComParity,
                StopBits = ConfigManager.ComStopBits,
                DataBits = ConfigManager.ComDataBits,
                Handshake = ConfigManager.ComHandShake
            };

            _serialPort.DataReceived += DataReceivedHandler;

            try
            {
                _serialPort.Open();
            }
            catch (Exception e)
            {
                OnSerialConnectFail?.Invoke(this, new MessengerEventArgs(e.Message));
                return false;
            }

            OnSerialConnectSuccess?.Invoke(this, new MessengerEventArgs("connected"));
            return true;
        }


        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            _buffer += sp.ReadExisting();
            if (_buffer.Contains("\r"))
            {
                Log(_buffer);
                Broadcast(_buffer);
                _buffer = String.Empty;
            }
        }

        public void Send(object sender, MessengerEventArgs args)
        {
            Send(args.Message);
        }

        public void Send(string message)
        {
            if (!IsConnected)
            {
                Log("Unable to send to serial!");
                return;
            }
            try
            {
                byte[] m = System.Text.Encoding.UTF8.GetBytes(message);
                _serialPort.Write(m, 0, m.Length);
            }
            catch (Exception)
            {
                Log("Unable to send to serial!");
                return;
            }
        }

        private void Broadcast(string msg)
        {
            if (!String.IsNullOrEmpty(msg))
                OnSerialDataRecieved?.Invoke(this, new MessengerEventArgs(msg));
        }

        private void Log(string msg)
        {
            if (!String.IsNullOrEmpty(msg))
            {
                _logDelegate?.Invoke(msg);
            }
        }
    }
}
