# BytePizza — Architecture Blueprint

- **Status:** Baseline arquitetural para implementação; parcialmente implementada
- **Last reviewed:** 2026-11-16
- **Source:** [`00-po-specification.md`](./00-po-specification.md)

> Esta blueprint é o alvo de arquitetura, não uma declaração de funcionalidades já entregues. O estado atual implementado é uma fundação de domínio (primitives, Value Objects e agregado Pizza), testes unitários e CI básico. O roteiro de implementação e a divisão de trabalho estão na seção 9.

## 1. Scope and architectural baseline

BytePizza starts as a modular monolith for multiple branches. Its backend is a versioned REST API, backed by SQL Server, with SignalR for operational updates. The MAUI clients are online-only and separate customer-facing journeys from staff/KDS workflows. A single deployment and database keep the initial operational and transactional model simple; modules own their domain rules and tables.

### Decisions carried from the product specification

- Support multiple branches from the first release. `BranchId` is mandatory on branch-owned operational data. Branch is not synonymous with a SaaS tenant.
- Use SQL Server as the production database provider.
- Calculate weighted flavor prices on the server and store price snapshots on the order.
- Reserve recipe ingredients when an order is confirmed; consume them when preparation begins, subject to the final stock policy.
- Keep payment gateway integration out of the MVP.
- Require a live API connection to create and operate orders; no offline order synchronization.
- Restrict every staff operation to authorized branches. Customers can only access their own orders.
- Use an outbox for events that must not be lost. SignalR is a delivery mechanism, not the source of truth.

### Runtime baseline

Target .NET 10 for backend, tests, and .NET MAUI clients, subject to validating supported platform workloads in CI. The repository's existing Domain and unit-test projects already target `net10.0`. Keep SDK pinning and workload validation in the foundation milestone; do not introduce a .NET 9 target.

## 2. Bounded Contexts and dependencies

Arrows represent compile-time/application calls or published integration events. Contexts do not read or write another context's tables directly. The initial deployment may share one SQL Server database, but schema/table ownership remains explicit.

```mermaid
flowchart LR
    CustomerApp[Customer MAUI App]
    StaffApp[Staff and KDS MAUI App]
    API[BytePizza API]
    Realtime[Realtime Gateway<br/>SignalR Hub]

    Identity[Identity & Access]
    Branches[Branch Management]
    Catalog[Catalog]
    Customers[Customer Management]
    StoreOps[Store Operations<br/>Tables and dining sessions]
    Ordering[Ordering]
    Inventory[Inventory]
    Fulfillment[Delivery & Fulfillment]
    Payments[Payments<br/>Deferred MVP]
    Notifications[Notifications]
    Reporting[Reporting<br/>Read models]

    CustomerApp -->|HTTPS / REST| API
    StaffApp -->|HTTPS / REST| API
    StaffApp <-->|Authenticated connection| Realtime
    CustomerApp <-->|Authenticated connection| Realtime
    API --> Identity
    API --> Branches
    API --> Catalog
    API --> Customers
    API --> StoreOps
    API --> Ordering
    API --> Inventory
    API --> Fulfillment
    API --> Reporting

    Ordering -->|Product availability and price quote| Catalog
    Ordering -->|Reserve/release/consume recipe stock| Inventory
    Ordering -->|Order accepted/ready events| Notifications
    Ordering -->|Create or update delivery| Fulfillment
    Ordering -->|Validate dine-in table/session| StoreOps
    Ordering -.->|Payment events / port, future| Payments
    Fulfillment -->|Delivery status events| Ordering
    Branches -->|Published changes / read contracts| Reporting
    Catalog -->|Published changes / read contracts| Reporting
    Ordering -->|Committed order events| Reporting
    Inventory -->|Committed stock events| Reporting
    Fulfillment -->|Committed delivery events| Reporting

    Ordering -->|Committed Order events via Outbox| Realtime
    Fulfillment -->|Committed Delivery events via Outbox| Realtime
    Identity -.->|User, role and branch claims| API
    Branches -.->|Authorized branch scope| API
```

### Context responsibilities and ownership

| Context or module      | Owns                                                                                                   | Important boundary                                                                                                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| Identity & Access      | Authentication principals, credentials, roles, permissions, refresh-token lifecycle                             | Supports staff and customer authentication policies; never expose staff claims to customer clients. Customer profile data belongs to Customer Management. |
| Branch Management      | Branches, operational configuration, branch membership/authorization                                   | Does not own orders, stock, or branch-specific catalog prices.                                                                      |
| Catalog                | Products, sizes, flavors, edges, add-ons, branch price/availability, and links to sellable recipes     | Exposes a pricing/availability query contract; Inventory owns ingredient recipes. Order stores price snapshots for historical pricing. |
| Customer Management    | Customer profiles, contact information, addresses and consent                                          | Ordering stores customer ID and delivery-address snapshot; it does not depend on mutable address data after placement.              |
| Store Operations       | Branch tables and dining sessions required by `DineIn`                                                  | Owns table/session lifecycle only. No table reservation or shift-management workflow is implied unless separately approved.          |
| Ordering               | Order aggregate, item/options snapshots, totals, state transitions, order history, idempotency records | Owns order lifecycle. Commands validate transitions and do not accept client-supplied prices.                                       |
| Inventory              | Ingredients, units, recipes, branch stock balances, reservations, movements and adjustments            | Owns stock consistency and reservations; no other context updates balances directly.                                                |
| Delivery & Fulfillment | Delivery assignment, driver, dispatch and delivery lifecycle                                           | Owns delivery state separately from order state; communicates completion to Ordering through an application port/event.             |
| Payments               | Payment attempts and payment state                                                                     | Deferred from MVP; keep a seam, not a prematurely implemented gateway abstraction.                                                  |
| Notifications          | Customer/staff notification delivery                                                                   | Consumes committed events; delivery failure must not roll back an already committed order.                                          |
| Reporting              | Branch-scoped read models and operational report queries                                               | Supporting read module, not a source-of-truth context. Consumes committed events or explicit read contracts; never reads source tables directly. |

