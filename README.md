# ArduinoOpenHardwareMonitor
A simple program for displaying information about your hardware on a 2004A display with Arduino.
To run this program you need:
1. Connect lcd 2004A display to arduino as shown in the picture (or D20 & D21 on Arduino Mega):
![alt text](https://arduino-ide.com/uploads/posts/2020-02/1582725384_podkljuchenie-lcd-2004a-k-arduino.png)
2. Connect a button to arduino (but to D2 pin):
![alt text](https://roboticsbackend.com/wp-content/uploads/2020/12/arduino_push_button_no_pull_up_down.png)
3. Upload [code](https://github.com/mrdekan/ArduinoOpenHardwareMonitor/blob/master/Arduino/HardwareMonitor/HardwareMonitor.ino) to arduino
4. Run the Microsoft Visual Studio project from the repository and run the program (the first time you turn it on, and every time it is not found the arduino on the old COM port will need to select the COM port(which will be saved in a text file))
