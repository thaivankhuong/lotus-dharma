# LD-0018: Endpoint Permission Integration

## Title
Integrate Permission Checks into Web Endpoints (Phase 1)

## Goal
Apply permission-based authorization to endpoints without breaking CQRS

## Scope
- Update existing endpoints to use permission attributes
- Maintain existing RequireAuthorization() calls
- Add permission attributes where appropriate
- Test endpoint authorization

## Implementation Steps
1. Identify endpoints requiring specific permissions
2. Add Permission attributes to endpoint methods
3. Update Categories endpoint as example
4. Update other endpoints (Products, TodoItems, etc.)
5. Test authorization works correctly
6. Ensure backward compatibility

## Acceptance Criteria
- Endpoints properly protected with permissions
- Existing RequireAuthorization() still works
- Permission checks happen at endpoint level
- Unauthorized requests properly rejected
- CQRS handlers remain unchanged