Audit history, authorization enforcement, telemetry, health checks, and delivery of integration events are cross-cutting/platform capabilities, not additional MVP Bounded Contexts. The KDS is a staff-client workflow over Ordering, not a separate source of truth. Administrative use cases stay with the context that owns the managed data. Promotions, fiscal operations, and payment gateway processing remain out of scope.

### Requirement-to-context coverage

| Product requirement | Owning context(s) or capability |
|---|---|
| RF-01 Branches, users, roles, and permissions | Branch Management; Identity & Access |
| RF-02–03 Catalog, branch prices, and availability | Catalog |
| RF-04–07 Order creation, pizza configuration, customer snapshots, and KDS | Ordering; Catalog; Customer Management; Store Operations; Notifications |
| RF-08 Ingredient recipes, reservations, and movements | Inventory |
| RF-09 Driver assignment and delivery lifecycle | Delivery & Fulfillment |
| RF-10 Operational change history | Cross-cutting audit capability |
| RF-11 Customer catalog and self-scoped order tracking | Identity & Access; Customer Management; Catalog; Ordering |
| RF-12 Branch-scoped operational reporting | Reporting read module |

This mapping shows no additional MVP source-of-truth context is currently required. Store Operations and Reporting were previously implicit in the requirements and are now explicit in the design; the remaining gaps are cross-cutting capabilities or unresolved product decisions, not unmodeled contexts.

### Cross-context consistency

- Order confirmation and inventory reservation must be atomic in the initial modular monolith. Coordinate through an application-level inventory port and one local database transaction; do not call remote services from inside that transaction.
- Persist the order changes and outbox records in the same transaction. Publish outbox messages asynchronously after commit.
- SignalR notifications may be delayed or duplicated. Clients should de-duplicate by event ID and re-query the REST API after reconnecting.
- If contexts are split into separate services/databases later, replace local atomicity with an explicit saga/compensation policy; do not assume a distributed transaction.

## 3. Domain and application contracts

The contracts below are the initial persistence/application boundary. Domain repository interfaces live with their consuming domain aggregates. `IUnitOfWork` is an Application abstraction. Infrastructure implements these contracts with EF Core and SQL Server; EF Core types do not leak into Domain or Application.

```csharp
// BytePizza.Domain/Orders/IOrderRepository.cs
namespace BytePizza.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);
}
```

```csharp
// BytePizza.Domain/Catalog/IProductRepository.cs
namespace BytePizza.Domain.Catalog;

public interface IProductRepository
{
    Task<Product?> GetAvailableForSaleAsync(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken);
}
```

```csharp
// BytePizza.Domain/Customers/ICustomerRepository.cs
namespace BytePizza.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken);
}
```

```csharp
// BytePizza.Domain/Inventory/IInventoryRepository.cs
namespace BytePizza.Domain.Inventory;

public interface IInventoryRepository
{
    Task<StockBalance?> GetBalanceAsync(
        Guid branchId,
        Guid ingredientId,
        CancellationToken cancellationToken);

    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken);

    Task AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken);

    Task<StockReservation?> GetReservationForOrderAsync(
        Guid branchId,
        Guid orderId,
        CancellationToken cancellationToken);
}
```

```csharp
// BytePizza.Application/Common/Persistence/IUnitOfWork.cs
namespace BytePizza.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
```

```csharp
// BytePizza.Application/Orders/Queries/IOrderQueries.cs
namespace BytePizza.Application.Orders.Queries;

using BytePizza.Application.Orders.Contracts;

public interface IOrderQueries
{
    Task<CreateOrderResponse?> GetByIdAsync(
        Guid branchId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<OrderListResponse> ListAsync(
        Guid branchId,
        OrderStatus? status,
        string? cursor,
        CancellationToken cancellationToken);
}
```

`ExecuteInTransactionAsync` is for database work only. It must use the provider execution strategy correctly, commit tracked changes and outbox records atomically, and must not contain network calls or SignalR sends. `IOrderRepository` is for aggregate operations in commands; `IOrderQueries` is implemented with read-only, `AsNoTracking` projections directly to DTOs.

Additional application ports expected in the implementation:

```csharp
namespace BytePizza.Application.Orders;

public interface IBranchAccessService
{
    Task<bool> CanAccessAsync(
        Guid userId,
        Guid branchId,
        CancellationToken cancellationToken);
}

public interface IInventoryReservationService
{
    Task ReserveForOrderAsync(
        Guid branchId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task ConsumeForOrderAsync(
        Guid branchId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task ReleaseForOrderAsync(
        Guid branchId,
        Guid orderId,
        CancellationToken cancellationToken);
}
```

The reservation service is an application boundary implemented by the Inventory module. Its implementation participates in the same local unit of work for order confirmation. Repositories should expose aggregate persistence, not generic CRUD for every entity.

## 4. Order HTTP API and OpenAPI contract

### Order flow

