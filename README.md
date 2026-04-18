# SkyMinerals Management Information System

A modern web-based application for managing mineral extraction, tagging, sales, and reporting for SKYminerals Ltd.

## 🚀 Features

- Staff management (add, update, delete)
- Mineral production tracking
- Tagging of mined minerals
- Sales recording to local counters
- Automatic report generation
- Secure login with role-based access

## 🛠️ Tech Stack

- **Backend:** .NET 9 (Razor Pages)
- **Database:** PostgreSQL 16
- **ORM:** Npgsql
- **Frontend:** HTML5, CSS3, Bootstrap

## 📦 Prerequisites

- .NET 9 SDK
- PostgreSQL 16
- Git

## 🔧 Installation

```bash
# Clone the repository
git clone https://github.com/mutabazi105/SkyMineralsModern.git
cd SkyMineralsModern

# Restore dependencies
dotnet restore

# Update the connection string in appsettings.json
# "Host=localhost;Port=5432;Database=skyminerals;Username=postgres;Password=yourpassword"

# Run database migrations (if any)
dotnet run

# Start the application
dotnet run
