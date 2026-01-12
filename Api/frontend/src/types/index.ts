// Common types used across the application

// Removed TagData interface - using simple string arrays for tags

export interface Campaign {
  id: string;
  campaignId: number;
  name: string;
  clientId?: string;
  clientName?: string;
  clientColor?: string;
  totalLeads: number;
  totalSent: number;
  totalOpened: number;
  totalReplied: number;
  totalPositiveReplies: number;
  totalBounced: number;
  totalClicked: number;
  emailIds: number[];
  // 24h stats
  sent24Hours?: number;
  opened24Hours?: number;
  replied24Hours?: number;
  clicked24Hours?: number;
  // 7d stats
  sent7Days?: number;
  opened7Days?: number;
  replied7Days?: number;
  clicked7Days?: number;
  status: 'Active' | 'Paused' | 'Completed' | 'Draft';
  tags?: string[];
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  lastUpdatedAt?: string;
}

export interface EmailAccount {
  id: string;
  email: string;
  name?: string;
  status: 'Active' | 'Inactive' | 'Warming';
  clientId?: string;
  clientName?: string;
  clientColor?: string;
  sent: number;
  opened: number;
  replied: number;
  positiveReplies: number;
  bounced: number;
  // Time-based dictionary stats (date string -> count)
  sentEmails: { [date: string]: number };
  openedEmails: { [date: string]: number };
  repliedEmails: { [date: string]: number };
  bouncedEmails: { [date: string]: number };
  warmupSent: number;
  warmupReplied: number;
  warmupSpamCount: number;
  warmupSavedFromSpam: number;
  campaignCount: number;
  activeCampaignCount: number;
  isSendingActualEmails: boolean | null;  // true = active, false = warmup only, null = inactive
  tags?: string[];
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  lastUpdatedAt?: string;
}

export interface Client {
  id: string;
  name: string;
  email?: string;
  company?: string;
  status: 'Active' | 'Inactive';
  color: string;
  notes?: string;
  campaignCount?: number;
  emailAccountCount?: number;
  createdAt: string;
  updatedAt?: string;
}

export interface ClientListItem {
  id: string;
  name: string;
}

export interface UserListItem {
  id: string;
  name: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: string;
  isActive: boolean;
  assignedClientIds: string[];
}

export interface CampaignListItem {
  id: string;
  name: string;
}

export interface SortConfig {
  column: string;
  direction: 'asc' | 'desc';
  mode: 'count' | 'percentage';
}

export interface MultiSortItem {
  column: string;
  direction: 'asc' | 'desc';
  mode: 'count' | 'percentage';
}

export interface MultiSortConfig {
  sorts: MultiSortItem[];
}

export interface ColumnDefinition {
  label: string;
  sortable: boolean;
  dualSort?: boolean;
  required?: boolean;
  description?: string;
}

export interface TableState {
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  searchQuery: string;
  sort: SortConfig; // Keep for backward compatibility
  multiSort: MultiSortConfig;
  selectedItems: Set<string>;
}

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  duration?: number;
}

export interface CreateCampaignRequest {
  name: string;
  clientId?: string;
}

export interface CreateClientRequest {
  name: string;
  email?: string;
  company?: string;
}

export interface UpdateCampaignRequest {
  name?: string;
  clientId?: string;
  status?: string;
}

export interface UpdateClientRequest {
  name?: string;
  email?: string;
  company?: string;
  status?: string;
}

// Dashboard Types
export interface DashboardOverview {
  stats: OverviewStats;
  topCampaigns: CampaignPerformanceMetric[];
  topClients: ClientPerformanceMetric[];
  performanceTrend: TimeSeriesDataPoint[];
  emailAccountSummary: EmailAccountSummary;
  recentActivities: RecentActivity[];
}

export interface OverviewStats {
  totalEmailAccounts: number;
  totalCampaigns: number;
  totalClients: number;
  totalUsers: number;
  totalEmailsSent: number;
  totalEmailsSentChange?: number;
  totalEmailsOpened: number;
  totalEmailsReplied: number;
  totalEmailsBounced: number;
  totalEmailsClicked: number;
  openRate: number;
  replyRate: number;
  bounceRate: number;
  clickRate: number;
  openRateChange: number;
  replyRateChange: number;
  bounceRateChange: number;
  recentEmailsSent: number;
  recentEmailsOpened: number;
  recentEmailsReplied: number;
  activeCampaigns: number;
  pausedCampaigns: number;
  completedCampaigns: number;
  totalPositiveReplies: number;
  positiveReplyRate: number;
  positiveReplyRateChange: number;
}

export interface CampaignPerformanceMetric {
  id: string;
  name: string;
  clientName: string;
  status: string;
  totalSent: number;
  totalOpened: number;
  totalReplied: number;
  totalBounced: number;
  openRate: number;
  replyRate: number;
  bounceRate: number;
  lastActivity: string;
  daysActive: number;
}

