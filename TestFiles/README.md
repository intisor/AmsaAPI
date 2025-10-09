# TestFiles Directory

This directory contains sample CSV files for testing import functionality.

## Files:

- **members.csv** - Sample member data for testing `/api/import/members/*` endpoints
- **exco.csv** - Sample EXCO data for testing `/api/import/exco/upload` endpoint

## Usage:

These files are referenced in `Import.http` using the syntax:
```
< ./TestFiles/members.csv
```

This allows you to:
1. Keep CSV data in separate files (version controlled)
2. Easily update test data without modifying `.http` files
3. Test with realistic file sizes
4. Reuse the same test data across multiple requests

## Format Requirements:

### members.csv
```
NAME,UNIT,DEPARTMENT,LEVEL,EMAIL,PHONE,MKANID
```

### exco.csv
```
NAME,UNIT,DEPARTMENT
```
