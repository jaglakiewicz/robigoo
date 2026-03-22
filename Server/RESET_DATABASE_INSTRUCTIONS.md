# Database Reset Instructions

## How to Reset the Database to Clean State

### Step 1: Stop the Application
Make sure the server application is completely stopped. The database files cannot be deleted while the application is running.

### Step 2: Run the Reset Script
Open PowerShell in the `Server` directory and run:

```powershell
.\reset-database.ps1
```

When prompted, type `YES` to confirm deletion.

### Step 3: Start the Application
Start the server application. It will automatically create a fresh database with:
- Only the master admin user
- No clients
- No crop sprayers
- No inspections
- Default settings

### Default Login Credentials
After reset, use these credentials to log in:
- **Username:** `admin`
- **Password:** `admin`

## What Gets Deleted
- All users (except master admin which is recreated)
- All clients
- All crop sprayers
- All inspections
- All inspection protocols
- All control marks
- All history/audit logs
- All settings (reset to defaults)

## Alternative: Manual Reset
If the script doesn't work, you can manually delete these files while the application is stopped:
1. `app_v2.db`
2. `app_v2.db-wal`
3. `app_v2.db-shm`

Then start the application to recreate the database.
