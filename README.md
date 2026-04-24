# AdmissionPlex v2

India's comprehensive admission and career guidance platform built with .NET 10.

## Architecture

4-layer clean architecture:

| Layer | Project | Purpose |
|-------|---------|---------|
| **Core** | AdmissionPlex.Core | Domain entities, enums, interfaces (zero dependencies) |
| **Shared** | AdmissionPlex.Shared | DTOs, validators, constants (used by API + Web) |
| **API** | AdmissionPlex.Api | ASP.NET Core controllers, EF Core, PostgreSQL, business services |
| **Web** | AdmissionPlex.Web | Blazor Server interactive SSR frontend |

## Tech Stack

- .NET 10, Blazor Server, ASP.NET Core Web API
- PostgreSQL 16 + Entity Framework Core 10
- CCAvenue payment gateway
- QuestPDF for psychometric report generation

## Getting Started

### Prerequisites
- .NET 10 SDK
- PostgreSQL 16+
- Node.js (optional, for frontend tooling)

### Setup
```bash
# Clone
git clone https://github.com/your-org/AdmissionPlexVersion2.git
cd AdmissionPlexVersion2

# Restore
dotnet restore

# Update connection string in src/AdmissionPlex.Api/appsettings.json

# Run API (auto-migrates in dev)
cd src/AdmissionPlex.Api
dotnet run

# Run Web (separate terminal)
cd src/AdmissionPlex.Web
dotnet run
```

## Features
- Quick Stream Selector Test (free)
- Full Psychometric Career Assessment (10 interest + 7 aptitude categories)
- 7-section PDF report generation (MapMySkills format)
- Engineering cutoff search & predictor
- AI-powered career chat
- Counsellor session booking
- Referral system with rewards
- CCAvenue payment integration
- Admin CMS & question bank management
- Coordinator & school management
