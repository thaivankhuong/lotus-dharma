# LD-0016: Permission Policy Handler

## Title
Implement Permission-Based Authorization Policy

## Goal
Create ASP.NET Core authorization policy for permission checks

## Scope
- PermissionRequirement class
- PermissionAuthorizationHandler
- Policy registration in DI container
- Integration with existing authorization system

## Implementation Steps
1. Create PermissionRequirement class
2. Create PermissionAuthorizationHandler
3. Register policy in Web/Program.cs or DI configuration
4. Update IIdentityService interface to support permission checks
5. Implement permission resolution logic

## Acceptance Criteria
- [Permission("Module.Action")] attribute works
- Permission checks integrate with existing authorization
- Handler resolves user permissions correctly
- Policy registration works without conflicts
- Existing role-based authorization still functions