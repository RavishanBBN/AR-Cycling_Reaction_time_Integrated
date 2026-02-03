# ESP32 Bluetooth Handler

## Overview
This is the ESP32-S3 firmware for the AR-Cycling project, handling BLE communication, sensor data acquisition, and SD card logging with RTC timestamps.

## Hardware
- **MCU**: Seeed Studio XIAO ESP32-S3
- **Expansion Board**: Seeed Studio XIAO ESP32-S3 Expansion Board
- **Sensors**:
  - BNO055 9-axis IMU (tilt/orientation detection)
  - Rotary Encoder (speed measurement)
  - Push Button (user input)
  - PCF8563 RTC (real-time clock for timestamps)
- **Storage**: SD Card (8GB, FAT32 formatted)

## Pin Configuration
```
GPIO 3 (D2)  - SD Card CS (Chip Select)
GPIO 4 (D0)  - Push Button
GPIO 2       - Encoder Channel A
GPIO 1       - Encoder Channel B
GPIO 5 (D4)  - I2C SDA (IMU + RTC)
GPIO 6 (D5)  - I2C SCL (IMU + RTC)
GPIO 7 (D8)  - SD Card SCK
GPIO 8 (D9)  - SD Card MISO
GPIO 9 (D10) - SD Card MOSI
```

## Features
- **BLE HID Keyboard**: Sends 'a' or 'space' key presses
- **BLE UART (NUS)**: Streams sensor data via Nordic UART Service
- **IMU Data**: Roll, pitch, tilt angle with warning/danger thresholds
- **Speed Calculation**: Real-time speed from encoder (km/h, mph)
- **SD Card Logging**: CSV format with RTC timestamps
- **Millisecond Precision**: Timestamps accurate to millisecond level
- **Date-based Files**: Creates separate CSV file for each day (YYYY-MM-DD.csv)

## CSV Data Format
The SD card stores data in daily CSV files:
```
timestamp, speed_kmh, tilt_angle
2026-02-03 14:23:45.123, 25.4, 12.3
```

## Building and Uploading
```bash
platformio run --target upload
platformio device monitor
```

## Configuration
Key parameters in src/main.cpp:
- WHEEL_DIAMETER_MM = 700.0
- ENCODER_PPR = 600
- SAMPLE_PERIOD_MS = 200
- TILT_WARNING_ANGLE = 30.0
- TILT_DANGER_ANGLE = 45.0

## Troubleshooting
- SD card must be FAT32 formatted
- I2C devices on GPIO 5/6 (SDA/SCL)
- Button: GPIO 4 to GND (momentary push)
- LittleFS removed to prevent conflicts

## Project Status
✅ SD card logging with RTC timestamps
✅ I2C pin conflicts resolved
✅ BLE HID + UART functionality
✅ IMU and encoder working
⚠️ Button needs hardware verification
