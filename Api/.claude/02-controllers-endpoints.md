# API Controllers and Endpoints

## Overview
19 controllers with 200+ endpoints for campaign management, email account tracking, webhook management, analytics, and authentication.

**Authentication Schemes:**
- **JWT**: Web application clients (access + refresh tokens)
- **API Key**: External integrations with permission-based access

---

## V1 APIs (API Key Authentication)

### CampaignsV1Controller
**File:** [Core/Controllers/V1/CampaignsV1Controller.cs](../Core/Controllers/V1/CampaignsV1Controller.cs)
**Route:** `api/v1/campaigns`
**Auth:** API Key
**Permissions:** ReadCampaigns, WriteCampaigns, AdminAll

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Get paginated campaigns (max 100/page) with metrics |
| GET | `/{id:int}` | Get campaign by ID |
| GET | `/{id:int}/stats` | Daily stats with date range filtering |
| GET | `/{id:int}/email-accounts` | Email accounts for campaign |
| GET | `/{id:int}/lead-history` | Lead conversations (max 50/page, filter replies) |
| GET | `/{id:int}/leads/{leadId}/history` | Complete email thread for lead |
| GET | `/{id:int}/templates` | Email templates in sequence order |
| POST | `/filtered` | Filter campaigns by client IDs (large lists) |
| POST | `/` | Create campaign |
| PUT | `/{id:int}/sequence` | Configure email sequences |
| POST | `/{id:int}/leads` | Upload leads (max 100/request) |

**Notes:** Hardcoded Smartlead client ID 3138, max 1 year time-range filtering

### EmailAccountsV1Controller
**File:** [Core/Controllers/V1/EmailAccountsV1Controller.cs](../Core/Controllers/V1/EmailAccountsV1Controller.cs)
**Route:** `api/v1/email-accounts`
**Auth:** API Key

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Paginated email accounts (max 100/page) |
| GET | `/{id}` | Email account details |
| GET | `/summary` | Total count + status breakdown |
| GET | `/{id}/stats` | Daily stats with date range |
| GET | `/{id}/warmup` | Warmup metrics |
| GET | `/{id}/warmup/daily-stats` | Daily warmup breakdown |
| GET | `/{id}/health` | Health metrics (default 7 days) |
| GET | `/{id}/campaigns` | Campaigns using this account |

**Notes:** Filters by assigned clients for non-admin users, max 1 year date range

### WebhooksV1Controller
**File:** [Core/Controllers/V1/WebhooksV1Controller.cs](../Core/Controllers/V1/WebhooksV1Controller.cs)
**Route:** `api/v1/webhooks`
**Auth:** API Key
**Permissions:** ReadWebhooks, AdminAll

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | All webhooks for user |
| GET | `/{id}` | Webhook details |
| GET | `/{id}/deliveries` | Delivery history (max 100) |

### ClientsV1Controller
**File:** [Core/Controllers/V1/ClientsV1Controller.cs](../Core/Controllers/V1/ClientsV1Controller.cs)
**Route:** `api/v1/clients`
**Auth:** API Key (Admin only)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Paginated clients (max 100/page) with campaign/email counts |
| GET | `/{id}` | Client details |
| POST | `/` | Create client (name required) |
| PUT | `/{id}` | Update client |
| DELETE | `/{id}` | Delete client (no campaigns/emails attached) |
| GET | `/search` | Quick search (max 50) |
| GET | `/{id}/campaigns` | Client's campaigns (paginated) |
| GET | `/{id}/email-accounts` | Client's email accounts (paginated) |
| POST | `/{id}/assign-campaigns` | Bulk assign campaigns |
| POST | `/{id}/assign-email-accounts` | Bulk assign email accounts |
| GET | `/stats` | Client statistics with reply timing |
| POST | `/stats` | Client stats (POST for large filters) |
| GET | `/{id}/stats` | Single client stats |

**Notes:** Auto-generates color from palette, validates no orphaned data before deletion

