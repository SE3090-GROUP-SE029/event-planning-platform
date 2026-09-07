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
dotnet ef database update
dotnet run

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
5. Run tests before every commit

```
#Backend
dotnet test

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