1. The client requests an order with product IDs, options, and quantities; it never supplies authoritative unit prices or totals.
2. The API resolves the authenticated customer/staff identity and authorized branch.
3. Ordering obtains current branch availability and server-side prices from Catalog, validates flavor fractions, and builds immutable price snapshots.
4. On confirmation, Ordering reserves recipe stock in the same local transaction. Insufficient stock fails without a partial reservation.
5. The API persists the order, history, idempotency result, and outbox event. Only after commit does the publisher notify SignalR clients.
6. KDS and customer clients use REST to recover authoritative state after disconnect/reconnect.

### Routes

| Method | Route                                                               | Purpose                                                  | Success                          |
| ------ | ------------------------------------------------------------------- | -------------------------------------------------------- | -------------------------------- |
| `POST` | `/api/v1/branches/{branchId}/orders`                                | Create and confirm an order                              | `201 Created`, `Location` header |
| `GET`  | `/api/v1/branches/{branchId}/orders/{orderId}`                      | Get order detail, scoped to customer or authorized staff | `200 OK`                         |
| `GET`  | `/api/v1/branches/{branchId}/orders?status=&after=&before=&cursor=` | KDS/staff order list; customer list is self-scoped       | `200 OK`                         |
| `POST` | `/api/v1/branches/{branchId}/orders/{orderId}/transitions`          | Apply an allowed lifecycle transition                    | `200 OK`                         |

Create requires an `Idempotency-Key` header (UUID). Repeating the same key and same request returns the original result; reusing it for a different request returns `409 Conflict`. Transition requests include the expected order version to detect concurrent updates.

### DTO contracts

```csharp
namespace BytePizza.Application.Orders.Contracts;

public sealed record CreateOrderRequest(
    Guid? CustomerId,
    OrderType OrderType,
    Guid? AddressId,
    string? DeliveryAddressSnapshot,
    Guid? TableId,
    string? Notes,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    Guid SizeId,
    int Quantity,
    Guid? EdgeId,
    IReadOnlyList<CreateOrderFlavorRequest> Flavors,
    IReadOnlyList<CreateOrderAddonRequest> Addons,
    string? Notes);

public sealed record CreateOrderFlavorRequest(
    Guid FlavorId,
    decimal Fraction);

public sealed record CreateOrderAddonRequest(
    Guid AddonId,
    int Quantity);

public sealed record CreateOrderResponse(
    Guid Id,
    string OrderNumber,
    Guid BranchId,
    OrderStatus Status,
    OrderType OrderType,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Discount,
    decimal Total,
    string Currency,
    DateTimeOffset CreatedAt,
    long Version,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SizeName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    decimal? EdgePrice,
    IReadOnlyList<OrderFlavorResponse> Flavors,
    IReadOnlyList<OrderAddonResponse> Addons,
    string? Notes);

public sealed record OrderFlavorResponse(
    Guid FlavorId,
    string Name,
    decimal Fraction,
    decimal AppliedUnitPrice);

public sealed record OrderAddonResponse(
    Guid AddonId,
    string Name,
    int Quantity,
    decimal UnitPrice);

public sealed record ChangeOrderStatusRequest(
    OrderStatus TargetStatus,
    long ExpectedVersion,
    string? Reason);

public sealed record OrderStatusResponse(
    Guid OrderId,
    OrderStatus Status,
    long Version,
    DateTimeOffset ChangedAt);

public sealed record OrderListResponse(
    IReadOnlyList<OrderSummaryResponse> Items,
    string? NextCursor);

public sealed record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    OrderType OrderType,
    OrderStatus Status,
    string? CustomerName,
    decimal Total,
    DateTimeOffset CreatedAt,
    long Version);

public enum OrderType
{
    Counter,
    DineIn,
    Delivery,
    Takeout
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Preparing,
    Ready,
    Completed,
    Canceled
}
```

The request's `DeliveryAddressSnapshot` is accepted only for a guest or where the caller is allowed to use the supplied address. Prefer resolving a stored `AddressId` for an authenticated customer, then copy a validated snapshot into the order. If the request contains both fields, define and validate precedence rather than silently selecting one. `TableId` is required for `DineIn`; `AddressId` or an approved guest address is required for `Delivery`; these conditional rules belong in application validation.

`UnitPrice`, `TotalPrice`, discounts, fees, and totals are response-only server calculations. `Fraction` values must be positive and sum exactly to `1.0000` within the configured precision. Weighted flavor price is `Σ(fraction × branch/size flavor price)`. Define the currency scale and rounding point centrally; do not round each flavor fraction independently in a way that changes the final sum.

### OpenAPI excerpt

