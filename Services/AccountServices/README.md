# Account Services - SOLID Architecture

This folder contains the refactored account services following SOLID principles. Each service has a single responsibility and is organized into focused modules.

## Folder Structure

```
Services/AccountServices/
├── Authentication/           # Login, Logout, Token Refresh
│   ├── IAuthenticationService.cs
│   └── AuthenticationService.cs
├── Registration/             # User Registration & Email Confirmation
│   ├── IRegistrationService.cs
│   └── RegistrationService.cs
├── Password/                 # Password Management
│   ├── IPasswordService.cs
│   └── PasswordService.cs
├── Profile/                  # Profile Management
│   ├── IProfileService.cs
│   └── ProfileService.cs
├── AccountManagement/        # Account Deletion
│   ├── IAccountManagementService.cs
│   └── AccountManagementService.cs
├── Shared/                   # Shared Interfaces & Utilities
│   ├── IAccountServices.cs   # Legacy interface (for backward compatibility)
│   └── AccountLinkBulider.cs
└── AccountServices.cs        # Legacy service (to be removed)
```

## SOLID Principles Applied

### 1. **Single Responsibility Principle (SRP)**
Each service has one specific responsibility:
- **AuthenticationService**: Handles login, logout, and token refresh
- **RegistrationService**: Manages user registration and email confirmation
- **PasswordService**: Handles password changes and resets
- **ProfileService**: Manages profile updates and photo uploads
- **AccountManagementService**: Handles account deletion

### 2. **Open/Closed Principle (OCP)**
- Services are open for extension through interfaces
- New implementations can be added without modifying existing code

### 3. **Liskov Substitution Principle (LSP)**
- All services implement their respective interfaces
- Can be substituted without breaking functionality

### 4. **Interface Segregation Principle (ISP)**
- Each interface contains only relevant methods
- Clients don't depend on methods they don't use

### 5. **Dependency Inversion Principle (DIP)**
- All dependencies are injected via interfaces
- High-level modules don't depend on low-level modules

## Service Responsibilities

### Authentication Service
- `LoginAsync()` - User authentication with lockout protection
- `LogoutAsync()` - User logout with token cleanup
- `RefreshTokenAsync()` - Token refresh functionality

### Registration Service
- `RegisterAsync()` - New user registration
- `ConfirmEmailAsync()` - Email confirmation
- `ResendConfirmationEmailAsync()` - Resend confirmation emails

### Password Service
- `ChangePasswordAsync()` - Password change for authenticated users
- `RequestPasswordResetAsync()` - Initiate password reset
- `ResetPasswordAsync()` - Complete password reset process

### Profile Service
- `ChangeEmailAsync()` - Email address changes
- `UploadPhotoAsync()` - Profile photo upload and management

### Account Management Service
- `DeleteAsync()` - Soft delete user accounts

## Background Processing

All services utilize Hangfire for background job processing:
- Email sending operations
- Token cleanup operations
- Image processing operations
- Operation logging

## Migration Notes

### Legacy Support
- `IAccountServices` interface remains in Shared folder for backward compatibility
- `AccountServices.cs` can be removed once all controllers are updated

### Next Steps
1. Update controllers to use specific service interfaces
2. Register new services in DI container
3. Remove legacy `AccountServices.cs` file
4. Update any remaining references

## Benefits

1. **Maintainability**: Each service is focused and easier to maintain
2. **Testability**: Services can be unit tested independently
3. **Scalability**: Services can be scaled independently
4. **Flexibility**: Easy to add new features or modify existing ones
5. **Code Reuse**: Services can be reused across different parts of the application 