# Microsoft Store release automation

The `store-release` workflow builds the x64/ARM64 Store upload on every push to
`main`. Publishing remains disabled until the one-time Partner Center
configuration below is complete.

## One-time setup

1. In Partner Center, open **Account settings → User management → Microsoft
   Entra applications**.
2. Add or create an Entra application and assign it the **Manager** role.
3. Open that application in Partner Center, select **Add new key**, and copy the
   Tenant ID, Client ID, and key value immediately. The key value is shown only
   once.
4. In the GitHub `microsoft-store` environment, add these secrets:
   - `PARTNER_CENTER_TENANT_ID`
   - `PARTNER_CENTER_SELLER_ID`
   - `PARTNER_CENTER_CLIENT_ID`
   - `PARTNER_CENTER_CLIENT_SECRET`
5. Run **store-release** manually. Enter the same version as the corresponding
   GitHub release, enable **Submit this build to the Microsoft Store**, and
   confirm that Partner Center accepts the submission.
6. Set the repository variable `STORE_PUBLISHING_ENABLED` to `true`.

After step 6, every successful Store build from `main` is submitted
automatically. Microsoft certification and rollout continue asynchronously.

The client secret should be rotated before it expires. Azure CLI is not
required by the release workflow.
