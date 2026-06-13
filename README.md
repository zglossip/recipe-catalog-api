# Recipe Catalog API 
Version 0.1.0

A REST API that will be used to service an upcoming recipe catalog/food diary application

## Frontend

This API serves as the backend to the [Recipe Catalog mobile application](https://github.com/zglossip/food-history-app)

To run the full application, you will need to clone and run both the backend and the frontend

In a future release, there may also be a webapp frontend

### Note about the Dockerfiles

There is a couple Dockerfiles included with this repository. This is primarily used by a Docker Compose config in the frontend repo.

## Instructions

### Live Development

* Ensure you have the [.NET 8.0 SDK](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian?tabs=dotnet8) installed
* In the terminal, run `dotnet watch run --urls=http://+:8080/`
* Navigate to `http://localhost:8080/swagger/index.html` to browse the API

### Database connection

In order to run this locally, you will need a database with a schema set up by the provided SQL queries in `/SQLTableDefinitions`. Set the connection string under `ConnectionStrings:DefaultConnection` in `appsettings.Development.json`:

```JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=recipe_catalog;Username=postgres;Password=yourpassword"
  }
}
```

| Property | Description |
| --- | --- |
| `Host` | The host of the data source |
| `Port` | The port of the data source (optional) |
| `Database` | The name of the database containing the schema |
| `Username` | The username to access the database |
| `Password` | The password to access the database |

Eventually, a database with this setup may be publically avaliable, but I don't currently have the resources to host it.

## API

All endpoints are in application/json. The model objects are listed below the endpoints.

### Endpoints

#### GET /recipe

Fetches a list of recipes based on optional query params. Default returns all recipes.

- **Query Parameters:**
  - `course` (string): fetches only recipes that have this course. Can have multiple courses
  - `cuisine` (string): fetches only recipes that have this cuisine. Can have multiple cuisines
  - `tag` (string): fetches only recipes that have this tag. Can have multiple tags
  - `name` (string): fetches only recipes whose name contains this value (case-insensitive)
  - `sort` (string): property to sort the recipes on (options are `id` and `name`. Default `id`)
  - `reverse` (boolean): sets whether the sorting should be reversed. Default false
- **Response:**
  - \[Recipe\]

#### GET /recipe/{id}

Fetches a recipe based on its unique ID

- **Path Parameters:**
  - `id` (number): the recipe ID
- **Response:**
  - Recipe

#### GET /recipe/{id}/ingredients

Fetches a list of ingredients for a recipe

- **Path Parameters:**
  - `id` (number): the recipe ID
- **Response:**
  - IngredientList

#### GET /recipe/{id}/instructions

Fetches a list of instructions for a recipe

- **Path Parameters:**
  - `id` (number): the recipe ID
- **Response:**
  - InstructionList

#### POST /recipe

Creates a new recipe, including its ingredients and instructions. Returns the generated ID.

- **Request:**
  - FullRecipeRequest
- **Response:**
  - `{ "id": 0 }`

#### PUT /recipe/{id}

Saves an existing recipe's core fields. Does not modify ingredients or instructions.

- **Path Parameters:**
  - `id` (number): the recipe ID
- **Request:**
  - RecipeRequest

#### PUT /recipe/{id}/ingredients

Saves a recipe's ingredients

- **Path Parameters:**
  - `id` (number): the recipe ID
- **Request:**
  - IngredientList

#### PUT /recipe/{id}/instructions

Saves a recipe's instructions

- **Path Parameters:**
  - `id` (number): the recipe ID
- **Request:**
  - InstructionList

#### DELETE /recipe/{id}

Deletes a recipe

- **Path Parameters:**
  - `id` (number): the recipe ID

## Models

**Recipe** (response)

```JSON
{
  "id": 0,
  "name": "string",
  "courseTypes": ["string"],
  "cuisineTypes": ["string"],
  "tags": ["string"],
  "servingAmount": 0,
  "servingName": "string",
  "source": "string",
  "uploaded": "2026-01-01T00:00:00Z"
}
```

| Property | Description |
| --- | --- |
| `id` | The unique ID for the recipe |
| `name` | The name of the recipe |
| `courseTypes` | A list of strings representing the different courses the recipe can apply to (i.e. main, side, breakfast, snack) |
| `cuisineTypes` | A list of strings representing the different styles of cuisine the recipe can apply to (i.e. Italian, American, Indian) |
| `tags` | A list of string representing a list of miscellanious tags saved to the recipe |
| `servingAmount` | The number of servings the recipe makes |
| `servingName` | The unit of measurement for a serving of the recipe (i.e. serving, slice, sandwich) |
| `source` | A link to the external recipe site, if there is one (nullable) |
| `uploaded` | The timestamp the recipe was created |

**RecipeRequest** (body for `PUT /recipe/{id}`)

```JSON
{
  "name": "string",
  "courseTypes": ["string"],
  "cuisineTypes": ["string"],
  "tags": ["string"],
  "servingAmount": 0,
  "servingName": "string",
  "source": "string"
}
```

**FullRecipeRequest** (body for `POST /recipe`)

A `RecipeRequest` plus `ingredients` (a list of Ingredient objects, see IngredientList below) and `instructions` (a list of strings).

**IngredientList**

```JSON
{
  "recipeId": 0,
  "ingredients": [
    {
      "name": "string",
      "quantity": 0.0,
      "uom": "string",
      "notes": "string"
    }
  ]
}
```

| Property | Description |
| --- | --- |
| `recipeId` | The unique ID of the recipe the ingredient list belongs to |
| `ingredients` | The list of ingredients for the recipe (NOTE: See the properties below for the ingredient object) |
| --- | --- |
| `name` | The name of the ingredient |
| `quantity` | The number of units for the ingredient |
| `uom` | The name of the unit of measurement (UOM) for the ingredient (i.e. c, tbs, ml) |
| `notes` | Any notes applied to the ingredient |

**InstructionList**

```JSON
{
  "recipeId": 0,
  "instructions": ["string"]
}
```

| Property | Description |
| --- | --- |
| `recipeId` | The unique ID of the recipe the instruction list belongs to |
| `instructions` | The ordered list of instructions for the recipe |

## Release History

### 0.1.0
This is the initial release of the application. This contains a basic CRUD api for recipes. Meant as a starting point for the application
