# Event Planning & Coordination Platform

A 4-person, 2-month full-stack project: REST API backend, React web, Flutter mobile, and AI planning service.

## Quick Start

See [CONTRIBUTING.md](CONTRIBUTING.md) for full local development setup.

TL;DR after cloning:
```bash
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
flutter run -d chrome  # or your device

# AI (new terminal)
cd agentic-ai
python -m venv venv
source venv/bin/activate  # or venv\Scripts\activate on Windows
pip install -r requirements.txt
python -m uvicorn src.main:app --reload