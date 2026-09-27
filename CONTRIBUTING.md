Daily Developer Workflow

1. A GitHub Issue is created using the team's issue template.

```
Assign labels
Set priority
Add milestone/sprint
Add acceptance criteria
```
2. Navigate to the repository and update dev & verify you're up to date.

```
cd event-planning-platform

git checkout dev
git pull origin dev

git status
git log --oneline -5
```
3. Create Feature Branch & verify

```
feature/issue-123-user-registration
git branch
```
4. Develop the Feature

```
# Backend
cd backend
dotnet restore
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api

# Web (new terminal)
cd web
npm install
npm run dev

# Mobile (new terminal)
cd mobile
flutter pub get
flutter run

# AI (new terminal)
cd agentic-ai
python -m venv venv
source venv/bin/activate  # or venv\Scripts\activate on Windows
pip install -r requirements.txt
python -m uvicorn src.main:app --reload
```

### Backend configuration

The API loads local settings from `backend/src/Api/appsettings.json` and
`appsettings.Development.json` through the standard ASP.NET Core configuration
providers. This includes the database connection, JWT, admin seed, SMTP, guest
AI, and Agentic AI sections; a local `.env` file is not read. The EF Core
design-time factory reads these JSON files when available, then applies
environment variables as overrides. Migrations can therefore run in CI using
`ConnectionStrings__DefaultConnection` without requiring a checked-in settings
file or application authentication configuration.

Keep developer-specific credentials out of committed settings. Use .NET User
Secrets for local secrets when needed, and inject production secrets through
the deployment platform's environment/configuration provider. Environment
variables may override JSON values in production. `ASPNETCORE_HTTPS_PORT` is
only a hosting-provided HTTPS port.

Backend integration tests require a PostgreSQL database. Set
`TEST_DATABASE_CONNECTION` to a dedicated test database connection string, or
use `ConnectionStrings__DefaultConnection` as a fallback. The test fixture
creates a separate schema for each test class so tests can safely share a
PostgreSQL service; these schemas remain in the configured test database.
GitHub Actions provides this database as a PostgreSQL service container.

5. Run tests before every commit

```
# Backend (set TEST_DATABASE_CONNECTION or ConnectionStrings__DefaultConnection)
dotnet test backend/backend.sln

#React web
npm test

#Flutter
flutter test

#Agentic-ai
pytest
```
6. Commit Using Conventional Commits

```
git status
git add .
git commit -m "feat(auth): add user registration endpoint"

```
7. Keep Feature Branch Updated Daily

```
#Update dev
git checkout dev
git pull origin dev

#Return to feature branch
git checkout feature/issue-123-user-registration

#Merge latest dev
git merge dev

#Resolve conflicts if necessary
git add .
git commit
```

8. Push Branch

```
git push -u origin feature/issue-123-user-registration
```

9. Create Pull Request

10. Code review (3 approvals)
IF checks fail → PR blocked, developer pushes fixes, CI re-runs automatically
IF changes requested → developer addresses, pushes, stale approvals dismissed, re-review needed