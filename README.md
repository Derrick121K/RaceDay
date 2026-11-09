
# RaceDay Event Management System

## PROG6212 Programming 2B — RaceDay API and MVC

RaceDay is a web-based event management API for South African road running, walking and cycling events. It supports account registration, session-based authentication, event management, event categories, participant enrolments and race results.

This repository extends the planning and database work from Part 1 with an ASP.NET Core Web API and an ASP.NET Core MVC client. The MVC application calls the API over HTTP and does not access SQL Server directly.

## Technology Stack

- C# and .NET 8
- ASP.NET Core Web API with controllers
- Entity Framework Core 8
- SQL Server and LocalDB for local development
- ASP.NET Core session state
- ASP.NET Core password hashing
- Swagger / OpenAPI
- xUnit for automated testing
- GitHub Actions for continuous integration

## Project Structure

```text
RaceDay/
├── .github/
│   └── workflows/
│       ├── part1-ci.yml
│       └── part2-ci.yml
├── RaceDay.Api/
│   ├── Controllers/
│   ├── Data/
│   ├── Migrations/
│   ├── Models/
│   ├── Security/
│   ├── Program.cs
│   └── appsettings.json
├── RaceDay.Api.Tests/
│   ├── AuthControllerTests.cs
│   ├── RaceDayWebApplicationFactory.cs
│   └── UnitTest1.cs
├── RaceDay.Mvc/
│   ├── Controllers/
│   ├── Services/
│   ├── Views/
│   └── Program.cs
├── RaceDay.Mvc.Tests/
├── RaceDay.Contracts/
│   └── Dtos.cs
├── docs/
├── RaceDay.slnx
└── README.md
```

## Prerequisites

Install the following before running the application:

- .NET 8 SDK
- Visual Studio 2022 with the ASP.NET and web development workload, or a compatible .NET development environment
- SQL Server LocalDB on Windows for the configured local database
- Git

## Getting Started

Clone the repository and check out the Part 3 development branch:

```bash
git clone https://github.com/Derrick121K/RaceDay.git
cd RaceDay
git checkout part3-mvc-development
```

Open `RaceDay.slnx` in Visual Studio.

### Database Configuration

The API uses the `DefaultConnection` connection string in `RaceDay.Api/appsettings.json`.

The local development configuration uses SQL Server LocalDB and the `RaceDayPart2` database. Keep this separate from the Part 1 database.

Before running the API for the first time, apply the Entity Framework Core migration.

In Visual Studio's Package Manager Console, select `RaceDay.Api` as the Default project and run:

```powershell
Update-Database
```

Alternatively, after installing the compatible EF Core command-line tool, run:

```bash
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

### Run the API

Start `RaceDay.Api` using Visual Studio or run:

```bash
dotnet run --project RaceDay.Api/RaceDay.Api.csproj
```

Open the Swagger UI at the HTTPS URL shown in the application output, followed by `/swagger`.

For example, during local development:

`https://localhost:7155/swagger/index.html`

Your port may differ depending on the launch profile.

### Run the MVC application

Apply the API database migration before starting the applications. Then run
the API and MVC app in separate terminals from the repository root.

Terminal 1 — API:

```powershell
dotnet run --project RaceDay.Api/RaceDay.Api.csproj --launch-profile https
```

Terminal 2 — MVC:

```powershell
$env:Api__BaseUrl = "https://localhost:7155/"
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj --launch-profile https
```

Open `https://localhost:7180`. The API base URL defaults to
`https://localhost:7155/` in `RaceDay.Mvc/appsettings.json`; use `Api__BaseUrl`
when the API runs elsewhere. If the local HTTPS development certificate is not
trusted, run `dotnet dev-certs https --trust` once for the current user.

After signing in as a Participant, use the participant dashboard to browse and
search events, view event categories, enrol, and review personal enrolments and
results. The MVC requests the authenticated participant's enrolments and results
from the API; it does not connect to SQL Server. The API validates event/category
membership and duplicate enrolments and enforces participant ownership.

The MVC app forwards the API's HttpOnly session cookie per browser request. It
does not maintain a shared cookie container or create a separate MVC login
session. See [docs/PART3_API_INTEGRATION_MAP.md](docs/PART3_API_INTEGRATION_MAP.md)
for the route and cookie contract.

