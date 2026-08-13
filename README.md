# UniSchedule Builder

A university scheduling web app: browse the weekly timetable, build your personal schedule as a student, post class exceptions (cancel / move / note) as a teacher, and manage everything from an admin dashboard.

## Tech stack

- ASP.NET Core MVC - web application framework
- C# - for backend logic
- Entity Framework Core - Object-Relational Mapper (ORM) for database interaction
- ASP.NET Core Identity - authentication and role management
- HTML, CSS - frontend technologies for structure and styling
- Bootstrap - frontend framework for responsive design
- JavaScript - for dynamic elements like timetable search, day pills and sortable tables
- SQLite - for database storage

## Features

- **Full timetable** — weekly Mon–Fri grid for all study groups with subject, teacher, room and time; filter by group, subject and teacher
- **Personal schedule** — students add/remove classes and see room occupancy (`N/M`) per class
- **Teacher exceptions** — teachers post cancel / relocate / note exceptions for their own classes, with date and time
- **Admin** — CRUD for subjects, rooms and timetable terms, plus a dashboard with stats, room usage and active exceptions
- **Study-group filtering** — 33 groups with term–group links

## User roles

- **Admin** — manages subjects, rooms and timetable terms; sees the dashboard with stats, room usage and active exceptions
- **Teacher** — posts exceptions (cancel / move / note) for their own classes
- **Student** — browses the timetable, builds a personal schedule and sees room occupancy

## Seeded accounts (password `Password123!`)

| Role    | Email                                          |
|---------|------------------------------------------------|
| Admin   | `admin@unischedule.com`                         |
| Student | `student@unischedule.com`, `student2@unischedule.com` |
| Teacher | `teacher_<id>@finki.ukim.mk` where `<id>` is a teacher id from the JSON |

Upon login users are redirected by role: Admin → `/AdminSubjects`, Teacher → `/TeacherExceptions`, Student → `/Schedule`.

## Project layout

```
uni-schedule-builder/
├── Controllers/        # Schedule, MySchedule, TeacherExceptions, AdminSubjects, AdminRooms, AdminDashboard, Account
├── Data/               # DbContext + idempotent seeder (DbSeeder.cs)
├── Migrations/         # EF Core migrations
├── Models/             # domain models + view models
├── Views/              # Razor views + shared _Timetable partial
└── wwwroot/            # site.css (dark dashboard theme), site.js
```