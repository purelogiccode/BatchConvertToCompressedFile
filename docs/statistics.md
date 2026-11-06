# Statistics

The application records one anonymous usage hit at startup.

## Endpoint

```
POST https://www.purelogiccode.com/ApplicationStats/stats
Authorization: Bearer <application key>
Content-Type: application/json

{
  "applicationId": "batchconverttocompressedfile",
  "version": "2.0.0"
}
```

The API is rate-limited per IP address and application. The app treats `429` or any non-success
response as an expected condition and continues normally.

## What is recorded

The Pure Logic Code statistics service records application metadata, daily/monthly call counts,
and country-level aggregates derived from the caller IP. The desktop app sends only:

- application ID
- application version

No file names, archive contents, user identity, or machine name are sent.

## Failure behavior

Statistics failures never interrupt startup and never produce a bug report:

- offline / DNS / TLS failures are logged at Information
- rate limits and WAF responses are logged at Information
- unexpected exceptions are logged at Error

## Configuration

| Item | Value |
| --- | --- |
| Endpoint | `https://www.purelogiccode.com/ApplicationStats/stats` |
| Application ID | `batchconverttocompressedfile` |
| Bearer token override | `APP_STATS_API_KEY` |
| Timeout | 20 seconds |