```yaml
openapi: 3.1.0
info:
  title: BytePizza Orders API
  version: 1.0.0
servers:
  - url: https://api.bytepizza.example
paths:
  /api/v1/branches/{branchId}/orders:
    post:
      operationId: createOrder
      summary: Create and confirm an order
      security:
        - bearerAuth: []
      parameters:
        - $ref: "#/components/parameters/BranchId"
        - name: Idempotency-Key
          in: header
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/CreateOrderRequest"
      responses:
        "201":
          description: Order created; Location points to the order resource
          headers:
            Location:
              required: true
              schema:
                type: string
                format: uri-reference
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/CreateOrderResponse"
        "400":
          $ref: "#/components/responses/Problem"
        "401":
          $ref: "#/components/responses/Problem"
        "403":
          $ref: "#/components/responses/Problem"
        "404":
          $ref: "#/components/responses/Problem"
        "409":
          description: Idempotency conflict, concurrent update, or invalid state
          content:
            application/problem+json:
              schema:
                $ref: "#/components/schemas/ProblemDetails"
        "422":
          description: Business rule violation, including insufficient stock
          content:
            application/problem+json:
              schema:
                $ref: "#/components/schemas/ProblemDetails"
    get:
      operationId: listOrders
      summary: List orders visible to the authenticated caller
      security:
        - bearerAuth: []
      parameters:
        - $ref: "#/components/parameters/BranchId"
        - name: status
          in: query
          schema:
            $ref: "#/components/schemas/OrderStatus"
        - name: cursor
          in: query
          schema:
            type: string
      responses:
        "200":
          description: Page of authorized orders
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/OrderListResponse"
        "401":
          $ref: "#/components/responses/Problem"
        "403":
          $ref: "#/components/responses/Problem"
  /api/v1/branches/{branchId}/orders/{orderId}:
    get:
      operationId: getOrder
      summary: Get an order visible to the authenticated caller
      security:
        - bearerAuth: []
      parameters:
        - $ref: "#/components/parameters/BranchId"
        - name: orderId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: Order detail
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/CreateOrderResponse"
        "401":
          $ref: "#/components/responses/Problem"
        "403":
          $ref: "#/components/responses/Problem"
        "404":
          $ref: "#/components/responses/Problem"
  /api/v1/branches/{branchId}/orders/{orderId}/transitions:
    post:
      operationId: changeOrderStatus
      summary: Apply an allowed order state transition
      security:
        - bearerAuth: []
      parameters:
        - $ref: "#/components/parameters/BranchId"
        - name: orderId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/ChangeOrderStatusRequest"
      responses:
        "200":
          description: Transition applied
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/OrderStatusResponse"
        "400":
          $ref: "#/components/responses/Problem"
        "401":
          $ref: "#/components/responses/Problem"
        "403":
          $ref: "#/components/responses/Problem"
        "404":
          $ref: "#/components/responses/Problem"
        "409":
          description: Invalid transition or stale expectedVersion
          content:
            application/problem+json:
              schema:
                $ref: "#/components/schemas/ProblemDetails"
components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
  parameters:
    BranchId:
      name: branchId
      in: path
      required: true
      schema:
        type: string
        format: uuid
  responses:
    Problem:
      description: RFC 9457 Problem Details response
      content:
        application/problem+json:
          schema:
            $ref: "#/components/schemas/ProblemDetails"
  schemas:
    CreateOrderRequest:
      type: object
      required: [orderType, items]
      properties:
        customerId:
          type: [string, "null"]
          format: uuid
        orderType:
          $ref: "#/components/schemas/OrderType"
        addressId:
          type: [string, "null"]
          format: uuid
        deliveryAddressSnapshot:
          type: [string, "null"]
        tableId:
          type: [string, "null"]
          format: uuid
        notes:
          type: [string, "null"]
          maxLength: 500
        items:
          type: array
          minItems: 1
          items:
            $ref: "#/components/schemas/CreateOrderItemRequest"
    CreateOrderItemRequest:
      type: object
      required: [productId, sizeId, quantity, flavors, addons]
      properties:
        productId:
          type: string
          format: uuid
        sizeId:
          type: string
          format: uuid
        quantity:
          type: integer
          minimum: 1
        edgeId:
          type: [string, "null"]
          format: uuid
        flavors:
          type: array
          minItems: 1
          items:
            $ref: "#/components/schemas/CreateOrderFlavorRequest"
        addons:
          type: array
          items:
            $ref: "#/components/schemas/CreateOrderAddonRequest"
        notes:
          type: [string, "null"]
    CreateOrderFlavorRequest:
      type: object
      required: [flavorId, fraction]
      properties:
        flavorId:
          type: string
          format: uuid
        fraction:
          type: number
          exclusiveMinimum: 0
          maximum: 1
    CreateOrderAddonRequest:
      type: object
      required: [addonId, quantity]
      properties:
        addonId:
          type: string
          format: uuid
        quantity:
          type: integer
          minimum: 1
    CreateOrderResponse:
      type: object
      required:
        [
          id,
          orderNumber,
          branchId,
          status,
          orderType,
          subtotal,
          total,
          currency,
          createdAt,
          version,
          items,
        ]
      properties:
        id:
          type: string
          format: uuid
        orderNumber:
          type: string
        branchId:
          type: string
          format: uuid
        status:
          $ref: "#/components/schemas/OrderStatus"
        orderType:
          $ref: "#/components/schemas/OrderType"
        subtotal:
          type: number
          format: decimal
        deliveryFee:
          type: number
          format: decimal
        discount:
          type: number
          format: decimal
        total:
          type: number
          format: decimal
        currency:
          type: string
          example: BRL
        createdAt:
          type: string
          format: date-time
        version:
          type: integer
          format: int64
        items:
          type: array
          items:
            $ref: "#/components/schemas/OrderItemResponse"
    OrderItemResponse:
      type: object
      required:
        [
          id,
          productId,
          productName,
          sizeName,
          quantity,
          unitPrice,
          totalPrice,
          flavors,
          addons,
        ]
      properties:
        id:
          type: string
          format: uuid
        productId:
          type: string
          format: uuid
        productName:
          type: string
        sizeName:
          type: string
        quantity:
          type: integer
        unitPrice:
          type: number
          format: decimal
        totalPrice:
          type: number
          format: decimal
        edgePrice:
          type: [number, "null"]
          format: decimal
        flavors:
          type: array
          items:
            $ref: "#/components/schemas/OrderFlavorResponse"
        addons:
          type: array
          items:
            $ref: "#/components/schemas/OrderAddonResponse"
        notes:
          type: [string, "null"]
    OrderFlavorResponse:
      type: object
      required: [flavorId, name, fraction, appliedUnitPrice]
      properties:
        flavorId:
          type: string
          format: uuid
        name:
          type: string
        fraction:
          type: number
        appliedUnitPrice:
          type: number
          format: decimal
    OrderAddonResponse:
      type: object
      required: [addonId, name, quantity, unitPrice]
      properties:
        addonId:
          type: string
          format: uuid
        name:
          type: string
        quantity:
          type: integer
        unitPrice:
          type: number
          format: decimal
    ChangeOrderStatusRequest:
      type: object
      required: [targetStatus, expectedVersion]
      properties:
        targetStatus:
          $ref: "#/components/schemas/OrderStatus"
        expectedVersion:
          type: integer
          format: int64
        reason:
          type: [string, "null"]
    OrderStatusResponse:
      type: object
      required: [orderId, status, version, changedAt]
      properties:
        orderId:
          type: string
          format: uuid
        status:
          $ref: "#/components/schemas/OrderStatus"
        version:
          type: integer
          format: int64
        changedAt:
          type: string
          format: date-time
    OrderListResponse:
      type: object
      required: [items]
      properties:
        items:
          type: array
          items:
            $ref: "#/components/schemas/OrderSummaryResponse"
        nextCursor:
          type: [string, "null"]
    OrderSummaryResponse:
      type: object
      required: [id, orderNumber, orderType, status, total, createdAt, version]
      properties:
        id:
          type: string
          format: uuid
        orderNumber:
          type: string
        orderType:
          $ref: "#/components/schemas/OrderType"
        status:
          $ref: "#/components/schemas/OrderStatus"
        customerName:
          type: [string, "null"]
        total:
          type: number
          format: decimal
        createdAt:
          type: string
          format: date-time
        version:
          type: integer
          format: int64
    OrderType:
      type: string
      enum: [Counter, DineIn, Delivery, Takeout]
    OrderStatus:
      type: string
      enum: [Pending, Confirmed, Preparing, Ready, Completed, Canceled]
    ProblemDetails:
      type: object
      required: [type, title, status]
      properties:
        type:
          type: string
          format: uri
        title:
          type: string
        status:
          type: integer
        detail:
          type: string
        instance:
          type: string
          format: uri-reference
        traceId:
          type: string
        errors:
          type: object
          additionalProperties:
            type: array
            items:
              type: string
```

