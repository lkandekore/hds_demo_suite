📘 HDS Demo Suite

A complete Health Diagnostic System (HDS) suite containing:

HDS Demo Server (fault ingestion + visualization)

Application A (C#) – sends faults and time series requests

Application B (Rust) – sends faults with a lightweight GUI

Shared models & capture request pipeline

This suite demonstrates fault reporting, diagnostics capture, logging, and real-time monitoring across multiple applications.

📂 Repository Structure
hds_demo_suite/
│
├── Application A/          # C# WPF app (fault simulator)
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── Models/
│   └── HdsClient.cs
│
├── application_b/          # Rust eframe/egui GUI app
│   ├── src/
│   │   ├── main.rs
│   │   └── hds_client.rs
│   ├── Cargo.toml
│   └── .gitignore
│
├── HDS Demo/               # ASP.NET Core Web API server
│   ├── Controllers/
│   ├── Models/
│   ├── Server/
│   ├── ViewModels/
│   └── Program.cs
│
├── .gitignore
└── README.md

🚀 Getting Started
1️⃣ Clone the repository
git clone https://github.com/lkandekore/hds_demo_suite.git
cd hds_demo_suite

🖥 Running the Components
🟦 1. Start the HDS Server (ASP.NET Core)
cd "HDS Demo"
dotnet run


Server runs at:

http://localhost:5005


Endpoints:

POST /api/v1/apps/register

POST /api/v1/faults/report

GET /api/v1/faults/all

🟩 2. Run Application A (C# WPF)

Inside Visual Studio / Rider:

Open Application A.csproj

Press F5 to start

Features:

Sends predefined faults (NullRef, Out of Range, Timeout, Config Missing)

Logs server responses

Sends CaptureRequest (DLT logs, PCAP, Memory dumps, Env metrics)

🟧 3. Run Application B (Rust GUI)
Prerequisites:

Install Rust:

https://rustup.rs

Run:
cd application_b
cargo run


Features:

Dark-mode UI

Fault buttons (matching App A)

Async reqwest client

Real-time log panel

🏗 Architecture Overview
+-------------------+     HTTP JSON POST     +------------------+
|  Application A     | ---------------------> |                  |
|  (C# WPF)          |                        |                  |
|                    |                        |    HDS Server    |
+-------------------+                        |   ASP.NET Core   |
                                               |   FaultRegistry |
+-------------------+     HTTP JSON POST     |    TimeSeries    |
|  Application B     | ---------------------> |    ViewModel     |
|  (Rust GUI)        |                        |                  |
+-------------------+                        +------------------+


Both applications report FaultSignature

Server aggregates, counts, timestamps and classifies

UI auto-updates using FaultReported event

Supports log capture, PCAP, thread dumps, memory dumps, etc.

📡 API Specification
POST /api/v1/apps/register
Request
{
  "application": "Application A",
  "version": "1.0.0"
}

Response
{
  "status": "registered",
  "application": "Application A"
}

POST /api/v1/faults/report
Request
{
  "ApplicationName": "Application A",
  "FaultCode": "F018",
  "Type": "F0",
  "Severity": "Error",
  "Description": "Null pointer dereference",
  "Timestamp": "2025-11-23T20:10:41Z",
  "CaptureRequest": {
    "LogFileLocation": "/var/logs/app/application_dlt.log",
    "Capture": [ "DLTLogs", "MemoryDump" ],
    "Environment": [ "CPU", "RAM", "THREADS" ]
  }
}

✔ Features

Real-time fault registry

Automatic fault classification (F0–FE)

Automatic timestamps + re-occurrence counting

Full diagnostics capture model

Multi-language clients (C#, Rust)

UI built for developer debugging

🛠 Technologies Used
Server

ASP.NET Core Web API

C#

JSON serialization

Application A

WPF / XAML

HttpClient

Newtonsoft.Json

Application B

Rust

eframe / egui

tokio

reqwest

serde

🙌 Contributing

Pull requests welcome!

📝 License

MIT License — use freely.
