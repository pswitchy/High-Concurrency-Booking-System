# High-Concurrency Hotel Booking Engine 🏨

![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=flat&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=flat&logo=redis&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=flat&logo=rabbitmq&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat&logo=docker&logoColor=white)

A high-performance backend system designed to handle **Flash Sale** scenarios (e.g., Agoda/Booking.com inventory). This project solves the "Double-Booking" problem using **Distributed Locking** and implements **Event-Driven Architecture** for asynchronous tasks.

## 🚀 Key Architectural Features

This project implements a multi-layered defense strategy to ensure **Strict Consistency** and **Resiliency**:

### 1. Concurrency Control 🔒
*   **Distributed Locking (RedLock):** Serializes requests across server instances using Redis. Prevents race conditions before they hit the database.
*   **Optimistic Concurrency (PostgreSQL):** Uses `xmin` (RowVersion) to enforce ACID properties. If a lock expires, the DB rejects the write if data changed.

### 2. Event-Driven Architecture (RabbitMQ) 📨
*   **Decoupling:** Uses a Producer/Consumer pattern to offload email notifications.
*   **Performance:** The Booking API returns immediately after the transaction, while the background worker handles the heavy lifting (sending emails) asynchronously.

### 3. System Resilience (Polly) 🛡
*   **Fault Tolerance:** Wraps critical database transactions in a **Retry Policy**.
*   **Exponential Backoff:** If the database encounters a transient failure or timeout, the system waits and retries (1s, 2s, 4s) instead of crashing immediately.

### 4. Idempotency 🔄
*   Prevents duplicate booking charges using Redis-backed middleware. Checks unique `Idempotency-Key` headers before processing requests.

## 🏗 Tech Stack

*   **Core:** C# .NET 8 Web API
*   **Database:** PostgreSQL (EF Core)
*   **Messaging:** RabbitMQ (RabbitMQ.Client)
*   **Caching & Locking:** Redis (StackExchange.Redis + RedLock.net)
*   **Resiliency:** Polly (Retry Policies)
*   **Testing:** Custom Load Simulator (Console App)

## 📂 Project Structure

```text
src/
├── AgodaBookingApi/             # The Main Web API
│   ├── Controllers/             # API Endpoints
│   ├── Services/
│   │   ├── BookingService.cs    # Core Logic (Locking + Polly + Producer)
│   │   ├── RabbitMqProducer.cs  # Message Queue Publisher
│   │   └── EmailBackgroundService.cs # Background Consumer
│   ├── Data/                    # EF Core Context
│   ├── Entities/                # Domain Models
│   └── Middleware/              # Idempotency Logic
│
├── BookingSimulator/            # High-Concurrency Load Tester
│   └── Program.cs               # Spawns 15+ concurrent threads
│
└── docker-compose.yml           # Infra (Postgres, Redis, RabbitMQ)
```

## ⚡ How to Run

### 1. Start Infrastructure
Spin up PostgreSQL, Redis, and RabbitMQ containers.
```bash
cd AgodaBookingApi
docker-compose up -d
```

### 2. Start the API
Open a terminal in `AgodaBookingApi` and run:
```bash
dotnet run
```
*Note the port number displayed (e.g., `http://localhost:5217`).*

### 3. Run the Race Condition Simulation
Open a **new** terminal window. Update the port in `BookingSimulator/Program.cs` if necessary, then run:
```bash
cd BookingSimulator
dotnet run
```

## 📊 Results Verification

### Load Test Output
The simulator attempts to book the **same room** with **15 concurrent users**.
*   **1 Success:** Booking confirmed + Email Queued.
*   **14 Failures:** Clean `409 Conflict` response.

<img width="387" height="331" alt="Screenshot 2025-12-06 at 6 56 33 PM" src="https://github.com/user-attachments/assets/1b14bd01-2fb8-4cb1-8d2b-7029a6aa3510" />


### Background Worker Logs
In the API terminal, you will see the asynchronous email processing:
```text
[RabbitMQ Producer] Queued email for Booking #1
[EmailBackgroundService] 📧 Sending Email -> {"BookingId":1, "Guest":"User_5", ...}
```
