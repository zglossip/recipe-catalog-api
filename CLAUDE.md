# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

REST API backend for a recipe catalog / food diary app. ASP.NET Core (.NET 8) on top of
PostgreSQL via raw Npgsql — no EF/ORM. Frontend lives in a separate repo
([food-history-app](https://github.com/zglossip/food-history-app)); this API is consumed by it.

## Commands

```bash
dotnet build                              # build
dotnet watch run --urls=http://+:8080/    # live-reload dev server
dotnet run --urls=http://+:8080/          # run without reload
```

Swagger UI (Development env only): `http://localhost:8080/swagger/index.html`.

There is no test project — no `dotnet test` target exists yet.

## Configuration

The DB connection comes from `ConnectionStrings:DefaultConnection` in `appsettings*.json`
(read in `DatabaseConnectionSupplier`). `appsettings.Development.json` holds the local
connection string. Note: the README's mention of a `connectionsettings.json` file is stale —
the code does not read it.

Schema is defined by the DDL scripts in `SQLTableDefinitions/`; all tables live in the
`recipe_catalog` Postgres schema. There are no migrations — apply these by hand to a fresh DB.

## Architecture

Three layers, each interface-backed and wired by DI in `Program.cs`
(services/DAOs are `Scoped`, the connection supplier is `Singleton`):

```
Controllers/  →  Services/  →  DAOs/  →  PostgreSQL
```

- **Controllers** — `RecipeController` is the single controller and owns every route under
  `/recipe`. Controllers do HTTP concerns only (status codes, existence checks before
  update/delete) and delegate to services.
- **Services** — orchestration. `RecipeService` is the key one: a full recipe is *composed*
  from multiple tables, so it fans out to `ICourseDao`/`ICuisineDao`/`ITagDao` per recipe to
  populate `CourseTypes`/`CuisineTypes`/`Tags`. This is an N+1 pattern on list endpoints — be
  aware when touching `GetAsync`.
- **DAOs** — raw SQL. Each DAO builds SQL strings and `NpgsqlParameter` lists, then calls the
  static helpers in `DAOs/Util/DaoUtil.cs` (`QueryAsync`, `QueryForListAsync`, `ExecuteAsync`,
  `CreateAsync`). Always parameterize via `NpgsqlParameter` — never string-concatenate user
  input into SQL.

### Data model

A recipe is split across tables: the `recipe` row plus child tables `course`, `cuisine`, `tag`
(each a `text` row keyed by `recipe_id`) and `ingredient`/`instruction` (ordered by a
`position` column). The DAO/service code reflects this split rather than a single join.

### Write paths (important, easy to confuse)

- **POST `/recipe`** → `RecipeService.CreateFullAsync` → `RecipeDao.CreateFullAsync`, which
  inserts the recipe and *all* children inside a single explicit `NpgsqlTransaction`
  (commit/rollback). This is the transactional, all-in-one create path.
- **`RecipeService.CreateAsync`** is the older non-transactional path (separate per-DAO inserts,
  no transaction). It is not currently called by the controller — prefer the `Full` path.
- **PUT `/recipe/{id}`** → `RecipeService.UpdateAsync` updates the recipe row, then for each of
  course/cuisine/tag does a **delete-all-then-recreate**. These calls are *not* wrapped in a
  transaction, so a mid-update failure can leave partial state.

### Supporting patterns

- **Mappers** (`DAOs/Mappers/`) — each wraps a `Func<NpgsqlDataReader, T>` via `AbstractMapper`
  and is passed into `DaoUtil` query helpers to turn rows into models. Handle DB nulls
  explicitly with `reader.IsDBNull(...)` (see `RecipeMapper`).
- **`QueryParamList<T>`** (`DAOs/Util/`) — generates numbered bind variables (`@course0`,
  `@course1`, …) for SQL `IN (...)` clauses from a list, keeping multi-value filters
  parameterized. Used by the filtered `RecipeDao.GetAsync`.
- **Models** — `Recipe` is the response shape (mutable class with a `Clone()` used so the
  populated copy doesn't mutate the DAO result). Inbound bodies use separate request models:
  `FullRecipeRequest` (POST, includes ingredients + instructions) and `RecipeRequest` (PUT).
- **Errors** — unhandled exceptions are caught by `GlobalExceptionHandler`
  (`Config/`, registered via `AddExceptionHandler`) and returned as RFC9110 `ProblemDetails`
  500s.

## Known TODOs in code

- CORS is hardcoded to `http://localhost:8081` in `Program.cs` and must be made
  environment-specific before any deploy (flagged in-code).
- HTTPS redirection is commented out.
