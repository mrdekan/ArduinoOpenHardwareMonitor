using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using OpenHardwareMonitor.Hardware;
using System.Runtime.InteropServices;
using System.IO;
using mvd = Microsoft.VisualBasic.Devices;//OpenHardwareMonitor has the same reference
using System.Text;

namespace ArduinoOpenHardwareMonitor
{
    class Program
    {
        //For hiding window
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        /*Build problem
        If you cannot build the application 
        because it is still running in the background, 
        then close it through the task manager 
        (if it is not there, then close visual studio 
        and check again)
        */

        private static SerialPort _serialPort;
        private static Computer thisComputer;
        private static mvd.ComputerInfo PC;
        private static bool connected = false;
        private static double totalRAM = 0;
        //Page settings
        private static int page = 1; //You can change this value to change the start page
        private static int allPages = 3;

        public static void Main()
        {
            //Getting serial port name from user or from the file "Setting.txt"
            //You also can find this file and change it if you set your Arduino to another port
            string port = "";
            if (!portIsValid(GetText()))
            {
                foreach (string s in SerialPort.GetPortNames())
                    if (s != "COM1") Console.WriteLine(s);
                while (!portIsValid(port))
                {
                    Console.WriteLine("Select port:");
                    port = Console.ReadLine().ToUpper();
                    if (!portIsValid(port)) Console.WriteLine("Enter a correct port");
                }
            }
            else port = GetText();
            Console.WriteLine("Connecting...");
            Task task = Save(port);
            PC = new mvd.ComputerInfo();
            totalRAM = Math.Round(PC.TotalPhysicalMemory / 1073741824.0, 1); //1024^3 to get Gb from bytes
            //Opening serial port
            _serialPort = new SerialPort();
            _serialPort.PortName = port;
            _serialPort.BaudRate = 9600;
            _serialPort.Open();

            //Comment out next line to avoid hiding the window
            ShowWindow(GetConsoleWindow(), 0);


            _serialPort.DataReceived += new SerialDataReceivedEventHandler
            (DataReceivedHandler);
            thisComputer = new Computer()
            {
                CPUEnabled = true,
                GPUEnabled = true,
            };
            thisComputer.Open();

            //Submitting a request for Arduino
            //You can change the text in request here and in the arduino code
            _serialPort.Write("123");

            while (true)
            {
                if (connected)
                {
                    string temp = "";
                    switch (page)
                    {
                        case 1:
                            temp = mainPage();
                            break;
                        case 2:
                            temp = gpuPage();
                            break;
                        case 3:
                            temp = cpuPage();
                            break;
                    }
                    try
                    {
                        _serialPort.Write(temp);
                        Thread.Sleep(1200);
                    }
                    catch
                    {
                        Console.Clear();
                        Console.WriteLine("At " + string.Format("{0:HH:mm:ss tt}", DateTime.Now) + " something went wrong :(");
                        Console.WriteLine("Disconnected");
                        connected = false;
                        Environment.Exit(0);
                    }
                }
            }
        }
        //Reading from the file "Settings.txt"
        private static string GetText()
        {
            if (File.Exists("Settings.txt"))
                return File.ReadAllText("Settings.txt");
            File.Create("Settings.txt");
            return "";
        }
        //Saving to "Settings.txt"
        private static async Task Save(string text)
        {
            File.WriteAllText("Settings.txt", "");
            File.WriteAllText("Settings.txt", text);
        }
        private static bool portIsValid(string port)
        {
            if (port != "COM1" && port.Length == 4 && port.StartsWith("COM") && Char.IsDigit(port[port.Length - 1]) && SerialPort.GetPortNames().Contains(port)) return true;
            return false;
        }
        //Receiver for serial port
        private static void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            if (indata != null)
            {
                if (indata == "1")
                {
                    page++;
                    if (page > allPages) page = 1;
                }
                else if (indata == "s")
                {
                    Console.Clear();
                    Console.WriteLine("Connected");
                    connected = true;
                }
            }
        }
        private static string mainPage()
        {
            double CPUusage = 0, GPUusage = 0;
            double CPUtemp = 0, GPUtemp = 0;
            foreach (var hardwareItem in thisComputer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.CPU)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load)
                        {
                            if (sensor.Name == "CPU Total")
                                CPUusage = Math.Round(sensor.Value.Value, 0);
                        }
                        else if (sensor.SensorType == SensorType.Temperature)
                            if (sensor.Name == "CPU Package")
                                CPUtemp = sensor.Value.Value;
                    }
                }
                else if (hardwareItem.HardwareType == HardwareType.GpuAti || hardwareItem.HardwareType == HardwareType.GpuNvidia)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load)
                        {
                            if (sensor.Name == "GPU Core")
                                GPUusage = sensor.Value.Value;
                        }
                        else if (sensor.SensorType == SensorType.Temperature)
                            if (sensor.Name == "GPU Core")
                                GPUtemp = sensor.Value.Value;
                    }
                }
            }
            StringBuilder temp = new StringBuilder();
            var performance = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            var memory = performance.NextValue();
            double ram = totalRAM - Math.Round(memory / 1000, 1);
            temp.Append("CPU&GPU temp: " + CPUtemp.ToString() + " " + GPUtemp.ToString());
            while (temp.Length < 20) temp.Append(" ");
            temp.Append("RAM: " + ram.ToString() + $"/{totalRAM}Gb");
            while (temp.Length < 40) temp.Append(" ");
			temp.Append("CPU load: " + CPUusage.ToString() + "%");
            while (temp.Length < 60) temp.Append(" ");
			temp.Append("GPU load: " + GPUusage.ToString() + "%");
            while (temp.Length < 80) temp.Append(" ");
			return temp.ToString();
        }
        private static string gpuPage()
        {
			StringBuilder temp = new StringBuilder();
			temp.Append("GPU load: ");
            double GPUusage = 0;
            string clockCore = "0";
            string clockMemory = "0";
            string firstTemp = "0";
            string secondTemp = "0";
            string power = "0";
            foreach (var hardwareItem in thisComputer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.GpuAti || hardwareItem.HardwareType == HardwareType.GpuNvidia)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Name == "GPU Core")
                            GPUusage = sensor.Value.Value;
                        else if (sensor.SensorType == SensorType.Clock)
                        {
                            if (sensor.Name == "GPU Core") clockCore = sensor.Value.Value.ToString();
                            else clockMemory = sensor.Value.Value.ToString();
                        }
                        else if (sensor.SensorType == SensorType.Temperature)
                        {
                            if (sensor.Name == "GPU Core")
                                firstTemp = sensor.Value.Value.ToString();
                            else if (sensor.Name == "GPU Hot Spot")
                                secondTemp = sensor.Value.Value.ToString();
                        }
                        else if (sensor.SensorType == SensorType.Power)
                            power = sensor.Value.Value.ToString();
                    }
                }
            }
            temp.Append(GPUusage.ToString() + "%");
            while (temp.Length < 20) temp.Append(" ");
			temp.Append("Clocks: " + clockMemory + " " + clockCore);
            while (temp.Length < 40) temp.Append(" ");
			temp.Append("GPU temps: " + firstTemp + (secondTemp == "0" ? " " + secondTemp : ""));
            while (temp.Length < 60) temp.Append(" ");
			temp.Append("Power: " + power + "W");
            while (temp.Length < 80) temp.Append(" ");
			return temp.ToString();
        }
        private static string cpuPage()
        {
			StringBuilder temp = new StringBuilder();
			string CPUpower = "0";
            string CPUtemp = "0";
            string CPUload = "0";
            string CPUclock = "0";
            foreach (var hardwareItem in thisComputer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.CPU)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Power && sensor.Name == "CPU Package") CPUpower = Math.Round(sensor.Value.Value, 3).ToString();
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name == "CPU Core #1") CPUclock = Math.Round(sensor.Value.Value / 1000, 2).ToString();
                        else if (sensor.SensorType == SensorType.Temperature && sensor.Name == "CPU Package") CPUtemp = sensor.Value.Value.ToString();
                        else if (sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total") CPUload = Math.Round(sensor.Value.Value, 0).ToString();
                    }
                }
            }
            temp.Append("CPU load: " + CPUload + "%");
            while (temp.Length < 20) temp.Append(" ");
			temp.Append("CPU clock: " + CPUclock + "GHz");
            while (temp.Length < 40) temp.Append(" ");
			temp.Append("CPU temp: " + CPUtemp);
            while (temp.Length < 60) temp.Append(" ");
			temp.Append("CPU power: " + CPUpower + "W");
            while (temp.Length < 80) temp.Append(" ");
			return temp.ToString();
        }
    }
}