### UsersV1Controller
**File:** [Core/Controllers/V1/UsersV1Controller.cs](../Core/Controllers/V1/UsersV1Controller.cs)
**Route:** `api/v1/users`
**Auth:** API Key (Admin only)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/assign-clients` | Assign clients to user (non-admin only) |
| GET | `/{userId}/clients` | User info with client assignments |
| DELETE | `/{userId}/clients` | Remove all client assignments |
| POST | `/stats` | User statistics and analytics |

**Notes:** Cannot assign clients to admins, cannot modify own assignments

### MasterInboxV1Controller
**File:** [Core/Controllers/V1/MasterInboxV1Controller.cs](../Core/Controllers/V1/MasterInboxV1Controller.cs)
**Route:** `api/v1/campaigns`
**Auth:** API Key

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/inbox-replies` | Get replied emails (max 20, removes Smartlead branding) |

---

## Campaign Management

### CampaignController
**File:** [Core/Controllers/Campaign/CampaignController.cs](../Core/Controllers/Campaign/CampaignController.cs)
**Route:** `api/campaigns`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Paginated campaigns with advanced filtering |
| POST | `/filter` | Filter campaigns (POST for large lists) |
| GET | `/{id}` | Campaign details |
| POST | `/` | Create campaign (admin) |
| PUT | `/{id}` | Update campaign (admin) |
| DELETE | `/{id}` | Delete campaign (admin) |
| POST | `/bulk-delete` | Bulk delete (admin) |
| GET | `/search` | Search campaigns |
| GET | `/download` | Export campaigns as CSV |
| POST | `/{id}/tags` | Assign tags (admin) |
| DELETE | `/{id}/tags/{tagName}` | Remove tag (admin) |
| GET | `/by-campaign-id/{campaignId}` | Get by numeric ID with daily stats |
| GET | `/{campaignId}/positive-replies` | Positive replies (placeholder) |
| GET | `/{id}/daily-stats` | Daily statistics |
| GET | `/{campaignId}/analytics` | Campaign analytics |
| PUT | `/bulk-assign-client` | Bulk client assignment (admin) |
| PUT | `/{id}/notes` | Update notes (admin) |
| GET | `/{id}/email-accounts` | Email accounts for campaign |
| GET | `/list` | Campaign dropdown list |
| GET | `/{id}/lead-conversations` | Lead conversations with email history |

**Filters:** search, sortBy/Direction/Mode, clientIds, timeRangeDays, emailAccountId, performanceFilterMinSent/MaxReplyRate

### EmailAccountController
**File:** [Core/Controllers/Campaign/EmailAccountController.cs](../Core/Controllers/Campaign/EmailAccountController.cs)
**Route:** `api/email-accounts`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Paginated email accounts with advanced filtering |
| GET | `/by-id/{id:long}` | Get by ID |
| GET | `/{email}` | Get by email address |
| POST | `/filter` | Filter (POST for large lists) |
| POST | `/assign-client` | Assign client (admin) |
| GET | `/{id}/warmup-metrics` | Warmup metrics with daily stats |
| GET | `/{id}/daily-stats` | Daily stats breakdown |
| POST | `/{id}/warmup-metrics/refresh` | Refresh metrics (placeholder) |
| GET | `/by-client/{clientId}` | Email accounts for client |
| GET | `/download` | Export as CSV |
| POST | `/{id}/tags` | Assign tags (admin) |
| DELETE | `/{id}/tags/{tagName}` | Remove tag (admin) |
| PUT | `/{id}/notes` | Update notes (admin) |
| GET | `/{id}/campaigns` | Campaigns using account |

**Filters:** search, sortBy/Direction/Mode, campaignId, timeRangeDays, minSent, warmupStatus, performanceFilterMinSent/MaxReplyRate, clientIds, userIds

### ClientController
**File:** [Core/Controllers/Campaign/ClientController.cs](../Core/Controllers/Campaign/ClientController.cs)
**Route:** `api/clients`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Paginated clients with sorting |
| GET | `/{id}` | Client details |
| POST | `/` | Create client (admin) |
| PUT | `/{id}` | Update client (admin) |
| DELETE | `/{id}` | Delete client (admin) |
| GET | `/search` | Quick search |
| GET | `/{id}/campaigns` | Client's campaigns |
| GET | `/{id}/email-accounts` | Client's email accounts |
| POST | `/assign-color` | Auto-assign colors |
| GET | `/dropdown` | Simplified dropdown list |
| GET | `/list` | List with pagination |

