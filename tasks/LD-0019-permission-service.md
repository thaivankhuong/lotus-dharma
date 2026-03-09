# LD-0019: Permission Service

## Title
Create Permission Service for Resolution and Caching

## Goal
Implement service to resolve user permissions efficiently

## Scope
- IPermissionService interface
- PermissionService implementation
- Permission resolution logic
- User permission aggregation

## Implementation Steps
1. Create IPermissionService interface
2. Implement PermissionService in Infrastructure
3. Add methods: GetUserPermissions, CheckPermission, etc.
4. Implement role + direct permission aggregation
5. Register service in DI container
6. Update IIdentityService to use PermissionService

## Acceptance Criteria
- Service resolves user permissions correctly
- Combines role permissions + direct permissions
- Handles permission inheritance properly
- Service is injectable and testable
- Performance is acceptable for endpoint usage