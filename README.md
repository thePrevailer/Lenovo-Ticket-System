# Lenovo SmartFix

Lenovo SmartFix is a simple support tool for Lenovo Windows laptops.
It checks the device, highlights common problems, suggests safe fixes, and creates a support packet when the issue still needs help.

## What It Does

- Scans device health and update status
- Finds common issues like slow performance, battery drain, storage pressure, memory pressure, Wi-Fi problems, and repeated crashes
- Offers safe fixes or guided steps
- Creates a support-ready packet for unresolved issues

## Main Flow

1. Start a scan
2. Review the findings
3. Run safe fixes or follow guided steps
4. Export a support packet if the problem is not solved

## Project Parts

- `LenovoSmartFix/`
  Windows solution with the core logic, local service, and WinUI app
- `LenovoSmartFix.Web/`
  React + TypeScript web prototype of the same flow

## Tech Stack

- .NET 8
- WinUI 3
- SQLite
- React
- TypeScript
- Vite

## Goal

Keep support simple:
fix easy problems on the device first, and give support a clear summary when escalation is needed.