On Windows Development runs, the API and MVC persist separate Data Protection
key rings under `%LOCALAPPDATA%\RaceDay\Development\DataProtection\Api` and
`%LOCALAPPDATA%\RaceDay\Development\DataProtection\Mvc`. Each key ring is
encrypted with DPAPI for the current Windows user. Run each app as that same
Windows user across restarts. The existing default ASP.NET Core key repository
is left untouched. Development logging uses console and debug providers;
Production logging and key configuration are unchanged.

## Authentication and Session Management

RaceDay uses server-side session state.

- Successful login stores the user's `UserId` and `Role` in the session.
- Passwords are hashed using ASP.NET Core's password hasher.
- Passwords and password hashes are not returned in authentication responses.
- Logout clears the session.
- Protected actions use the session authorization attribute to check authentication and, where required, the user's role.

The local development application uses an in-memory distributed cache for session storage. This is suitable for development and is not a persistent, shared session store for a multi-instance production deployment.

## User Roles

### Organiser

Organisers can create events, manage events they own, manage their event categories, view enrolments for their events, update enrolment statuses and record or maintain results for their events.

### Participant

Participants can register, log in, browse events, enrol in events, view their own enrolments and view their own results.

The API checks the session identity and role for protected operations. Organiser event-management actions also check ownership of the relevant event.

### Create a Development Organiser

Public registration creates participant accounts only. To create a local
organiser for development or a demo, apply the database migrations, then run
the API interactively in the Development environment:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project RaceDay.Api/RaceDay.Api.csproj -- --provision-organiser
```

The command prompts for the organiser's email and name, then reads the password
without echoing it and asks for confirmation. It hashes the password with the
ASP.NET Core password hasher before saving the account. It refuses duplicate
emails and is unavailable outside the Development environment. Do not put the
password in command arguments, source control, or a committed settings file.

## API Endpoint Reference

All request and response schemas can also be inspected in Swagger while the API is running.

### Authentication

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/register` | Register an account |
| POST | `/api/auth/login` | Authenticate an account and start a session |
| POST | `/api/auth/logout` | Clear the current session |
| GET | `/api/auth/me` | Return the current session user's details |

### Events

| Method | Route | Purpose | Access |
|---|---|---|---|
| GET | `/api/events` | List events | Public |
| GET | `/api/events/{id}` | View an event | Public |
| GET | `/api/event-types` | List event types for organiser event forms | Public |
| POST | `/api/events` | Create an event | Organiser |
| PUT | `/api/events/{id}` | Update an owned event | Organiser / owner |
| DELETE | `/api/events/{id}` | Delete an owned event | Organiser / owner |

### Categories

| Method | Route | Purpose | Access |
|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | List categories for an event | Public |
| GET | `/api/categories/{id}` | View a category | Public |
| POST | `/api/events/{eventId}/categories` | Create a category | Organiser / owner |
| PUT | `/api/categories/{id}` | Update a category | Organiser / owner |
| DELETE | `/api/categories/{id}` | Delete a category | Organiser / owner |

### Enrolments

| Method | Route | Purpose | Access |
|---|---|---|---|
| POST | `/api/enrolments` | Enrol in an event and select a category | Participant |
| GET | `/api/enrolments/me` | List the current participant's enrolments | Participant |
| GET | `/api/enrolments/{id}` | View an enrolment owned by the participant or organiser of its event | Participant / organiser with ownership |
| GET | `/api/events/{eventId}/enrolments` | List enrolments for an owned event | Organiser / owner |
| PUT | `/api/enrolments/{id}/status` | Update an enrolment status for an owned event | Organiser / owner |

The API checks that a selected category belongs to the chosen event and prevents a participant from enrolling in the same event more than once.

### Results

| Method | Route | Purpose | Access |
|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | Record a finishing time and position | Organiser / owner |
| GET | `/api/results/me` | View the current participant's results | Participant |
| GET | `/api/enrolments/{enrolmentId}/result` | View a result when authorised | Participant / enrolment owner or Organiser / event owner |
| PUT | `/api/enrolments/{enrolmentId}/result` | Update a result for an owned event | Organiser / owner |
| DELETE | `/api/enrolments/{enrolmentId}/result` | Delete a result for an owned event | Organiser / owner |

## Image Storage and Blob Uploads

RaceDay supports event banner and participant profile image uploads.
The MVC application sends uploads to the API. The API validates JPEG,
PNG and WebP images (maximum 5 MB) and stores them through
IBlobStorageService using Azure.Storage.Blobs. The database stores
relative Blob paths. MVC retrieves images through API endpoints and
does not connect directly to Blob Storage.

