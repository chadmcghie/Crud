# Database Schema

This is the database schema implementation for the spec detailed in [spec.md](../spec.md)

## Schema Changes

No schema structure changes are required. This spec focuses on data management, not schema modifications.

## Database Operations

### Reset Strategy
- **Option 1: Truncate Tables** - Clear all data from existing tables while preserving schema
- **Option 2: Transaction Rollback** - Not viable with SQLite across processes
- **Option 3: File Replacement** - Not viable while API holds connection

### Implementation Approach

#### Truncate Operation Sequence
1. Disable foreign key constraints temporarily
2. Truncate tables in dependency order:
   - Independent tables first (Roles, Walls, Windows)
   - Dependent tables last (People with foreign keys)
3. Re-enable foreign key constraints
4. Verify data cleanup completion

### Performance Considerations
- Use batch operations to minimize round trips
- Consider keeping lookup/reference data if needed
- Implement connection pooling for reset operations

### Data Integrity Rules
- Ensure complete data removal between test runs
- Maintain referential integrity after reset
- Preserve auto-increment sequences if needed
- No production data should ever be accessible to test reset mechanisms