### HTTP error mapping

- `400 Bad Request`: malformed payload or failed structural validation.
- `401 Unauthorized`: missing/invalid authentication.
- `403 Forbidden`: authenticated caller lacks branch/operation permission.
- `404 Not Found`: resource does not exist or is intentionally concealed by access policy.
- `409 Conflict`: stale order version, invalid lifecycle transition, or idempotency-key reuse with a different payload.
- `422 Unprocessable Content`: valid request that violates business rules, such as unavailable item, invalid flavor composition, or insufficient stock.
- `application/problem+json`: standardized Problem Details body. Use RFC 9457-compatible ASP.NET Core ProblemDetails while preserving the required response shape.

## 5. Solution project and folder tree

Use one backend project per architectural layer, with bounded contexts as first-class folders. This preserves the referenced prompt's four backend projects while avoiding a single undifferentiated `Services` folder. The two MAUI applications share only client-side presentation/API abstractions, not UI workflows.

```text
BytePizza/
├── BytePizza.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── README.md
├── docs/
│   ├── 00-po-specification.md
│   ├── 01-architecture-blueprint.md
│   └── adr/
│       ├── 0001-dotnet-version.md
│       ├── 0002-sql-server.md
│       ├── 0003-signalr-realtime.md
│       └── 0004-ci-cd.md
├── .github/
│   └── workflows/
│       ├── ci-api.yml
│       ├── ci-mobile-android.yml
│       └── ci-client-windows.yml
├── src/
│   ├── BytePizza.Domain/
│   │   ├── Common/
│   │   │   ├── Entity.cs
│   │   │   ├── ValueObject.cs
│   │   │   └── IDomainEvent.cs
│   │   ├── Branches/
│   │   │   ├── Branch.cs
│   │   │   └── IBranchRepository.cs
│   │   ├── Catalog/
│   │   │   ├── Product.cs
│   │   │   ├── Flavor.cs
│   │   │   ├── Edge.cs
│   │   │   ├── ProductSize.cs
│   │   │   └── IProductRepository.cs
│   │   ├── Customers/
│   │   │   ├── Customer.cs
│   │   │   ├── Address.cs
│   │   │   └── ICustomerRepository.cs
│   │   ├── Orders/
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   ├── OrderFlavor.cs
│   │   │   ├── OrderStatus.cs
│   │   │   ├── OrderType.cs
│   │   │   ├── Money.cs
│   │   │   ├── OrderPlacedDomainEvent.cs
│   │   │   └── IOrderRepository.cs
│   │   ├── Inventory/
│   │   │   ├── Ingredient.cs
│   │   │   ├── Recipe.cs
│   │   │   ├── StockBalance.cs
│   │   │   ├── StockReservation.cs
│   │   │   ├── StockMovement.cs
│   │   │   └── IInventoryRepository.cs
│   │   └── Fulfillment/
│   │       ├── Delivery.cs
│   │       ├── DeliveryStatus.cs
│   │       └── IDeliveryRepository.cs
│   ├── BytePizza.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   ├── Errors/
│   │   │   ├── Persistence/IUnitOfWork.cs
│   │   │   └── Results/Result.cs
│   │   ├── Branches/
│   │   │   ├── Contracts/
│   │   │   └── Queries/
│   │   ├── Catalog/
│   │   │   ├── Contracts/
│   │   │   ├── Queries/
│   │   │   └── Validators/
│   │   ├── Customers/
│   │   │   ├── Contracts/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Orders/
│   │   │   ├── Contracts/
│   │   │   ├── Commands/CreateOrder/
│   │   │   ├── Commands/ChangeOrderStatus/
│   │   │   ├── Queries/GetOrder/
│   │   │   ├── Queries/ListOrders/
│   │   │   ├── Ports/IBranchAccessService.cs
│   │   │   ├── Ports/IInventoryReservationService.cs
│   │   │   └── Validators/
│   │   ├── Inventory/
│   │   │   ├── Contracts/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   └── Fulfillment/
│   │       ├── Contracts/
│   │       ├── Commands/
│   │       └── Queries/
│   ├── BytePizza.Infrastructure/
│   │   ├── DependencyInjection.cs
│   │   ├── Persistence/
│   │   │   ├── BytePizzaDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/
│   │   │   └── UnitOfWork.cs
│   │   ├── Identity/
│   │   ├── Realtime/
│   │   ├── Outbox/
│   │   ├── Observability/
│   │   └── Resilience/
│   ├── BytePizza.Api/
│   │   ├── BytePizza.Api.csproj
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   ├── Orders/
│   │   │   ├── Catalog/
│   │   │   ├── Customers/
│   │   │   ├── Inventory/
│   │   │   └── Fulfillment/
│   │   ├── Hubs/OrderUpdatesHub.cs
│   │   ├── Authorization/
│   │   ├── Middleware/
│   │   ├── OpenApi/
│   │   └── Configuration/
│   ├── BytePizza.Client.Shared/
│   │   ├── Api/
│   │   ├── Authentication/
│   │   ├── Navigation/
│   │   ├── Localization/
│   │   └── Validation/
│   ├── BytePizza.Client.Customer/
│   │   ├── BytePizza.Client.Customer.csproj
│   │   ├── App.xaml
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Models/
│   │   └── Resources/
│   └── BytePizza.Client.Staff/
│       ├── BytePizza.Client.Staff.csproj
│       ├── App.xaml
│       ├── Views/
│       ├── ViewModels/
│       ├── Kds/
│       ├── Models/
│       └── Resources/
└── tests/
    ├── BytePizza.Domain.UnitTests/
    ├── BytePizza.Application.UnitTests/
    ├── BytePizza.Infrastructure.IntegrationTests/
    ├── BytePizza.Api.IntegrationTests/
    └── BytePizza.ArchitectureTests/
```