API routes:
- POST /api/events/{id}/banner
- GET /api/events/{id}/banner-image
- POST /api/users/me/profile-image
- GET /api/users/me/profile-image

### Local Blob Storage

The local integration was tested with Azurite, an Azure Storage emulator.
This verifies local Blob integration; it is not evidence of a live Azure
Storage account deployment.

Start Azurite:
docker run -d --name raceday-azurite --restart unless-stopped `
  -p 127.0.0.1:10000:10000 -v raceday-azurite-data:/data `
  mcr.microsoft.com/azure-storage/azurite:3.35.0 `
  azurite --blobHost 0.0.0.0 --location /data --skipApiVersionCheck

## Docker Build and Run

Build from the repository root:
docker build -f .\RaceDay.Mvc\Dockerfile -t raceday-mvc:part3-verified .

Run the MVC container. The API must be running on the host at port 5000:
docker run -d --name raceday-mvc-part3-verified `
  -p 127.0.0.1:8082:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e Api__BaseUrl=http://host.docker.internal:5000/ `
  -v raceday-mvc-dpkeys-verified:/home/app/.aspnet/DataProtection-Keys `
  raceday-mvc:part3-verified

Open http://localhost:8082. Use localhost rather than 127.0.0.1
in the browser because of the application's allowed-host configuration.

## Running Automated Tests

The test project uses xUnit, ASP.NET Core's test host and Entity Framework Core's in-memory provider.

The test factory replaces the normal SQL Server registration with a separate in-memory database for API tests.

Run restore, Release build, the API tests and the MVC tests from the repository root:

```bash
dotnet restore RaceDay.slnx
dotnet build RaceDay.slnx --configuration Release
dotnet test RaceDay.Api.Tests/RaceDay.Api.Tests.csproj --configuration Release
dotnet test RaceDay.Mvc.Tests/RaceDay.Mvc.Tests.csproj --configuration Release
```

The automated tests currently cover:

- Registration request validation
- Invalid registration returning `400 Bad Request`
- Successful registration returning `201 Created`
- Duplicate email registration returning `409 Conflict`
- Successful login returning user details
- Rejection of an incorrect password with `401 Unauthorized`
- Logout returning `204 No Content`
- Profile retrieval and updates, including role modification resistance
- Participant result fields and ownership isolation
- Participant and organiser ownership for enrolment detail
- MVC authentication, antiforgery, API cookie forwarding/clearing and per-user session isolation
- MVC login errors, API failures, expired sessions, forbidden access and role-aware navigation
- Organiser access restrictions, event listing/create/edit, category creation,
  enrolment status management and result recording/validation

The MVC organiser workflow is exercised through a fake HTTP API. API controller
authorization remains covered by the API test project; live database behavior
should also be checked with a local Development database before a demonstration.

### Live organiser workflow check

With the API and MVC running as described above, sign in using a Development
organiser provisioned with the README procedure. Open `/organiser`, create an
event, then use its detail page to add categories, review enrolments and record
results. A participant session should be denied at `/organiser/events`. The MVC
uses the API routes and preserves the API's session cookie; it never connects
to SQL Server itself.

## Continuous Integration

Part 3 workflow: .github/workflows/part3-ci.yml

The workflow runs on pushes and pull requests targeting main and
part3-mvc-development. It restores, builds and tests RaceDay.slnx.

The Part 2 workflow remains available at .github/workflows/part2-ci.yml.
Add the successful Part 3 GitHub Actions run URL after pushing.

## Part 1 Documentation

The original database design, SQL script, ERD and API endpoint plan remain in the `docs/` directory. They provide the design reference for the Part 2 implementation.

## Video Demonstration

**Part 2 demonstration:** [Video link](https://youtu.be/Ky8FH73jlck).

The Part 2 presentation is available here:

[Video link](https://youtu.be/Ky8FH73jlck)

## Repository and Submission

Repository:

https://github.com/Derrick121K/RaceDay

Part 2 development branch:

https://github.com/Derrick121K/RaceDay/tree/part2-api-development

[Video link](https://youtu.be/Ky8FH73jlck)

## Part 3 Video Demonstration

Add the unlisted YouTube Part 3 demonstration URL here after recording.
The demonstration should show the organiser and participant workflows,
image upload, Docker deployment, repository and successful CI run.

## Part 3 Repository

https://github.com/Derrick121K/RaceDay/tree/part3-mvc-development