**Filters:** search, sortBy/Direction/Mode, filterByUserId

---

## Analytics & Dashboard

### DashboardController
**File:** [Core/Controllers/Analytics/DashboardController.cs](../Core/Controllers/Analytics/DashboardController.cs)
**Route:** `api/dashboard`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/overview` | Comprehensive dashboard (5min cache) |
| POST | `/filtered-overview` | Filtered dashboard (POST) |
| GET | `/performance-trend` | Performance trend charts |
| POST | `/filtered-performance-trend` | Filtered trend (POST) |
| GET | `/campaign-performance-trend` | Campaign trend |
| POST | `/filtered-campaign-performance-trend` | Filtered campaign trend |
| GET | `/top-campaigns` | Top campaigns (default 10) |
| POST | `/filtered-top-campaigns` | Filtered top campaigns (POST) |
| GET | `/filtered-top-campaigns` | Filtered top campaigns (GET) |
| GET | `/top-clients` | Top clients (default 10) |
| GET | `/email-accounts-summary` | Email account health summary |
| GET | `/recent-activities` | Recent activities (default 20) |
| POST | `/stats` | Stats with custom date range |
| GET | `/realtime` | Real-time metrics (auto-refresh) |

**Filters:** minimumSent, sortBy, sortDescending, period, useCompositeScore, minimumReplyRate, maxBounceRate, statuses, timeRangeDays, clientIds, campaignIds

**Notes:** `allCampaigns=true` requires admin (system-wide data)

### AnalyticsController
**File:** [Core/Controllers/Analytics/AnalyticsController.cs](../Core/Controllers/Analytics/AnalyticsController.cs)
**Route:** `api/analytics`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/dashboard` | Analytics dashboard (5min cache) |
| POST | `/dashboard/filtered-overview` | Filtered analytics |
| GET | `/dashboard/performance-trend` | Performance trend |
| POST | `/dashboard/filtered-performance-trend` | Filtered trend |
| GET | `/dashboard/campaign-performance-trend` | Campaign trend |
| POST | `/dashboard/filtered-campaign-performance-trend` | Filtered campaign trend |
| GET | `/performance-trends` | Performance trends |
| GET | `/email-account-performance` | Email account performance |
| GET | `/client-comparison` | Client comparison |

**Periods:** 7d, 30d, 90d, all

---

## Authentication & Users

