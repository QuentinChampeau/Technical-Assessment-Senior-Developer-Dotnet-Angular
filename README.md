# Technical Assessment—Senior Developer / Staff Engineer

## Overview

This technical assessment evaluates your ability
to design and implement a production-ready expense management system using modern
technologies. You will be building a RESTful backend service with proper architecture,
data persistence, and production-ready features, along with a responsive frontend
application that provides an intuitive user interface for expense management.

**Technology Stack**: .NET 9.0, ASP.NET Core, Redis, C#, Angular

**Your name:** Quentin CHAMPEAU

## Getting Started

### Backend

1. Run the `AppHost` project to start the backend with its dependencies
2. The application will automatically configure Redis and other required services
3. Use the provided `WebApi.http` file to test the endpoints

### Frontend

1. Navigate to the frontend directory
2. Run `npm install` to install dependencies
3. Run `ng serve` to start the development server
4. Access the application at `http://localhost:4200`

### API Documentation

The backend exposes both the raw OpenAPI specification and an interactive API UI:

- OpenAPI document: `/openapi/v1.json`
- Scalar UI: `/scalar`

Scalar provides a clean interactive interface to explore and manually test all available endpoints.

## Requirements

### Core Technologies

#### Backend

- **.NET 10.0** with ASP.NET Core
- **Redis** for cache access
- **PostgreSQL** and **Entity FrameworkCore** for data persistence
- **C# 14.0** features where applicable
- **xUnit, Moq, FluentAssertions** testing
- **OpenAPI** and **Scalar UI** for interactive API documentation

#### Frontend

- **Angular** (latest version)
- **TypeScript**
- **RxJS** for reactive programming
- **Tailwindcss**
- **Chart.js** for charts handling
- **Jasmine/Karma** for testing

### API Endpoints to Implement

You must implement the following REST endpoints:

- `POST /expenses` - Create a new expense
- `GET /expenses` - Retrieve all expenses with pagination support
- `GET /expenses/{id}` - Retrieve a specific expense by ID

### Frontend Features to Implement

You must implement a responsive Angular application with the following features:

- **Expense Management Dashboard**
  - Overview of expenses with summary statistics
  - Filterable and sortable expense list with pagination
  - Visualizations (charts/graphs) of expense data

- **Expense Operations**
  - Form to create new expenses
  - View detailed information for a specific expense
  - Search and filter functionality for expenses

### Technical feature to Implement

you must implement an Audit for Backend Entities

- Track changes for all entity properties to ensure a detailed change log.
- Provide functionality to view the history of an entity, including its associated child entities (if applicable).

### Technical Requirements

#### Data Model

Create an appropriate expense model with the following properties:

- Unique identifier
- Description
- Amount (decimal)
- Category
- Date
- Created/Updated timestamps

#### Production-Ready Features

Your implementation must include:

> **NOTE:** This is some example, implement what you are judging the most important

##### Backend Features

1. **Input Validation**
   - Validate all incoming requests
   - Return appropriate HTTP status codes
   - Provide meaningful error messages

2. **Error Handling**
   - Global exception handling
   - Structured error responses
   - Logging of errors and important events

3. **Data Persistence**
   - Use PostgreSQL and Entity FrameworkCore as the primary data store
   - Use Redis as primary cache
   - Implement proper serialization/deserialization
   - Handle connection failures gracefully

4. **API Documentation**
   - OpenAPI/Swagger integration
   - Well-documented endpoints with examples

5. **Testing**
   - Unit tests for core business logic
   - Integration tests for API endpoints
   - Test coverage for edge cases

6. **Architecture**
   - Clean separation of concerns
   - Dependency injection
   - Repository pattern or similar data access abstraction

##### Frontend Features

1. **Responsive Design**
   - Mobile-first approach
   - Adaptive layouts for different screen sizes
   - Consistent user experience across devices

2. **State Management**
   - Proper use of services and state management patterns
   - Efficient data flow between components
   - Reactive programming with RxJS

