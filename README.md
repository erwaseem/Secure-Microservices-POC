# 🔐 Secure Microservices Architecture – .NET 8 POC

A hands-on **Microservices Architecture POC** built using ASP.NET Core 8, YARP API Gateway, JWT Authentication, RabbitMQ, MassTransit, Entity Framework Core, and SQL Server.

This project demonstrates how to build a secure, loosely coupled, event-driven e-commerce application using microservices architecture. It is designed for learning, practical implementation, and senior .NET developer interview preparation.

## 📌 Project Overview

The application follows a microservices architecture in which each service manages its own responsibilities and database. All HTTP requests from the UI are routed through a centralized API Gateway, which validates JWT tokens and forwards requests to the appropriate downstream service.

The application includes product catalogue management, shopping cart operations, user authentication, order placement, and asynchronous notification processing.

## 🏗️ Architecture

```text
                     Browser
                        |
                        v
               ASP.NET Core MVC UI
                   Port: 5168
                        |
                        v
                 YARP API Gateway
                   Port: 5020
                JWT Validation
                        |
          +-------------+-------------+
          |             |             |
          v             v             v
      Auth-Service  ProductService  Cart_Services
       Port: 5001    Port: 5138     Port: 5164
          |             |             |
          v             v             v
     SecureAuthDb  SecureProductDb  CartServiceDb
          |             |             |
          +-------------+-------------+
                        |
                        v
                  RabbitMQ Broker
                   + MassTransit
                        |
                        v
               NotificationService
                  .NET Worker
```

**Key architectural principle:** The UI never calls a microservice directly. All HTTP requests go through the YARP API Gateway.

## 🛠️ Technology Stack

| Technology | Purpose |
|---|---|
| ASP.NET Core 8 | Backend APIs and web application |
| ASP.NET Core MVC | User interface |
| YARP | API Gateway and reverse proxy |
| JWT | Authentication and token validation |
| RabbitMQ | Message broker |
| MassTransit | Asynchronous messaging and event handling |
| Entity Framework Core 8 | ORM and database operations |
| Microsoft SQL Server | Relational database |
| C# | Application development |
| Dependency Injection | Service registration and dependency management |
| REST APIs | Communication between UI, gateway, and services |

## 📂 Microservices and Responsibilities

### 1. Auth-Service
**Port:** `5001`

Responsible for user registration and login.

Features:
- User registration and duplicate username validation.
- User authentication.
- JWT token generation using HS256.
- Role and user claims.
- Publishes `UserRegisteredEvent` after registration.

Database: `SecureAuthDb`

### 2. ProductService
**Port:** `5138`

Responsible for product catalogue management.

Features:
- Retrieve all products.
- Search and filter products by category.
- Retrieve product details by GUID.
- Create, update, and delete products.
- Seed demo products.
- Publish `ProductChangedEvent` when products are created or deleted.

Database: `SecureProductDb`

### 3. Cart_Services
**Port:** `5164`

Responsible for shopping cart operations and order placement.

Features:
- Create or retrieve a cart using the browser session ID.
- Add products to the cart.
- Increase or decrease product quantities.
- Remove individual cart items.
- Calculate subtotal, discount, and grand total.
- Place an order by clearing the cart.
- Publish `OrderPlacedEvent`.

Database: `CartServiceDb`

### 4. API Gateway
**Port:** `5020`

Built using YARP (Yet Another Reverse Proxy).

Responsibilities:
- Provide a single entry point for HTTP requests.
- Route requests to the appropriate microservice.
- Validate JWT tokens.
- Protect product write operations.
- Centralize gateway-level middleware and error handling.

Configured routes:

| Route | Path | Downstream Port |
|---|---|---|
| Auth | `/auth/{**catch-all}` | 5001 |
| Products | `/api/Products/{**catch-all}` | 5138 |
| Cart | `/api/Cart/{**catch-all}` | 5164 |

### 5. NotificationService

A background .NET Worker that consumes asynchronous events from RabbitMQ through MassTransit.

