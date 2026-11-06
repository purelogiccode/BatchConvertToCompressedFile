# Donation

Batch Convert to Compressed File is donation-supported. The donation banner is visible until a
completed donation is verified.

## User flow

1. Click **Donate** in the banner (or **Help -> Donate**).
2. The browser opens:

   ```
   https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file&email=<optional>
   ```

3. Complete the donation with the same email address you will enter in the app.
4. Enter that email in the banner and click **Verify donation**.
5. If the API confirms a completed donation, the banner is hidden permanently for the current
   user and `donation.json` is written.

The banner intentionally has **no close button**. It goes away only after verification.

## Verification API

```
POST https://www.purelogiccode.com/api/donations/verify
X-API-KEY: <application key>
Content-Type: application/json

{
  "email": "donor@example.com",
  "applicationSlug": "batch-convert-to-compressed-file"
}
```

Successful response:

```json
{
  "donated": true,
  "applicationSlug": "batch-convert-to-compressed-file",
  "totalAmount": 10.0,
  "lastDonationAtUtc": "2026-11-05T12:00:00Z"
}
```

The server verifies the email against completed donations for the application. A local file is
only a cache of a successful response; deleting it shows the banner again until verification is
repeated.

## Server registration

The verification endpoint only accepts application slugs registered as donationware on the
Pure Logic Code site. The slug `batch-convert-to-compressed-file` is present in the website product
catalog and deployed in production.

The live API returns `200` with `donated:false` for an email without a completed donation:

```powershell
POST https://www.purelogiccode.com/api/donations/verify
X-API-KEY: <application key>

{"email":"nobody@example.com","applicationSlug":"batch-convert-to-compressed-file"}
```

If the slug is missing from a future deployment, the API returns `404` and the app shows:

> Donation verification is not available for this application yet. Please try again later.

## Privacy

- Only the email address is sent to the verification API.
- The response includes the total donated amount and the last donation timestamp.
- The email is stored locally in `donation.json` after successful verification.

## Security notes

- Verification is server-side; editing `donation.json` does not create a donation record.
- The API key identifies the application, not the user.
- No payment data is handled by the desktop application.
