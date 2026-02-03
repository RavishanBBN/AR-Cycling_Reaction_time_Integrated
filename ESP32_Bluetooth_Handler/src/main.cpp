#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BNO055.h>

#include <BleKeyboard.h>
#include <BLEDevice.h>
#include <BLESecurity.h>
#include <BLE2902.h>

// RTC and SD card for timestamped logging
#include <PCF8563.h>
#include <SPI.h>
#include <SD.h>
#include <FS.h>

// =======================
// GPIO CONFIG (SET THESE)
// =======================
// IMPORTANT: Use real GPIO numbers (NOT D4/D10 macros).
static const int BTN_GPIO     = 4;   // Push button (moved from 5)
static const int ENCODER_A_GPIO = 2; // Encoder A
static const int ENCODER_B_GPIO = 1; // Encoder B (moved from 7)
static const int I2C_SDA_GPIO = 5;   // I2C SDA (D4) - expansion board Grove connector
static const int I2C_SCL_GPIO = 6;   // I2C SCL (D5) - expansion board Grove connector
static const int SD_CS_GPIO = 3;     // SD card chip select (D2) - expansion board SD slot

// =======================
// BIKE CONFIG
// =======================
static const float WHEEL_DIAMETER_MM = 700.0f;
static const int   ENCODER_PPR = 600;                 // mechanical PPR
static const float COUNTS_PER_REV = ENCODER_PPR * 4.0f; // your ISR scheme is effectively 4x
static const float TILT_WARNING_ANGLE = 30.0f;
static const float TILT_DANGER_ANGLE  = 45.0f;

// =======================
// BLE CONFIG (your working style)
// =======================
static const char* BLE_DEVICE_NAME   = "HL2_Button_Test";
static const char* BLE_MANUFACTURER  = "Seeed_XIAO_ESP32C3";
static const uint8_t BLE_BATTERY_LEVEL = 100;

BleKeyboard bleKeyboard(BLE_DEVICE_NAME, BLE_MANUFACTURER, BLE_BATTERY_LEVEL);

enum OutputMode { MODE_CHAR_A, MODE_SPACE };
volatile OutputMode outputMode = MODE_CHAR_A;

static const uint32_t DEBOUNCE_MS = 30;
static const uint32_t HOLD_BLOCK_MS = 250;

// debounce state
bool lastRaw = HIGH;
bool stableState = HIGH;
uint32_t lastRawChangeMs = 0;
uint32_t lastPressMs = 0;

// event flag for CSV output (latched until next sample)
volatile bool btnEventLatched = false;

// =======================
// ENCODER STATE
// =======================
volatile int32_t encoderCount = 0;
volatile unsigned long lastPulseTime = 0;

// =======================
// IMU
// =======================
Adafruit_BNO055 bno = Adafruit_BNO055(55, 0x28);
bool imuOk = false;

// =======================
// RTC (PCF8563)
// =======================
PCF8563 rtc;
bool rtcOk = false;

// =======================
// SD CARD LOGGING
// =======================
static const int SD_BUFFER_SIZE = 20;  // Write to SD every 20 readings (~4 seconds at 200ms)
String sdLogBuffer = "";
int sdBufferCount = 0;
File sdFile;
bool sdOk = false;
String currentDateFile = "";

// =======================
// LOGGING
// =======================
static const uint32_t SAMPLE_PERIOD_MS = 200;

// =======================
// BLE UART (Nordic UART Service)
// =======================
static const char* NUS_SERVICE_UUID = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
static const char* NUS_RX_UUID      = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
static const char* NUS_TX_UUID      = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

BLECharacteristic* nusTx = nullptr;
bool nusConnected = false;

class NusServerCallbacks : public BLEServerCallbacks {
  void onConnect(BLEServer* pServer) override {
    nusConnected = true;
  }
  void onDisconnect(BLEServer* pServer) override {
    nusConnected = false;
    pServer->getAdvertising()->start();
  }
};

class NusRxCallbacks : public BLECharacteristicCallbacks {
  void onWrite(BLECharacteristic* pChar) override {
    std::string value = pChar->getValue();
    if (!value.empty()) {
      // Optional: handle commands from phone here
    }
  }
};