Events consumed:
- `UserRegisteredEvent`
- `ProductChangedEvent`
- `OrderPlacedEvent`

The current POC logs notification confirmations. Email or SMS integration can be added later.

### 6. Shared Class Library

Contains shared C# event contracts used by publishers and consumers.

Examples:
- `UserRegisteredEvent`
- `ProductChangedEvent`
- `OrderPlacedEvent`

The library maintains a consistent event schema across services and contains no business logic.

## 🔒 Authentication and Security

The POC uses JWT-based authentication.

Authentication flow:

1. The user registers or logs in through the UI.
2. Auth-Service validates the credentials.
3. Auth-Service generates a JWT access token.
4. The UI stores the token in ASP.NET Core session.
5. The UI sends the Bearer token for protected write operations.
6. The API Gateway validates the token.
7. The gateway forwards the request to the appropriate service.

Security features:
- Centralized JWT validation.
- Role and user claims in the token.
- Protected product write operations.
- Public product read operations.
- Centralized gateway-level error handling.

**Important:** This is a learning POC. Password hashing, secret management, token lifecycle improvements, and additional production security controls are required before real-world deployment.

## 📨 Asynchronous Communication

The project uses RabbitMQ and MassTransit for event-driven communication.

| Event | Publisher | Consumer |
|---|---|---|
| `UserRegisteredEvent` | Auth-Service | NotificationService |
| `ProductChangedEvent` | ProductService | NotificationService |
| `OrderPlacedEvent` | Cart_Services | NotificationService |

### Benefits

- Loose coupling between services.
- Asynchronous processing.
- Independent service development.
- Reduced direct dependencies.
- Easier integration of additional consumers.

MassTransit configures consumer endpoints and queues using `ConfigureEndpoints(context)`.

## 🛒 End-to-End Application Flow

1. Open the shopping application.
2. Browse products loaded through the API Gateway.
3. Search or filter products.
4. Add products to the shopping cart.
5. View cart items and order totals.
6. Increase or decrease item quantities.
7. Remove items from the cart.
8. Place an order.
9. Cart_Services clears the cart and publishes `OrderPlacedEvent`.
10. NotificationService consumes the event and logs the confirmation.

The cart is associated with the ASP.NET Core session cookie, allowing each browser session to maintain an isolated cart without requiring login for basic cart operations.

## 🗄️ Database Design

The POC uses separate SQL Server databases for individual services.

| Database | Service |
|---|---|
| `SecureAuthDb` | Auth-Service |
| `SecureProductDb` | ProductService |
| `CartServiceDb` | Cart_Services |

Entity Framework Core migrations are used to manage database schema changes.

The cart contains `Carts` and `CartItems` tables. Cart items store product details, price information, and quantity for the shopping session.

## 🚀 Prerequisites

Install the following before running the project:

- .NET 8 SDK
- Microsoft SQL Server
- SQL Server Management Studio (SSMS), recommended
- RabbitMQ
- Git
- Visual Studio 2022 or Visual Studio Code

## ⚙️ Configuration

Before starting the services:

1. Configure SQL Server connection strings for each service.
2. Configure RabbitMQ connection settings.
3. Configure the JWT secret consistently between Auth-Service and API Gateway.
4. Configure the UI's API Gateway URL in `appsettings.json`.

Example gateway configuration:

```json
{
  "ApiGateway": {
    "BaseUrl": "http://localhost:5020"
  }
}
```

This is an example configuration fragment. Merge it into your existing `appsettings.json` rather than replacing the entire file.

Use environment variables, .NET User Secrets, or a secret manager for sensitive settings. Do not commit production credentials or JWT secrets to GitHub.

## ▶️ Running the Application

Start the components in the following order.

### Step 1: Start SQL Server

Ensure SQL Server is running and all connection strings are configured correctly.

### Step 2: Start RabbitMQ

Start your RabbitMQ instance and verify that it is accessible.

The reference configuration uses:

```text
localhost:5672
```

