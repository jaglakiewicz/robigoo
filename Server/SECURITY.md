# Security Configuration - Robigoo

## Overview

This document describes the security measures implemented in the Robigoo application.

## Authentication & Authorization

### JWT Tokens
- **Access Token**: Short-lived (30 minutes by default), used for API authentication
- **Refresh Token**: Long-lived (7 days by default), used to obtain new access tokens
- **Algorithm**: HMAC-SHA256
- **Validation**: Issuer, Audience, Signature, and Lifetime are all validated

### Password Security
- **Algorithm**: BCrypt with work factor 12
- **Legacy Support**: SHA256 hashes are automatically upgraded on login
- **Password Requirements**:
  - Minimum 8 characters
  - At least one uppercase letter
  - At least one lowercase letter
  - At least one digit

### Session Management
- Sessions are tracked in database
- Single active session per user (with force takeover option)
- Session activity is logged with IP and User-Agent
- Automatic session invalidation on logout

## Security Headers

The following security headers are automatically added to all responses:

| Header | Value | Purpose |
|--------|-------|---------|
| Content-Security-Policy | default-src 'self'; ... | Prevents XSS and injection attacks |
| X-Content-Type-Options | nosniff | Prevents MIME type sniffing |
| X-Frame-Options | DENY | Prevents clickjacking |
| X-XSS-Protection | 1; mode=block | Legacy XSS protection |
| Referrer-Policy | strict-origin-when-cross-origin | Controls referrer information |
| Permissions-Policy | geolocation=(), microphone=(), camera=() | Restricts browser features |

## Rate Limiting

### Login Endpoint
- **Limit**: 5 attempts per minute per IP
- **Lockout**: Automatic after exceeding limit

### General API
- **Limit**: 100 requests per minute per IP
- **Global Limit**: 200 requests per minute per IP

## Audit Logging

### Login Attempts
All login attempts (successful and failed) are logged with:
- Username
- IP Address
- User-Agent
- Timestamp
- Success/Failure status
- Failure reason (if applicable)

### Data Changes
All entity changes are logged in the ChangeLog table with:
- Entity name and ID
- User who made the change
- Timestamp
- Description of changes

## Configuration

### appsettings.json

```json
{
  "Jwt": {
    "Key": "YOUR_256_BIT_SECRET_KEY",
    "Issuer": "Robigoo",
    "Audience": "RobigooUsers",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  },
  "Security": {
    "AllowedOrigins": ["https://your-domain.com"],
    "PasswordMinLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialChar": false
  },
  "RateLimiting": {
    "LoginPermitLimit": 5,
    "LoginWindowMinutes": 1,
    "GeneralPermitLimit": 100,
    "GeneralWindowMinutes": 1
  }
}
```

### Environment Variables (Production)

For production, set sensitive values via environment variables:

```bash
Jwt__Key=your-secure-256-bit-key-here
Security__AllowedOrigins__0=https://your-production-domain.com
```

## CORS Configuration

- **Development**: Permissive (AllowAnyOrigin)
- **Production**: Restricted to configured origins only

## Best Practices for Deployment

1. **Change the JWT Key**: Generate a new 256-bit key for production
2. **Configure CORS**: Set specific allowed origins
3. **Use HTTPS**: Always use HTTPS in production
4. **Review Rate Limits**: Adjust based on expected traffic
5. **Monitor Logs**: Regularly review login attempts and audit logs
6. **Database Backups**: Implement regular backup strategy
7. **Update Dependencies**: Keep all packages up to date

## Generating a Secure JWT Key

```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))

# Linux/Mac
openssl rand -base64 32
```
