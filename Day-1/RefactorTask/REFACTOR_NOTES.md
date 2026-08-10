# Refactoring Notes

## 1. Giant controller action
**Smell:** The POST action contains almost all application logic.

**Consequence:** The code is difficult to understand, maintain, and test.

**Fix:** Move business logic into an OrderService.

## 2. Database access in controller
**Smell:** The controller directly uses Entity Framework.

**Consequence:** The controller is tightly coupled to the database.

**Fix:** Move database operations into an OrderRepository.

## 3. No separation of concerns
**Smell:** Validation, business logic, database access, and HTTP responses are mixed together.

**Consequence:** Changes in one area can affect unrelated functionality.

**Fix:** Separate Controller, Service, and Repository layers.

## 4. Empty catch blocks
**Smell:** Exceptions are caught and ignored.

**Consequence:** Errors disappear and debugging becomes difficult.

**Fix:** Remove unnecessary catches or catch specific exceptions, log them, and rethrow.

## 5. Synchronous EF Core calls
**Smell:** FirstOrDefault, ToList, and SaveChanges are synchronous inside an async action.

**Consequence:** Threads can be blocked during database operations.

**Fix:** Use FirstOrDefaultAsync, ToListAsync, and SaveChangesAsync.

## 6. Missing cancellation support
**Smell:** Database operations do not accept CancellationToken.

**Consequence:** Database work may continue after a request is cancelled.

**Fix:** Pass CancellationToken from Controller to Service to Repository and EF Core.

## 7. Untyped response
**Smell:** The endpoint returns object.

**Consequence:** The API response contract is unclear.

**Fix:** Return typed ActionResult or typed HTTP results.

## 8. Off-by-one bug
**Smell:** The loop uses `i <= request.Items.Count`.

**Consequence:** It attempts to access an index outside the collection.

**Fix:** Use `i < request.Items.Count` or foreach.

## 9. Possible null reference
**Smell:** `request.DiscountCode.Trim()` is called without checking for null.

**Consequence:** A missing discount code can cause NullReferenceException.

**Fix:** Check for null or use a null-safe approach.

## 10. No automated tests
**Smell:** The application has no tests.

**Consequence:** Refactoring can introduce regressions without being detected.

**Fix:** Add unit tests and an integration test.

## 11. Business rules in HTTP layer
**Smell:** Discounts and order calculations are implemented directly in the controller.

**Consequence:** Business rules are difficult to reuse and test.

**Fix:** Move order calculation and business rules to OrderService.

## 12. Poor exception handling
**Smell:** A broad exception handler catches everything.

**Consequence:** The API hides the actual failure and makes troubleshooting difficult.

**Fix:** Catch only expected specific exceptions and log them.

## 13. Multiple database SaveChanges calls
**Smell:** The controller calls SaveChanges more than once during one order operation.

**Consequence:** The operation may leave partial data if the second operation fails.

**Fix:** Move the operation into a service/repository and use one consistent transaction.

## 14. Hard-coded business rules
**Smell:** Discount values such as 10%, 5%, and 50 are embedded in the controller.

**Consequence:** Business rules are difficult to change and test.

**Fix:** Centralize business rules in the service/domain layer.

## 15. Controller is difficult to unit test
**Smell:** The controller directly depends on EF Core DbContext.

**Consequence:** Tests require database setup and become complicated.

**Fix:** Inject abstractions such as IOrderService.

## 16. Mixed responsibilities
**Smell:** The controller performs validation, calculations, persistence, logging, and response formatting.

**Consequence:** The class violates the Single Responsibility Principle.

**Fix:** Give each layer one clear responsibility.