The `guest/guest` credentials are suitable only for local development where RabbitMQ's security restrictions permit them. Configure dedicated credentials for other environments.

### Step 3: Start ProductService

```bash
cd ProductService
dotnet run
```

Expected port: `5138`

### Step 4: Start Cart_Services

```bash
cd Cart_Services
dotnet run
```

Expected port: `5164`

### Step 5: Start Auth-Service

```bash
cd Auth-Service
dotnet run
```

Expected port: `5001`

### Step 6: Start API Gateway

```bash
cd API-Gateway
dotnet run
```

Expected port: `5020`

### Step 7: Start NotificationService

```bash
cd NotificationService
dotnet run
```

This is a background worker and does not expose an HTTP port.

### Step 8: Start the UI

```bash
cd UI
dotnet run
```

Expected port: `5168`

**Note:** Run each service from a separate terminal. The commands assume these project folder names match your repository. Adjust them if your actual solution structure differs.

## 🌱 Seed Demo Products

After starting ProductService and the API Gateway, execute:

```http
POST http://localhost:5020/api/Products/seed
```

This endpoint seeds demo products and skips existing SKUs, as described in the POC reference.

## 🧪 Testing Checklist

- [ ] User registration.
- [ ] User login and JWT generation.
- [ ] JWT validation at the API Gateway.
- [ ] Product listing and filtering.
- [ ] Product creation with authentication.
- [ ] Product update and deletion.
- [ ] Cart creation and item management.
- [ ] Cart quantity updates and total calculations.
- [ ] Order placement.
- [ ] RabbitMQ event publishing.
- [ ] NotificationService event consumption.
- [ ] Verification that UI requests pass through the API Gateway.

## 💡 Key Design Decisions

**Why YARP?**

Provides a configurable reverse proxy integrated with ASP.NET Core, making it suitable for centralized routing and gateway middleware.

**Why RabbitMQ with MassTransit?**

Separates event publishers from consumers and simplifies message handling, endpoint configuration, and retry strategies.

**Why separate databases?**

Supports service ownership of data and reduces direct database coupling between microservices.

**Why JWT?**

Provides a token-based approach to authentication between the UI, gateway, and protected endpoints.

**Why a Shared library?**

Maintains consistent event contracts between publishers and consumers.

## ⚠️ Current POC Limitations

The following improvements should be considered before production deployment:

- Replace plaintext password storage with BCrypt or Argon2 password hashing.
- Move JWT secrets into a secure secret manager and implement rotation.
- Introduce refresh-token handling or another suitable token renewal strategy.
- Add real email/SMS notification integration.
- Containerize services using Docker and Docker Compose.
- Consider Kubernetes for orchestration and independent scaling.
- Add health checks, circuit breakers, and appropriate retry policies using Polly.
- Configure distributed session storage, such as Redis, when running multiple UI instances.
- Add production-grade logging, monitoring, rate limiting, and automated integration tests.
- Review authorization and input validation across every service and endpoint.

## 🎯 Learning Objectives

This POC demonstrates practical concepts in:

- Microservices architecture.
- API Gateway and reverse proxy patterns.
- JWT authentication and claims.
- RESTful API design.
- Event-driven architecture.
- RabbitMQ and MassTransit.
- Entity Framework Core and SQL Server.
- Service-specific data ownership.
- Dependency injection.
- Production-readiness considerations.
- Senior .NET developer interview preparation.

## 📚 Intended Audience

- .NET developers learning microservices.
- Developers preparing for senior .NET interviews.
- Engineers exploring YARP, RabbitMQ, and MassTransit.
- Developers building a portfolio project demonstrating distributed application concepts.

## 📄 Project Status

**Proof of Concept (POC)** — intended for learning, experimentation, and interview preparation. It is not production-ready without the security, resilience, observability, and deployment improvements outlined above.

## 👨‍💻 Author

**Waseem Ahmad**

.NET Developer | ASP.NET Core | C# | Angular | SQL Server | Microservices

---

⭐ If you find this project useful, consider starring the repository and sharing feedback or suggestions for improvement.
