// Smartlead API Documentation Custom JavaScript

window.onload = function() {
    // Add custom header
    const header = document.querySelector('.swagger-ui .topbar');
    if (header) {
        const customNav = document.createElement('div');
        customNav.style.cssText = 'position: absolute; right: 20px; top: 50%; transform: translateY(-50%);';
        customNav.innerHTML = `
            <a href="/" style="color: white; text-decoration: none; margin: 0 10px;">Dashboard</a>
            <a href="/api/v1" style="color: white; text-decoration: none; margin: 0 10px;">API v1</a>
            <a href="https://github.com/smartlead-ai/api-docs" target="_blank" style="color: white; text-decoration: none; margin: 0 10px;">
                <svg width="20" height="20" fill="white" style="vertical-align: middle;">
                    <path d="M10 0C4.477 0 0 4.477 0 10c0 4.42 2.865 8.17 6.84 9.49.5.09.68-.22.68-.48 0-.24-.01-.87-.01-1.71-2.78.6-3.37-1.34-3.37-1.34-.45-1.16-1.11-1.46-1.11-1.46-.91-.62.07-.61.07-.61 1 .07 1.53 1.03 1.53 1.03.89 1.52 2.34 1.08 2.91.83.09-.65.35-1.09.63-1.34-2.22-.25-4.56-1.11-4.56-4.93 0-1.09.39-1.98 1.03-2.68-.1-.25-.45-1.27.1-2.64 0 0 .84-.27 2.75 1.02A9.58 9.58 0 0110 4.84c.85 0 1.7.11 2.5.33 1.91-1.29 2.75-1.02 2.75-1.02.55 1.37.2 2.39.1 2.64.64.7 1.03 1.59 1.03 2.68 0 3.84-2.34 4.68-4.57 4.93.36.31.68.92.68 1.85 0 1.34-.01 2.42-.01 2.75 0 .27.18.58.69.48A10.02 10.02 0 0020 10c0-5.523-4.477-10-10-10z"/>
                </svg>
            </a>
        `;
        header.appendChild(customNav);
    }

    // Add example requests
    const exampleRequests = {
        '/api/v1/campaigns': {
            get: {
                description: 'Retrieve paginated campaigns with filtering',
                curl: `curl -X GET "https://api.smartlead.ai/api/v1/campaigns?page=1&limit=10" \\
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \\
  -H "X-API-Key: YOUR_API_KEY"`,
                response: {
                    success: true,
                    data: [
                        {
                            id: "camp_123",
                            name: "Q4 Outreach Campaign",
                            status: "active",
                            totalSent: 1500,
                            totalOpened: 450,
                            totalReplied: 75,
                            openRate: 30.0,
                            replyRate: 5.0
                        }
                    ],
                    pagination: {
                        page: 1,
                        limit: 10,
                        total: 156,
                        totalPages: 16
                    }
                }
            }
        },
        '/api/webhooks': {
            post: {
                description: 'Create a new webhook subscription',
                curl: `curl -X POST "https://api.smartlead.ai/api/webhooks" \\
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \\
  -H "Content-Type: application/json" \\
  -d '{
    "url": "https://your-app.com/webhook",
    "events": ["campaign.started", "email.opened", "email.replied"],
    "isActive": true
  }'`,
                response: {
                    id: "webhook_456",
                    url: "https://your-app.com/webhook",
                    secret: "whsec_abc123...",
                    events: ["campaign.started", "email.opened", "email.replied"],
                    isActive: true,
                    createdAt: "2024-01-15T10:00:00Z"
                }
            }
        },
        '/api/keys': {
            post: {
                description: 'Generate a new API key',
                curl: `curl -X POST "https://api.smartlead.ai/api/keys" \\
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \\
  -H "Content-Type: application/json" \\
  -d '{
    "name": "Production API Key",
    "permissions": ["campaigns:read", "campaigns:write", "webhooks:manage"],
    "expiresAt": "2025-01-01T00:00:00Z"
  }'`,
                response: {
                    id: "key_789",
                    key: "sk_live_abc123xyz456...",
                    name: "Production API Key",
                    permissions: ["campaigns:read", "campaigns:write", "webhooks:manage"],
                    expiresAt: "2025-01-01T00:00:00Z",
                    createdAt: "2024-01-15T10:00:00Z"
                }
            }
        }
    };

    // Add copy button to code blocks
    setTimeout(() => {
        const codeBlocks = document.querySelectorAll('.swagger-ui pre');
        codeBlocks.forEach(block => {
            const button = document.createElement('button');
            button.innerHTML = '📋 Copy';
            button.style.cssText = 'position: absolute; top: 10px; right: 10px; background: #007BFF; color: white; border: none; padding: 5px 10px; border-radius: 3px; cursor: pointer; font-size: 12px;';
            button.onclick = () => {
                navigator.clipboard.writeText(block.textContent);
                button.innerHTML = '✅ Copied!';
                setTimeout(() => {
                    button.innerHTML = '📋 Copy';
                }, 2000);
            };
            
            const wrapper = document.createElement('div');
            wrapper.style.position = 'relative';
            block.parentNode.insertBefore(wrapper, block);
            wrapper.appendChild(block);
            wrapper.appendChild(button);
        });
    }, 2000);

    // Add rate limit information
    const rateLimitInfo = document.createElement('div');
    rateLimitInfo.className = 'info';
    rateLimitInfo.style.cssText = 'background: #f0f8ff; border: 1px solid #007BFF; border-radius: 4px; padding: 15px; margin: 20px 0;';
    rateLimitInfo.innerHTML = `
        <h3 style="color: #007BFF; margin-top: 0;">📊 Rate Limits</h3>
        <table style="width: 100%; border-collapse: collapse;">
            <thead>
                <tr style="border-bottom: 2px solid #007BFF;">
                    <th style="text-align: left; padding: 8px;">Authentication Type</th>
                    <th style="text-align: left; padding: 8px;">Rate Limit</th>
                    <th style="text-align: left; padding: 8px;">Window</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td style="padding: 8px;">JWT Bearer Token</td>
                    <td style="padding: 8px;">1000 requests</td>
                    <td style="padding: 8px;">Per hour</td>
                </tr>
                <tr style="background: #f8f9fa;">
                    <td style="padding: 8px;">API Key</td>
                    <td style="padding: 8px;">5000 requests</td>
                    <td style="padding: 8px;">Per hour</td>
                </tr>
                <tr>
                    <td style="padding: 8px;">Webhook Deliveries</td>
                    <td style="padding: 8px;">100 events</td>
                    <td style="padding: 8px;">Per minute</td>
                </tr>
            </tbody>
        </table>
        <p style="margin-bottom: 0; margin-top: 10px; color: #666;">
            <strong>Note:</strong> Rate limit headers are included in all responses: 
            <code>X-RateLimit-Limit</code>, <code>X-RateLimit-Remaining</code>, <code>X-RateLimit-Reset</code>
        </p>
    `;

    // Insert rate limit info after main title
    setTimeout(() => {
        const infoContainer = document.querySelector('.swagger-ui .info');
        if (infoContainer && infoContainer.parentNode) {
            infoContainer.parentNode.insertBefore(rateLimitInfo, infoContainer.nextSibling);
        }
    }, 1000);

    // Add webhook events documentation
    const webhookEvents = document.createElement('div');
    webhookEvents.className = 'info';
    webhookEvents.style.cssText = 'background: #f5f0ff; border: 1px solid #6f42c1; border-radius: 4px; padding: 15px; margin: 20px 0;';
    webhookEvents.innerHTML = `
        <h3 style="color: #6f42c1; margin-top: 0;">🔔 Webhook Events</h3>
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 10px;">
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">campaign.created</code> - New campaign created</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">campaign.started</code> - Campaign started</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">campaign.paused</code> - Campaign paused</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">campaign.completed</code> - Campaign completed</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">email.sent</code> - Email sent</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">email.opened</code> - Email opened</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">email.clicked</code> - Link clicked</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">email.replied</code> - Email replied</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">email.bounced</code> - Email bounced</div>
            <div><code style="background: #e9ecef; padding: 2px 6px; border-radius: 3px;">lead.unsubscribed</code> - Lead unsubscribed</div>
        </div>
        <p style="margin-bottom: 0; margin-top: 10px; color: #666;">
            <strong>Signature Verification:</strong> All webhook payloads include an HMAC-SHA256 signature in the 
            <code>X-Webhook-Signature</code> header for security verification.
        </p>
    `;

    setTimeout(() => {
        const infoContainer = document.querySelector('.swagger-ui .info');
        if (infoContainer && infoContainer.parentNode) {
            infoContainer.parentNode.appendChild(webhookEvents);
        }
    }, 1500);

    // Add authentication guide
    const authGuide = document.createElement('div');
    authGuide.className = 'info';
    authGuide.style.cssText = 'background: #f0fff0; border: 1px solid #28a745; border-radius: 4px; padding: 15px; margin: 20px 0;';
    authGuide.innerHTML = `
        <h3 style="color: #28a745; margin-top: 0;">🔐 Authentication Guide</h3>
        <div style="margin-bottom: 15px;">
            <h4>JWT Bearer Token (Web Applications)</h4>
            <pre style="background: #1a1a2e; color: #f8f9fa; padding: 10px; border-radius: 4px; overflow-x: auto;">
// Login to get tokens
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "password123"
}

// Use access token in requests
GET /api/v1/campaigns
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...</pre>
        </div>
        <div>
            <h4>API Key (Server-to-Server)</h4>
            <pre style="background: #1a1a2e; color: #f8f9fa; padding: 10px; border-radius: 4px; overflow-x: auto;">
// Generate API key from dashboard or API
POST /api/keys
Authorization: Bearer YOUR_JWT_TOKEN

// Use API key in requests
GET /api/v1/campaigns
X-API-Key: sk_live_abc123xyz456...</pre>
        </div>
    `;

    setTimeout(() => {
        const infoContainer = document.querySelector('.swagger-ui .info');
        if (infoContainer && infoContainer.parentNode) {
            infoContainer.parentNode.appendChild(authGuide);
        }
    }, 2000);

    // Track API calls for analytics
    let apiCallCount = 0;
    const originalFetch = window.fetch;
    window.fetch = function(...args) {
        apiCallCount++;
        console.log(`API Call #${apiCallCount}:`, args[0]);
        return originalFetch.apply(this, args);
    };

    // Add search functionality enhancement
    const searchBox = document.querySelector('.swagger-ui .filter input');
    if (searchBox) {
        searchBox.placeholder = 'Search endpoints, models, or operations...';
        searchBox.style.width = '300px';
    }

    // Add keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Ctrl/Cmd + K to focus search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            const searchBox = document.querySelector('.swagger-ui .filter input');
            if (searchBox) searchBox.focus();
        }
        
        // Ctrl/Cmd + / to toggle all operations
        if ((e.ctrlKey || e.metaKey) && e.key === '/') {
            e.preventDefault();
            const expandButtons = document.querySelectorAll('.swagger-ui .expand-operation');
            expandButtons.forEach(btn => btn.click());
        }
    });

    console.log('🚀 Smartlead API Documentation loaded successfully!');
};