void sendBleLine(const String& line) {
  if (!nusConnected || !nusTx) return;

  // BLE notify payloads are small; chunk into 20 bytes
  const char* data = line.c_str();
  size_t len = line.length();
  size_t i = 0;
  while (i < len) {
    size_t chunk = (len - i > 20) ? 20 : (len - i);
    nusTx->setValue((uint8_t*)(data + i), chunk);
    nusTx->notify();
    i += chunk;
    delay(2);
  }

  // newline as separate packet
  const char nl = '\n';
  nusTx->setValue((uint8_t*)&nl, 1);
  nusTx->notify();
}

// =======================
// TABLE OUTPUT
// =======================
void printHeader() {
  Serial.println("---------------------------------------------------------------------------------------------------------------");
  Serial.println("   ms | ble | btn | enc_count | speed_kmh | speed_mph | dist_m | imu |  roll | pitch |  tilt | status");
  Serial.println("---------------------------------------------------------------------------------------------------------------");
}

void printRow(uint32_t ms, bool bleConn, bool btnEvent, int32_t count,
              float speedKmh, float speedMph, float distM,
              bool imuOkLocal, float roll, float pitch, float tilt, const char* tiltStatus) {
  char buf[180];
  snprintf(buf, sizeof(buf),
           "%6lu |  %d  |  %d  | %9ld | %9.3f | %9.3f | %6.3f |  %d  | %6.2f | %6.2f | %6.2f | %s",
           (unsigned long)ms,
           bleConn ? 1 : 0,
           btnEvent ? 1 : 0,
           (long)count,
           speedKmh, speedMph, distM,
           imuOkLocal ? 1 : 0,
           imuOkLocal ? roll : NAN,
           imuOkLocal ? pitch : NAN,
           imuOkLocal ? tilt : NAN,
           tiltStatus);
  Serial.println(buf);
}

// =======================
// ENCODER ISRs (logic from your simple test)
// =======================
void IRAM_ATTR handleEncoderA() {
  bool a = digitalRead(ENCODER_A_GPIO);
  bool b = digitalRead(ENCODER_B_GPIO);

  if (a == b) encoderCount++;
  else encoderCount--;

  lastPulseTime = millis();
}

void IRAM_ATTR handleEncoderB() {
  bool a = digitalRead(ENCODER_A_GPIO);
  bool b = digitalRead(ENCODER_B_GPIO);

  if (a != b) encoderCount++;
  else encoderCount--;

  lastPulseTime = millis();
}

// =======================
// BLE send key (robust)
// =======================
void sendKeyPress() {
  if (!bleKeyboard.isConnected()) return;

  if (outputMode == MODE_CHAR_A) {
    bleKeyboard.press('a');
    delay(8);
    bleKeyboard.releaseAll();
  } else {
    bleKeyboard.press(' ');
    delay(8);
    bleKeyboard.releaseAll();
  }
}

// LittleFS logging removed - using SD card only

// =======================
// RTC HELPER FUNCTIONS
// =======================
int monthFromStr(const char* m) {
  if (strncmp(m, "Jan", 3) == 0) return 1;
  if (strncmp(m, "Feb", 3) == 0) return 2;
  if (strncmp(m, "Mar", 3) == 0) return 3;
  if (strncmp(m, "Apr", 3) == 0) return 4;
  if (strncmp(m, "May", 3) == 0) return 5;
  if (strncmp(m, "Jun", 3) == 0) return 6;
  if (strncmp(m, "Jul", 3) == 0) return 7;
  if (strncmp(m, "Aug", 3) == 0) return 8;
  if (strncmp(m, "Sep", 3) == 0) return 9;
  if (strncmp(m, "Oct", 3) == 0) return 10;
  if (strncmp(m, "Nov", 3) == 0) return 11;
  if (strncmp(m, "Dec", 3) == 0) return 12;
  return 1;
}

void setRTCToCompileTime() {
  const char* date = __DATE__; // "Feb 02 2026"
  const char* time = __TIME__; // "HH:MM:SS"

  int month = monthFromStr(date);
  int day = atoi(date + 4);
  int year = atoi(date + 7);

  int hour = atoi(time);
  int minute = atoi(time + 3);
  int second = atoi(time + 6);

  Serial.printf("[RTC] Setting to compile time: %04d-%02d-%02d %02d:%02d:%02d\n",
                year, month, day, hour, minute, second);

  rtc.stopClock();
  rtc.setYear((uint8_t)(year % 100));
  rtc.setMonth((uint8_t)month);
  rtc.setDay((uint8_t)day);
  rtc.setHour((uint8_t)hour);
  rtc.setMinut((uint8_t)minute);
  rtc.setSecond((uint8_t)second);
  rtc.startClock();
}