3. **User Experience**
   - Intuitive interface with clear navigation
   - Loading states and error feedback
   - Form validation with meaningful error messages

4. **Performance Optimization**
   - Lazy loading of modules
   - Efficient change detection
   - Optimized asset loading

5. **Testing**
   - Unit tests for components and services
   - End-to-end tests for critical user flows
   - Test coverage for edge cases

6. **Architecture**
   - Feature modules organization
   - Smart and presentational component separation
   - Reusable component library

## Evaluation Criteria

### Backend

- [x] All three endpoints are functional and tested
- [x] Proper input validation and error handling
- [x] Redis integration working correctly
- [x] Clean, readable, and maintainable code
- [x] Appropriate use of HTTP status codes
- [x] Basic logging implementation

### Frontend

- [x] Responsive UI that works on mobile and desktop
- [x] Implementation of all required features (dashboard, expense creation, viewing)
- [x] Proper integration with backend API endpoints
- [x] Clean, readable, and maintainable code
- [x] Proper form validation and error handling
- [x] Consistent styling and user experience

## Submission Guidelines

### Backend

1. **Code Quality**: Ensure your code follows C# coding standards and best practices
2. **Documentation**: Include README updates with setup instructions and architectural decisions
3. **Testing**: Provide evidence that all endpoints work correctly (via the provided WebApi.http file)
4. **Comments**: Add meaningful comments explaining complex business logic or architectural choices

### Frontend

1. **Code Quality**: Ensure your code follows Angular/TypeScript coding standards and best practices
2. **Documentation**: Include setup instructions and component documentation
3. **Testing**: Provide evidence that all features work correctly
4. **Comments**: Add meaningful comments explaining complex UI logic or architectural choices

## Technical Constraints

### Backend

- All endpoints must be RESTful
- Follow standard HTTP conventions
- Implement proper async/await patterns
- Use built-in .NET dependency injection

### Frontend

- Use Angular (latest version) with TypeScript
- Implement responsive design that works on mobile and desktop
- Follow Angular best practices for project structure and component design
- Use reactive programming patterns with RxJS
- Ensure proper error handling and loading states
- Implement proper form validation

## FAQ

- The project didn't run in Debug when I run Aspire.Net.AppHost, is it normal?
  No, this is working. We have tested it. Make sure you have .NET 10.0 SDK installed on your machine and Docker is running.

## Questions?

If you have any questions about the requirements or need clarification on any aspect of the assessment, please don't
hesitate to ask.

## How to submit

Create a private repository on GitHub and invite the following team members: - n2jsoft-hr-cr

We will review your submission and provide feedback.

Good luck! 🚀

<img src="https://media3.giphy.com/media/v1.Y2lkPTc5MGI3NjExaDRuZ3BmNXJjcGh5OTh5dmJ4YzFxbnJlZjlqaWg3ZXRlcHlicDZtZiZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/l1ugmrXA6gLlIbNE4/giphy.gif">

## Architectural Decisions

### Backend

**Caching strategy**: Redis is used as a read-through cache with a 5-minute TTL (time to live).
All list cache keys are registered in a Redis SET so any write operation (create, update, delete) can invalidate the full
list in a single call, avoiding stale pagination results.

**Audit trail**: Changes are tracked manually at the service layer rather than via EF Core interceptors. This keeps the
audit shape explicit and testable without adding a dependency on EF Core's internal change-tracking pipeline.
Each write operation records a `ChangesJson` payload — full snapshot on create/delete, old/new diff on update.

**Validation**: Data annotations handle shape validation at the HTTP boundary (model binding).
A second pass in `ExpenseService.ValidateRequest()` enforces business rules (e.g. description trimming before length
check), keeping controllers thin.

### Frontend

**Route-level lazy loading**: Each route uses `loadComponent` to defer loading component bundles until the route is
activated, reducing the initial bundle size.

**E2E testing**: Playwright tests use `page.route()` interception to mock the backend API. This keeps the tests
deterministic and runnable without a live backend, while still exercising the full Angular rendering and navigation stack.
