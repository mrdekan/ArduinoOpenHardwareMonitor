#include <Wire.h>
#include <LiquidCrystal_I2C.h>
//Button pin
#define PIN_BUTTON 2
//Display settings for 2004A
LiquidCrystal_I2C lcd(0x27, 20, 4);

bool buttonNext = true;
String receiveVal;
void setup()
{
  //Initialize button and interrupting
  pinMode(PIN_BUTTON, INPUT_PULLUP);
  attachInterrupt(0, btnIsr, FALLING);
  
  //Initialize serial port
  Serial.begin(9600);
  
  //Initialize display
  lcd.init();
  lcd.backlight();
  lcd.noAutoscroll();
  lcd.print("Waiting for connection...");
}
//On button pressed
void btnIsr() {
  if (digitalRead(PIN_BUTTON) == LOW && buttonNext) {
    Serial.print("1");
    buttonNext = false;
    delay(100);
  }
  else if (digitalRead(PIN_BUTTON) == HIGH) buttonNext = true;
}
void loop()
{
  if (Serial.available() > 0)
  {
    receiveVal = Serial.readString();    
    if (receiveVal != "123") {
      lcd.setCursor(0, 0);
      lcd.print(receiveVal);
    }
    else {
      //Answering or request
      Serial.print("s");
      lcd.clear();
      lcd.print("Connected");
    }
  }
}