The tree is a target, not a list of files already present. Extend it with these context folders as their lanes are implemented:

- `BytePizza.Domain/StoreOperations/` for dining tables/sessions.
- `BytePizza.Application/StoreOperations/` and `BytePizza.Application/Reporting/` for use cases and read contracts.
- `BytePizza.Api/Endpoints/StoreOperations/` and `BytePizza.Api/Endpoints/Reporting/`.
- `BytePizza.Infrastructure/Audit/` for audit adapters; audit remains a cross-cutting capability.
- Dedicated Store Operations, Reporting, and client test folders under the matching test projects.

### Dependency direction

```text
BytePizza.Domain       -> no project/framework dependencies
BytePizza.Application  -> BytePizza.Domain
BytePizza.Infrastructure -> BytePizza.Application + BytePizza.Domain
BytePizza.Api          -> BytePizza.Application + BytePizza.Infrastructure (composition root only)
BytePizza.Client.*     -> BytePizza.Client.Shared (no reference to backend projects)
Tests                  -> projects under test
```

Do not make the API depend on Infrastructure types outside `Program.cs` composition. Client request/response contracts should be generated from OpenAPI or maintained as client-owned contracts; the MAUI client must not reference the server Application project.

## 6. Realtime contract

Expose an authenticated hub at `/hubs/order-updates`. The server derives allowed branch and customer groups from validated identity claims and authorization services; clients must not be allowed to join arbitrary group names.

Suggested server-to-client events:

```text
OrderChanged(OrderChangedEvent)
DeliveryChanged(DeliveryChangedEvent)
```

`OrderChangedEvent` contains `eventId`, `branchId`, `orderId`, `orderNumber`, `status`, `version`, and `occurredAt`. It must not contain payment credentials, full address details, or unnecessary customer PII. The client compares `version` to its last known version and fetches the REST resource if it detects a gap. Publish only after the database transaction commits, preferably by consuming the persisted outbox.

## 7. Architectural Decision Records

### ADR-0001 — .NET target framework

- **Status:** Accepted — target .NET 10; .NET 9 rejected.
- **Context:** The product specification and current Domain/test projects use .NET 10. Older agent prompts and the initial blueprint contained conflicting .NET 9 references.
- **Decision:** Target `net10.0` for API and shared backend projects and the corresponding .NET MAUI 10 target frameworks. Pin the SDK via `global.json` to an installed, supported .NET 10 SDK. Verify third-party package and platform workload compatibility before implementation.
- **Consequences:** Keeps API, mobile clients, tests, and CI aligned. Any future framework change must update this ADR, project targets, workloads, and all agent prompts together.

### ADR-0002 — SQL Server as production database

- **Status:** Accepted.
- **Context:** SQL Server was selected in the product decisions. Initial ordering and inventory confirmation needs local transactional consistency.
- **Decision:** Use EF Core with the SQL Server provider and one production database for the modular monolith. Keep schema/table ownership per context, enforce branch scoping in application/query boundaries, and use SQL Server concurrency support (for example `rowversion`) for stock balances and order versions. Integration tests must run against SQL Server, not rely solely on an in-memory provider.
- **Consequences:** Provider-specific features and migrations are authoritative. Do not claim PostgreSQL support unless it is separately implemented and tested. Cross-context confirmation is an explicit local transaction; all external notifications use outbox publication after commit.

