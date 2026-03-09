# LD-0021: CQRS Authorization Behavior

## Title
Add Optional CQRS Pipeline Authorization (Phase 2)

## Goal
Implement deeper authorization at CQRS command/query level

## Scope
- IRequirePermission interface
- PermissionAuthorizationBehavior
- Optional pipeline behavior
- Backward compatibility

## Implementation Steps
1. Create IRequirePermission interface
2. Create PermissionAuthorizationBehavior<TRequest,TResponse>
3. Add behavior to pipeline (conditionally)
4. Update sample commands to implement interface
5. Test pipeline authorization works
6. Ensure existing handlers remain unaffected

## Acceptance Criteria
- Commands/Queries can declare required permissions
- Pipeline checks permissions before handler execution
- Behavior is optional (doesn't break existing code)
- Integration with existing AuthorizationBehaviour
- Clear separation between endpoint and CQRS authorization