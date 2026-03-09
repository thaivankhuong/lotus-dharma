# LD-0017: Permission Attribute

## Title
Create Permission Attribute for Declarative Authorization

## Goal
Provide easy-to-use attribute for permission-based authorization

## Scope
- PermissionAttribute class
- Integration with existing AuthorizeAttribute
- Support for multiple permissions
- Backward compatibility

## Implementation Steps
1. Create PermissionAttribute class
2. Extend existing authorization behavior to handle permissions
3. Add support for comma-separated permission list
4. Update AuthorizationBehaviour to process permission attributes
5. Test attribute usage on endpoints

## Acceptance Criteria
- [Permission("Users.View")] works on endpoints
- Multiple permissions supported: [Permission("Users.View,Users.Create")]
- Attribute integrates with existing authorization
- Clear error messages for unauthorized access
- No breaking changes to existing code