### ADR-0003 — SignalR for operational realtime updates

- **Status:** Accepted.
- **Context:** Staff KDS and customer tracking need prompt updates, but REST remains the authoritative order interface.
- **Decision:** Use ASP.NET Core SignalR for push updates. Authenticate connections, authorize branch/customer subscriptions server-side, publish committed domain integration events through an outbox, and expose REST resynchronization after reconnect.
- **Consequences:** SignalR delivery is treated as at-least-once/best-effort from the client's perspective; events are versioned and de-duplicated. If deployment scales beyond one API instance, configure a supported SignalR backplane or managed service. Do not broadcast sensitive data to branch-wide groups.

### ADR-0004 — CI/CD by workload and target platform

- **Status:** Accepted.
- **Context:** API, Android MAUI, and Windows KDS have different SDK/workload and runner requirements; one Ubuntu API build cannot validate the Windows client.
- **Decision:** Use GitHub Actions with separate jobs/workflows: Linux for restore, format/analyzers, API build and tests; SQL Server-backed integration tests in CI; a compatible Android runner for MAUI Android build; and Windows runner for Windows client/KDS build. Trigger pull requests and pushes according to the agreed `develop` and `feature/**` workflow. Pin SDK/workload setup and use lock files where practical.
- **Consequences:** CI time and runner costs increase, but each deliverable is built on a supported platform. Keep secrets in GitHub environments, restrict deployment to protected branches/tags, build once and promote artifacts, and never deploy from untrusted pull-request code. Add deployment/release jobs only after hosting targets and environments are selected.

## 8. Decisions still required before their affected contracts are frozen

These are explicit product-owner decision gates. The implementation coordinator should record each answer in the specification/ADR before assigning work that depends on it; agents must not silently invent behavior.

| Decision | Affected work | Recommended default if approved |
|---|---|---|
| Central catalog with branch overrides, or fully branch-managed catalog/prices | Catalog, reports | Shared product definitions with branch-specific price and availability overrides |
| Customer account required, guest checkout, or both; supported customer-app platforms | Identity, Ordering, MAUI | Authenticated account first; guest checkout only after its privacy and order-recovery rules are specified |
| Manual payment recording in MVP | Ordering, staff app | Keep payment state out of order lifecycle; defer manual payment records unless explicitly required |
| Stock reservation/consumption and cancellation compensation | Inventory, Ordering | Reserve at confirmation; consume at preparation; post-preparation cancellation requires an audited adjustment |
| Delivery fees, discounts, rounding, table/session rules, cancellation policy | Catalog, Ordering, Store Operations, Fulfillment | Define before freezing pricing and order contracts; do not make silent defaults |
| Expected load/SLO, hosting, environments, release and rollback policy | Infrastructure, CI/CD | Start with agreed measurable targets and a non-production deployment before production |

The existing baseline decisions are .NET 10, SQL Server, modular monolith, online-only operation, server-calculated prices, branch-scoped authorization, and no payment gateway in the MVP.

## 9. Implementation roadmap and parallel-agent protocol

### 9.1 Coordination rules

1. Assign one integration owner to freeze contracts, own `BytePizza.slnx`, shared project/package configuration, shared `DbContext`/dependency injection, migrations, and merges.
2. Run independent lanes in separate branches or worktrees. Give each agent a bounded scope, named files/folders, upstream contract, acceptance criteria, and explicit exclusions.
3. Use context paths such as `src/BytePizza.Domain/Catalog/` and `src/BytePizza.Application/Catalog/`; testers own only the assigned context's test folder. Parallel agents must not edit the same shared files. Only the integration owner changes solution/project/package configuration, shared `DbContext`/DI, and migrations.
4. For each implementation wave: **Coder agents implement in parallel → integrate and build → Tester agents add/run independent suites in parallel → integrate and run all affected tests → Reviewer agents review the integrated diff in parallel → fix findings → rerun tests and review changed areas**.
5. Testers should receive the frozen contracts and acceptance criteria, not another agent's unintegrated assumptions. Reviewers review the integrated commit/PR, not competing working copies.
6. Every lane reports changed files, commands/results, known gaps, and handoff dependencies. No phase is complete on a code-only claim: its exit gate must pass.

### 9.2 Delivery waves

