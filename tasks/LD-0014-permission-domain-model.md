# LD-0014: Refine Permission Domain Model

## Title
Refine Permission Domain Entity

## Goal
Refine the existing `Permission` domain entity so it follows clean domain modeling practices while remaining fully compatible with the database schema created in **LD-0013**.

The objective is to ensure the entity has clear invariants, simple validation rules, and a stable structure without introducing unnecessary complexity.

## Scope

This task focuses on **improving the Permission domain entity**, not redesigning the authorization system.

Included:

- Review the existing `Permission` entity created in LD-0013
- Add lightweight validation for permission naming
- Ensure permission names follow the `Module.Action` convention
- Ensure controlled construction and protected internal state
- Confirm navigation relationships introduced in LD-0013 remain correct
- Apply minimal improvements to maintain domain consistency

Not included:

- Database schema changes
- EF Core migrations
- Permission seeding
- Authorization policies or handlers
- Endpoint integration
- Caching
- CQRS pipeline behaviors
- Admin APIs

## Implementation Steps

1. Review the current `Permission` entity created in **LD-0013**.

2. Ensure the entity enforces the **Module.Action** naming convention.

   Valid examples:

   - `Users.View`
   - `Users.Create`
   - `Products.Update`
   - `Orders.Approve`

3. Add simple domain validation to prevent invalid permission names.

4. Ensure the entity follows domain modeling practices:

   - private setters where appropriate
   - controlled construction
   - explicit methods for state updates

5. Add or refine domain methods if useful, such as:

   - metadata update
   - activate / deactivate

6. Confirm navigation relationships added in LD-0013 remain correct:

   - `Role → RolePermissions`
   - `User → UserPermissions`

7. Ensure the entity remains compatible with existing EF Core configurations.

## Constraints (Important)

- Do **not** modify the database schema.
- Do **not** create a new migration.
- Do **not** redesign existing `Role` or `User` aggregates.
- Do **not** introduce unnecessary value objects.
- Do **not** introduce infrastructure dependencies into Domain.
- Do **not** implement authorization logic in this task.
- Do **not** implement future tasks.

This task should remain **small, focused, and safe**.

## Acceptance Criteria

- `Permission` entity enforces the `Module.Action` naming format.
- Invalid permission names are prevented by domain validation.
- Entity uses controlled construction and protected setters.
- Navigation properties remain correct.
- Project builds successfully.
- No database migration is generated.
- No infrastructure or authorization logic appears in the Domain layer.