### AuthController
**File:** [Core/Controllers/Authentication/AuthController.cs](../Core/Controllers/Authentication/AuthController.cs)
**Route:** `api/auth`
**Auth:** Public (login/refresh), JWT (others)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/login` | Authenticate (5 attempts/15min rate limit) |
| POST | `/refresh` | Refresh access token |
| POST | `/logout` | Revoke refresh token |
| GET | `/me` | Current user info |
| GET | `/sessions` | Active sessions |
| DELETE | `/sessions/{sessionId}` | Revoke session |
| POST | `/logout-all` | Revoke all sessions |
| GET | `/user-sessions/{userId}` | User sessions (admin) |
| DELETE | `/admin/users/{userId}/sessions/{sessionId}` | Revoke user session (admin) |
| DELETE | `/admin/users/{userId}/sessions` | Revoke all user sessions (admin) |

**Rate Limit:** 5 attempts per IP+email per 15 minutes

### UsersController
**File:** [Core/Controllers/Authentication/UsersController.cs](../Core/Controllers/Authentication/UsersController.cs)
**Route:** `api/users`
**Auth:** JWT (Admin for most)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | All users (admin) |
| GET | `/{id}` | User details (admin) |
| POST | `/` | Create user (admin) |
| PUT | `/{id}` | Update user (admin) |
| DELETE | `/{id}` | Delete user (admin) |
| PUT | `/{id}/clients` | Update client assignments (admin) |
| POST | `/change-password` | Change own password |
| POST | `/generate-api-key` | Generate API key |
| POST | `/{id}/reset-password` | Reset user password (admin) |
| GET | `/list` | User dropdown list |
| GET | `/{id}/clients` | User's assigned clients |

**Protected:** Cannot modify admins or self (prevents lockout)

### ApiKeysController
**File:** [Core/Controllers/Authentication/ApiKeysController.cs](../Core/Controllers/Authentication/ApiKeysController.cs)
**Route:** `api/apikeys`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | All API keys for user |
| GET | `/{id}` | API key details |
| POST | `/` | Create API key |
| PUT | `/{id}` | Update API key |
| DELETE | `/{id}` | Revoke API key |
| GET | `/{id}/usage` | Usage statistics (max 100) |
| GET | `/permissions` | Available permissions |

**Permissions:** ReadCampaigns, WriteCampaigns, ReadEmailAccounts, WriteEmailAccounts, ReadWebhooks, WriteWebhooks, DeleteWebhooks, AdminAll

**Create Params:** name, description, permissions[], rateLimit, expiresAt, ipWhitelist[]

---

## Webhooks & Events

### WebhooksController
**File:** [Core/Controllers/ExternalApi/WebhooksController.cs](../Core/Controllers/ExternalApi/WebhooksController.cs)
**Route:** `api/webhooks`
**Auth:** JWT or API Key
**Permissions:** ReadWebhooks, WriteWebhooks, DeleteWebhooks, AdminAll

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | All webhooks |
| GET | `/{id}` | Webhook details |
| POST | `/` | Create webhook |
| PUT | `/{id}` | Update webhook |
| DELETE | `/{id}` | Delete webhook |
| GET | `/{id}/deliveries` | Delivery history (page/pageSize or limit/offset) |
| POST | `/{id}/test` | Send test webhook |

**Params:** failuresOnly filter for deliveries

### WebhookEventsController
**File:** [Core/Controllers/ExternalApi/WebhookEventsController.cs](../Core/Controllers/ExternalApi/WebhookEventsController.cs)
**Route:** `api/webhook-events`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | All event configs |
| GET | `/{id}` | Event config details |
| POST | `/` | Create event config |
| PUT | `/{id}` | Update event config |
| DELETE | `/{id}` | Delete event config |
| GET | `/{id}/triggers` | Event trigger history (max 100) |
| GET | `/triggers/recent` | Recent triggers (max 50) |
| GET | `/event-types` | Available event types |

**Event Types:**
- `reply_rate_drop` - Reply rate below threshold
- `bounce_rate_high` - Bounce rate above threshold
- `no_positive_reply_for_x_days` - No positive replies in X days
- `no_reply_for_x_days` - No replies in X days
- `campaign.created` - Campaign created (admin only)

---

## System

### StatusController
**File:** [Core/Controllers/System/StatusController.cs](../Core/Controllers/System/StatusController.cs)
**Route:** `/status`
**Auth:** Public

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | API health check |

### AccountsController
**File:** [Core/Controllers/System/AccountsController.cs](../Core/Controllers/System/AccountsController.cs)
**Route:** `api/accounts`
**Auth:** JWT

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/` | Admin accounts (placeholder) |
| GET | `/{accountId}/email-accounts` | Email accounts (placeholder) |
| POST | `/` | Add account (placeholder) |

**Note:** Placeholder implementation, not fully built

---

## Response Types

### PaginatedResponse
```json
{
  "data": [],
  "totalCount": 0,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 0,
  "hasPrevious": false,
  "hasNext": false
}
```

### ApiResponse
```json
{
  "success": true,
  "data": {},
  "message": "string",
  "errorCode": null
}
```

---

## Role-Based Access

### Admin Users
- Full CRUD on all resources
- See all clients/campaigns/email accounts
- Manage users and assignments
- System-wide analytics

### Regular Users
- Filtered by assigned client IDs
- Read-only access to campaigns/email accounts
- Manage own API keys and sessions
- Cannot see unassigned clients

---

## Common Patterns

### Pagination
- Query params: `page` (1-indexed), `pageSize` or `limit`
- Response: PaginatedResponse with navigation metadata

### Sorting
- `sortBy`: column name
- `sortDirection`: asc/desc
- `sortMode`: optional multi-column sorting

### Filtering
- Time ranges: `timeRangeDays`, `startDate`, `endDate`
- Client/User filtering: `clientIds[]`, `userIds[]`, `filterByClientIds[]`
- Performance: `minSent`, `performanceFilterMinSent`, `performanceFilterMaxReplyRate`

### CSV Export
- GET with query params for filters
- Returns CSV file with comprehensive metrics
- Used by: CampaignController, EmailAccountController