void initRTC() {
  Serial.print("[RTC] Initializing PCF8563...");
  rtc.init();
  
  if (!rtc.checkClockIntegrity()) {
    Serial.println(" integrity check failed. Setting to compile time.");
    setRTCToCompileTime();
  } else {
    Serial.println(" OK.");
  }
  
  rtcOk = true;
}

String getTimestamp() {
  if (!rtcOk) return "NO_RTC";
  
  static uint8_t lastSecond = 255;
  static uint32_t secondStartMillis = 0;
  
  Time now = rtc.getTime();
  
  // Sync millisecond counter when second changes
  if (now.second != lastSecond) {
    lastSecond = now.second;
    secondStartMillis = millis();
  }
  
  uint32_t millisInSecond = (millis() - secondStartMillis) % 1000;
  
  char timestamp[30];
  snprintf(timestamp, sizeof(timestamp), 
           "20%02u-%02u-%02u %02u:%02u:%02u.%03lu",
           now.year, now.month, now.day,
           now.hour, now.minute, now.second,
           (unsigned long)millisInSecond);
  
  return String(timestamp);
}

String getCurrentDate() {
  if (!rtcOk) return "NO_DATE";
  
  Time now = rtc.getTime();
  char dateStr[15];
  snprintf(dateStr, sizeof(dateStr), "20%02u-%02u-%02u", now.year, now.month, now.day);
  return String(dateStr);
}

// =======================
// SD CARD FUNCTIONS
// =======================
void listSDFiles() {
  Serial.println("[SD] Files on card:");
  File root = SD.open("/");
  if (!root) {
    Serial.println("  Failed to open root directory");
    return;
  }
  
  if (!root.isDirectory()) {
    Serial.println("  Root is not a directory");
    root.close();
    return;
  }
  
  int fileCount = 0;
  while (true) {
    File entry = root.openNextFile();
    if (!entry) break;
    
    Serial.print("  ");
    Serial.print(entry.name());
    Serial.print(" - ");
    Serial.print(entry.size());
    Serial.println(" bytes");
    
    entry.close();
    fileCount++;
  }
  
  root.close();
  Serial.printf("[SD] Total files: %d\n", fileCount);
}

void initSD() {
  Serial.print("[SD] Initializing SD card...");
  
  SPI.begin();
  pinMode(SD_CS_GPIO, OUTPUT);
  
  if (!SD.begin(SD_CS_GPIO, SPI, 4000000)) {
    Serial.println(" FAILED. Check card/wiring.");
    sdOk = false;
    return;
  }
  
  Serial.println(" OK.");
  
  // Verify card is readable
  uint8_t cardType = SD.cardType();
  if (cardType == CARD_NONE) {
    Serial.println("[SD] No SD card detected!");
    sdOk = false;
    return;
  }
  
  Serial.print("[SD] Card Type: ");
  if (cardType == CARD_MMC) Serial.println("MMC");
  else if (cardType == CARD_SD) Serial.println("SDSC");
  else if (cardType == CARD_SDHC) Serial.println("SDHC");
  else Serial.println("UNKNOWN");
  
  uint64_t cardSize = SD.cardSize() / (1024 * 1024);
  Serial.printf("[SD] Card Size: %lluMB\n", cardSize);
  Serial.printf("[SD] Total space: %lluMB\n", SD.totalBytes() / (1024 * 1024));
  Serial.printf("[SD] Used space: %lluMB\n", SD.usedBytes() / (1024 * 1024));
  
  listSDFiles();
  
  sdOk = true;
}

bool openDailyFile() {
  if (!sdOk || !rtcOk) return false;
  
  String dateStr = getCurrentDate();
  if (dateStr == "NO_DATE") return false;
  
  // If date changed or first time, close old file and open new
  if (dateStr != currentDateFile) {
    if (sdFile) {
      sdFile.flush();
      sdFile.close();
      Serial.printf("[SD] Closed previous file: %s.csv\n", currentDateFile.c_str());
    }
    
    currentDateFile = dateStr;
    String filename = "/" + dateStr + ".csv";
    
    bool fileExists = SD.exists(filename.c_str());
    sdFile = SD.open(filename.c_str(), FILE_APPEND);
    
    if (!sdFile) {
      Serial.printf("[SD] Failed to open %s\n", filename.c_str());
      sdOk = false;
      return false;
    }
    
    // Write header if new file
    if (!fileExists || sdFile.size() == 0) {
      sdFile.println("Timestamp,Speed_kmh,Tilt_deg");
      sdFile.flush();
      Serial.printf("[SD] Created new file: %s\n", filename.c_str());
    } else {
      Serial.printf("[SD] Opened existing file: %s\n", filename.c_str());
    }
  }
  
  return true;
}