| Wave | Parallel work lanes | Required outputs and exit gate |
|---|---|---|
| 0 — Product and solution foundation | PO/Architect resolves decision gates that block contracts; one Coder/DevOps lane creates projects, SDK/build conventions, dependency rules, and CI skeleton | Projects in solution; .NET 10 alignment; architecture tests prevent forbidden references; all current domain unit tests pass (record the executed count in validation evidence). No context implementation starts against moving contracts. |
| 1 — Independent domain contexts | Coders: **Branch & access model**; **Catalog & pricing rules**; **Customer profile/address**; **Store Operations tables/sessions**; **Inventory & recipes**; **Ordering aggregate/state/history**; **Fulfillment lifecycle**. Keep file ownership within each context. | Pure-domain invariants and domain events; context ownership documented; no EF/API dependencies in Domain. Each lane builds independently and its contracts are integrated. |
| 1T / 1R — Domain verification | Testers split by context-owned test folders and write unit tests for invariants, boundary/error results, and allowed transitions. Then reviewers split into domain/DDD and dependency-boundary lanes. | All domain tests pass; architecture tests enforce layer boundaries; review findings resolved. Shared test project configuration is changed only by the integration owner. |
| 2 — Application use cases | Coders: **Catalog read/pricing ports**; **branch authorization and customer access**; **create/query/transition order**; **inventory reserve/consume/release**; **delivery/table workflows**; **report query contracts**. Ordering uses frozen Catalog and Inventory ports. | Commands/queries, DTOs, validators, CancellationToken propagation, Result/Problem mapping contracts, `AsNoTracking` query interfaces, and idempotency/concurrency contracts. |
| 2T / 2R — Use-case verification | Testers split by application feature: validators and handlers, authorization matrix, idempotency, state transitions, stock failure/rollback contract. Reviewers split by business rules, authorization, async/error semantics. | Handler tests pass without databases; negative and cross-branch cases are covered; no accepted client-supplied prices. |
| 3 — Persistence and adapters | Coders: **EF mappings/repositories per context** in separate files; **SQL Server transaction/unit of work**; **identity adapter**; **outbox publisher**. Integration owner alone composes shared `DbContext`, DI, and migration. | SQL Server schema/migrations, concurrency controls, atomic order+stock+outbox transaction, retry/idempotency persistence. No context reads/writes another context's tables directly. |
| 3T / 3R — Integration verification | Testers split: **repository/mapping**; **transaction/concurrency/outbox**; **identity persistence**. Reviewers split: **SQL/data ownership**; **transaction correctness**; **resilience/secrets**. | SQL Server-backed tests pass, including insufficient-stock rollback, concurrent updates, and duplicate idempotency requests. In-memory EF is not accepted as a substitute. |
| 4 — API and security boundary | Coders: **Catalog/customer endpoints**; **Order endpoints/OpenAPI**; **Inventory/fulfillment endpoints**; **auth/branch policies and ProblemDetails**. One owner composes `Program.cs` and middleware. | Versioned API matches approved contracts; branch/customer resource authorization enforced server-side; validation and error status mapping are consistent. |
| 4T / 4R — API verification | Testers split by endpoint groups using `WebApplicationFactory` and SQL Server test fixtures; reviewers split by auth/OWASP, API/OpenAPI contract, privacy/logging. | Integration/contract tests prove allowed and forbidden access, validation, idempotency, status transitions, and RFC-compatible errors. |
| 5 — Realtime, reporting, operations | Coders: **outbox dispatch and notification delivery**; **SignalR hub/groups/resync**; **branch-scoped report projections/queries**; **audit/telemetry/health checks**. | Outbox is committed atomically and retried safely; authenticated clients cannot subscribe to arbitrary groups; reports are read-only and scoped; sensitive data is excluded. |
| 5T / 5R — Cross-context verification | Testers split: **outbox/retry/duplicate delivery**; **SignalR authorization/resync**; **report projections and branch isolation**. Reviewers split: **event consistency**, **realtime privacy**, **query performance/observability**. | Tests verify post-commit publication, duplicate/gap behavior, reconnect REST resync, group authorization, and report scoping. |
| 6 — MAUI client applications | Coders: **shared typed API/auth/navigation primitives**; **customer catalog/order/tracking journey**; **staff order-entry/KDS journey**. Keep customer and staff UI projects distinct; one owner controls shared client contracts. | Android customer app and Windows staff/KDS app compile; strict MVVM; no server-project references; user-facing errors/loading/reconnect flows; online-only behavior is explicit. |
| 6T / 6R — Client verification | Testers split by customer journeys, staff/KDS journeys, and shared client services/ViewModels. Reviewers split by MVVM/accessibility/privacy and API/session handling. | ViewModel/service tests pass; critical journeys are exercised on supported targets; no business logic or API calls in code-behind. |
| 7 — Release readiness | DevOps: platform CI, SQL Server integration job, dependency/security checks, container/deployment pipeline and environments. QA: end-to-end smoke/load/recovery. Reviewers: final architecture/security/performance sweep. | Mainline quality gates are green; migrations and rollback are rehearsed; secrets and environments are configured outside source; SLOs and operational runbooks are approved; release candidate is deployable and observable. |

### 9.3 Task cards for coder/tester/reviewer agents

Create one task per lane above using this handoff shape:

```text
Role: Coder | Tester | Reviewer
Wave/lane:
Objective:
Frozen inputs/contracts:
Owned paths:
Must not edit:
Acceptance criteria:
Tests/commands and expected evidence:
Dependencies:
Handoff to:
```

For a context lane, keep production and test file ownership disjoint from other agents. Example: the Catalog coder owns `src/BytePizza.Domain/Catalog/**` and its assigned Application files; the Catalog tester owns only its designated test directory; the Catalog reviewer examines the integrated Catalog diff and reports findings without editing it unless separately assigned a remediation task. Shared contracts and dependency changes are integrated serially.

### 9.4 Definition of done for the application

- Product decisions and API/OpenAPI contracts are approved and versioned.
- All in-scope contexts in the diagram have an implemented owner, or are explicitly marked deferred; no accidental scope remains.
- Unit, architecture, SQL Server integration, API contract, client, and end-to-end smoke tests pass in CI.
- Branch/customer authorization, idempotency, order/stock atomicity, concurrency, outbox delivery, privacy, and recovery behavior have explicit tests.
- Android customer and Windows staff/KDS targets build on their supported runners.
- Deployment, secrets, migrations/rollback, health checks, alerts, backup/restore, and release procedures are documented and exercised for the agreed environments.
- Product owner accepts the deferred scope and operational SLO evidence before production.

### Handoff

Use the roadmap's wave/lane task cards to assign multiple Coder agents without overlapping ownership; only after integration, assign parallel Tester agents, then parallel Reviewers. Repeat this cycle for each wave and block dependent waves until their exit gates pass.