export interface ClientPerformanceMetric {
  id: string;
  name: string;
  campaignCount: number;
  emailAccountCount: number;
  totalSent: number;
  totalOpened: number;
  totalReplied: number;
  openRate: number;
  replyRate: number;
  lastActivity: string;
  color: string;
}

export interface TimeSeriesDataPoint {
  date: string;
  emailsSent: number;
  emailsOpened: number;
  emailsReplied: number;
  emailsBounced: number;
  openRate: number;
  replyRate: number;
}

export interface EmailAccountSummary {
  totalAccounts: number;
  activeAccounts: number;
  warmingUpAccounts: number;
  warmedUpAccounts: number;
  pausedAccounts: number;
  issueAccounts: number;
  accountsByProvider: Record<string, number>;
  accountsByStatus: Record<string, AccountStatusCount>;
}

export interface AccountStatusCount {
  count: number;
  percentage: number;
}

export interface RecentActivity {
  id: string;
  type: string;
  title: string;
  description: string;
  timestamp: string;
  icon: string;
  color: string;
}

export interface DashboardFilterRequest {
  clientIds?: string[];
  campaignIds?: string[];
  startDate?: string;
  endDate?: string;
  period?: string;
}

// Webhook Types
export interface Webhook {
  id: string;
  name: string;
  url: string;
  headers: Record<string, string>;
  isActive: boolean;
  retryCount: number;
  timeoutSeconds: number;
  lastTriggeredAt?: string;
  failureCount: number;
  createdAt: string;
  updatedAt: string;
}


export interface WebhookDelivery {
  id: string;
  eventType: string;
  statusCode?: number;
  responseBody?: string;
  errorMessage?: string;
  attemptCount: number;
  deliveredAt?: string;
  createdAt: string;
  isSuccess: boolean;
}

export interface CreateWebhookRequest {
  name: string;
  url: string;
  headers?: Record<string, string>;
  retryCount?: number;
  timeoutSeconds?: number;
}

export interface UpdateWebhookRequest {
  name?: string;
  url?: string;
  headers?: Record<string, string>;
  retryCount?: number;
  timeoutSeconds?: number;
  isActive?: boolean;
}

// Webhook Event Types
export interface WebhookEventConfig {
  id: string;
  webhookId: string;
  eventType: string;
  name: string;
  description: string;
  configParameters: Record<string, any>;
  targetScope: TargetScopeConfig;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastCheckedAt?: string;
  lastTriggeredAt?: string;
}

export interface TargetScopeConfig {
  type: 'clients' | 'campaigns' | 'users';
  ids: string[];
}

export interface CreateWebhookEventConfigRequest {
  webhookId: string;
  eventType: string;
  name: string;
  description: string;
  configParameters: Record<string, any>;
  targetScope: TargetScopeConfig;
}

export interface UpdateWebhookEventConfigRequest {
  name?: string;
  description?: string;
  configParameters?: Record<string, any>;
  targetScope?: TargetScopeConfig;
  isActive?: boolean;
}

export interface WebhookEventTrigger {
  id: string;
  eventConfigId: string;
  webhookId: string;
  campaignId: string;
  campaignName: string;
  triggerData: string;
  statusCode?: number;
  responseBody?: string;
  errorMessage?: string;
  isSuccess: boolean;
  attemptCount: number;
  createdAt: string;
  deliveredAt?: string;
}

export interface EventTypeInfo {
  type: string;
  name: string;
  description: string;
  requiredParameters: {
    name: string;
    type: string;
    description: string;
  }[];
}

// Analytics Types
export interface AnalyticsDashboardResponse {
  overview: AnalyticsOverview;
  performanceTrends: PerformanceTrendDataPoint[];
  emailAccountPerformance: EmailAccountPerformanceMetric[];
  clientComparison: ClientComparisonMetric[];
}

export interface AnalyticsOverview {
  totalCampaigns: number;
  campaignGrowth: number;
  activeCampaigns: number;
  pausedCampaigns: number;
  totalEmailAccounts: number;
  emailAccountGrowth: number;
  activeClients: number;
  clientGrowth: number;
  replyRate: number;
  replyRateChange: number;
  openRate: number;
  bounceRate: number;
}

export interface PerformanceTrendDataPoint {
  date: string;
  sent: number;
  opened: number;
  replied: number;
  bounced: number;
  replyRate: number;
  openRate: number;
}

export interface EmailAccountPerformanceMetric {
  emailAccountId: string;
  email: string;
  name: string;
  sent: number;
  opened: number;
  replied: number;
  bounced: number;
  replyRate: number;
  openRate: number;
  bounceRate: number;
}

export interface ClientComparisonMetric {
  clientId: string;
  clientName: string;
  clientColor?: string;
  campaigns: number;
  activeCampaigns: number;
  sent: number;
  opened: number;
  replied: number;
  bounced: number;
  replyRate: number;
  openRate: number;
  bounceRate: number;
}