void logToSD(float speedKmh, float tilt) {
  if (!sdOk || !rtcOk) return;
  
  String timestamp = getTimestamp();
  String line = timestamp + "," + String(speedKmh, 3) + "," + String(tilt, 2);
  
  // Add to buffer
  sdLogBuffer += line + "\n";
  sdBufferCount++;
  
  // Write buffer to SD when full
  if (sdBufferCount >= SD_BUFFER_SIZE) {
    if (openDailyFile()) {
      sdFile.print(sdLogBuffer);
      sdFile.flush();
      Serial.printf("[SD] Flushed %d readings to %s.csv\n", sdBufferCount, currentDateFile.c_str());
    }
    sdLogBuffer = "";
    sdBufferCount = 0;
  }
}

// =======================
// SETUP
// =======================
void setup() {
  Serial.begin(115200);
  delay(300);

  Serial.println();
  Serial.println("===== Integrated: BLE Button + Encoder + BNO055 =====");

  // Print resolved GPIOs (so you know what you are truly using)
  Serial.printf("[PINS] BTN_GPIO=%d, ENC_A=%d, ENC_B=%d, SDA=%d, SCL=%d\n",
                BTN_GPIO, ENCODER_A_GPIO, ENCODER_B_GPIO, I2C_SDA_GPIO, I2C_SCL_GPIO);

  // Button
  pinMode(BTN_GPIO, INPUT_PULLUP);

  // Encoder
  pinMode(ENCODER_A_GPIO, INPUT_PULLUP);
  pinMode(ENCODER_B_GPIO, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(ENCODER_A_GPIO), handleEncoderA, CHANGE);
  attachInterrupt(digitalPinToInterrupt(ENCODER_B_GPIO), handleEncoderB, CHANGE);
  Serial.println("[ENC] Interrupts attached.");

  // BLE init (shared by HID + UART)
  BLEDevice::init(BLE_DEVICE_NAME);

  // BLE UART (NUS)
  BLEServer* pServer = BLEDevice::createServer();
  pServer->setCallbacks(new NusServerCallbacks());

  BLEService* pService = pServer->createService(NUS_SERVICE_UUID);
  nusTx = pService->createCharacteristic(
    NUS_TX_UUID,
    BLECharacteristic::PROPERTY_NOTIFY
  );
  nusTx->addDescriptor(new BLE2902()); // <-- CCCD

  BLECharacteristic* nusRx = pService->createCharacteristic(
    NUS_RX_UUID,
    BLECharacteristic::PROPERTY_WRITE | BLECharacteristic::PROPERTY_WRITE_NR
  );
  nusRx->setCallbacks(new NusRxCallbacks());

  pService->start();
  BLEAdvertising* pAdvertising = pServer->getAdvertising();
  pAdvertising->addServiceUUID(NUS_SERVICE_UUID);
  pAdvertising->start();

  // BLE HID (your method)
  BLESecurity* pSecurity = new BLESecurity();
  pSecurity->setAuthenticationMode(ESP_LE_AUTH_NO_BOND);
  pSecurity->setCapability(ESP_IO_CAP_NONE);
  pSecurity->setKeySize(16);
  pSecurity->setInitEncryptionKey(ESP_BLE_ENC_KEY_MASK | ESP_BLE_ID_KEY_MASK);

  bleKeyboard.begin();
  Serial.println("[BLE] Advertising started. Pair with BLE app to see UART data.");

  // I2C (ONLY ONCE)
  Wire.begin(I2C_SDA_GPIO, I2C_SCL_GPIO);
  Wire.setClock(100000); // conservative, stable
  Serial.println("[I2C] Started.");

  // IMU init
  Serial.print("[IMU] Initializing BNO055...");
  imuOk = bno.begin();
  if (!imuOk) {
    Serial.println(" FAILED (check wiring, address, pins, power).");
  } else {
    Serial.println(" OK.");
    bno.setExtCrystalUse(true);
  }

  // RTC init (shares I2C with IMU)
  initRTC();

  // SD card init
  initSD();
  if (sdOk && rtcOk) {
    openDailyFile();
    Serial.println("[SD] Ready for timestamped logging.");
  }

  printHeader();
}

