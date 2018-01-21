# com-socket-bridge
ComSocket Bridge provides a two-way communication bridge between a COM/Serial PORT and TCP/IP

## Easiest Installation
Simply run the exe and it will:
1. Install the Windows Service
2. Run the Windows Service

## Uninstallation
1. Run the exe with this parameter: uninstall
    ```
    comsocketbridge.exe uninstall
    ```
    
## Configuration
Open comsocketbridge.exe.config

#### Settings
* **isRunAsService** - if true, application will automatically install and start the service
* **comport** - COM/Serial Port to listen on
* **combaudrate** - COM/Serial Baud Rate. Valid value: integer
* **comstopbits** - COM/Serial Stop Bits. 
    * [Valid Values](https://msdn.microsoft.com/en-us/library/system.io.ports.stopbits)
* **comparity** -  COM/Serial Parity. 
    * [Valid Values](https://msdn.microsoft.com/en-us/library/system.io.ports.parity)
* **comdatabits** - COM/Serial Data Bits. Valid value: integer
* **comhandshake** - CCOM Handshake. 
    * [Valid Values](https://msdn.microsoft.com/en-us/library/system.io.ports.handshake)
* **tcpport** - TCP Port
* **retryOnComFailms** - If COM connection failed, this is the interval to retry COM connection
  * milliseconds.
  * Comment out or set to -1 to disable retry logic
* **logfilefolder** - folder for the logfile location
  * Comment out to disable
  * File format: comsocketbridge.MMddyyy.txt
  * If unable to write to log, application will continue to run, just without logging.
* **greet** - Greets clients on connect.