// =======================
// LOOP
// =======================
void loop() {
  // ---------- Button debounce + latch event ----------
  bool raw = digitalRead(BTN_GPIO);

  if (raw != lastRaw) {
    lastRaw = raw;
    lastRawChangeMs = millis();
  }

  if ((millis() - lastRawChangeMs) > DEBOUNCE_MS && raw != stableState) {
    stableState = raw;

    if (stableState == LOW) { // press edge
      uint32_t now = millis();
      if (now - lastPressMs >= HOLD_BLOCK_MS) {
        lastPressMs = now;

        // Latch event for output/logging
        btnEventLatched = true;

        // Send HID key if connected
        sendKeyPress();
      }
    }
  }

  // ---------- Sample everything every 200 ms ----------
  static uint32_t lastSampleMs = 0;
  static int32_t lastCount = 0;
  static float totalDistanceM = 0.0f;

  uint32_t nowMs = millis();
  if (nowMs - lastSampleMs >= SAMPLE_PERIOD_MS) {
    uint32_t dtMs = nowMs - lastSampleMs;
    lastSampleMs = nowMs;

    // BLE status
    bool bleConn = bleKeyboard.isConnected();

    // Encoder snapshot
    int32_t count;
    uint32_t timeSincePulse;
    noInterrupts();
    count = encoderCount;
    timeSincePulse = nowMs - lastPulseTime;
    interrupts();

    int32_t delta = count - lastCount;
    lastCount = count;

    float countsPerSecond = (delta * 1000.0f) / (float)dtMs;
    float revPerSecond = countsPerSecond / COUNTS_PER_REV;
    float wheelCircumferenceM = (PI * WHEEL_DIAMETER_MM) / 1000.0f;

    float speedMps = revPerSecond * wheelCircumferenceM;
    float speedKmh = speedMps * 3.6f;
    float speedMph = speedKmh * 0.621371f;

    float revThis = delta / COUNTS_PER_REV;
    totalDistanceM += revThis * wheelCircumferenceM;

    if (timeSincePulse > 2000) { // stopped
      speedKmh = 0;
      speedMph = 0;
    }

    // IMU read (ONLY if imuOk)
    float roll = NAN, pitch = NAN, tilt = NAN;
    const char* tiltStatus = "IMU_FAIL";

    if (imuOk) {
      sensors_event_t event;
      bno.getEvent(&event);

      roll = event.orientation.y;
      pitch = event.orientation.z;
      tilt = sqrtf(roll * roll + pitch * pitch);

      tiltStatus = "UPRIGHT";
      if (tilt > TILT_DANGER_ANGLE) tiltStatus = "DANGER";
      else if (tilt > TILT_WARNING_ANGLE) tiltStatus = "WARNING";
    }

    // Button event for this sample
    bool btnEvent = btnEventLatched;
    btnEventLatched = false;

    // Build CSV line (for file logging + BLE UART)
    String line;
    line.reserve(140);
    line += nowMs;
    line += ",";
    line += (bleConn ? "1" : "0");
    line += ",";
    line += (btnEvent ? "1" : "0");
    line += ",";
    line += count;
    line += ",";
    line += String(speedKmh, 3);
    line += ",";
    line += String(speedMph, 3);
    line += ",";
    line += String(totalDistanceM, 3);
    line += ",";
    line += (imuOk ? "1" : "0");
    line += ",";
    line += (imuOk ? String(roll, 2) : "NA");
    line += ",";
    line += (imuOk ? String(pitch, 2) : "NA");
    line += ",";
    line += (imuOk ? String(tilt, 2) : "NA");
    line += ",";
    line += tiltStatus;

    // Serial table output
    printRow(nowMs, bleConn, btnEvent, count, speedKmh, speedMph,
             totalDistanceM, imuOk, roll, pitch, tilt, tiltStatus);

    // Send CSV over BLE UART
    sendBleLine(line);
    
    // SD card log with RTC timestamp (Speed and Tilt only)
    logToSD(speedKmh, tilt);
  }

  delay(1);
}
