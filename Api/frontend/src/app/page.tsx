'use client';

import { useState, useEffect, useRef } from 'react';
import { ProtectedRoute } from '@/components/protected-route';
import { useAuth } from '@/contexts/auth-context';
import { usePageTitle } from '@/hooks/use-page-title';
import { PageHeader } from '@/components/page-header';
import { DashboardLoadingState } from '@/components/dashboard/dashboard-loading-state';
import { DashboardErrorState } from '@/components/dashboard/dashboard-error-state';
import { KPICards } from '@/components/dashboard/kpi-cards';
import { DashboardFilters } from '@/components/dashboard/dashboard-filters';
import { PerformanceChart } from '@/components/dashboard/performance-chart';
import { QuickStats } from '@/components/dashboard/quick-stats';
import { ClientStats } from '@/components/client-stats';
import { UserStats } from '@/components/user-stats';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Progress } from '@/components/ui/progress';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/hooks/use-toast';
import { apiClient, ENDPOINTS, handleApiErrorWithToast } from '@/lib/api';
import { Separator } from '@/components/ui/separator';
import { format } from 'date-fns';
import { 
  DashboardOverview, 
  CampaignPerformanceMetric, 
  ClientPerformanceMetric,
  EmailAccountSummary,
  RecentActivity,
  TimeSeriesDataPoint 
} from '@/types';
import { 
  BarChart3, 
  TrendingUp, 
  TrendingDown,
  Target,
  Mail,
  Users,
  Activity,
  Clock,
  Send,
  Eye,
  Reply,
  AlertTriangle,
  CheckCircle,
  Pause,
  RefreshCw,
  Calendar as CalendarIcon,
  Settings,
  ArrowUpRight,
  ArrowDownRight,
  Minus,
  Play
} from 'lucide-react';
import { 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer, 
  BarChart, 
  Bar, 
  PieChart, 
  Pie, 
  Cell,
  AreaChart,
  Area,
  ComposedChart,
  Legend,
  RadialBarChart,
  RadialBar
} from 'recharts';
import { 
  ChartContainer, 
  ChartTooltip, 
  ChartTooltipContent, 
  ChartLegend, 
  ChartLegendContent,
  type ChartConfig 
} from '@/components/ui/chart';

const COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#06B6D4', '#F97316', '#EC4899'];

// Chart configurations
const performanceChartConfig: ChartConfig = {
  emailsSent: {
    label: 'Emails Sent',
    color: 'hsl(var(--chart-1))',
  },
  emailsOpened: {
    label: 'Emails Opened',
    color: 'hsl(var(--chart-2))',
  },
  emailsReplied: {
    label: 'Emails Replied',
    color: 'hsl(var(--chart-3))',
  },
  emailsBounced: {
    label: 'Emails Bounced',
    color: 'hsl(var(--chart-4))',
  },
  openRate: {
    label: 'Open Rate %',
    color: '#8B5CF6',
  },
  replyRate: {
    label: 'Reply Rate %',
    color: '#06B6D4',
  },
};

const campaignChartConfig: ChartConfig = {
  replyRate: {
    label: 'Reply Rate',
    color: 'hsl(var(--chart-1))',
  },
  openRate: {
    label: 'Open Rate',
    color: 'hsl(var(--chart-2))',
  },
  totalSent: {
    label: 'Total Sent',
    color: 'hsl(var(--chart-3))',
  },
};

const clientChartConfig: ChartConfig = {
  totalSent: {
    label: 'Emails Sent',
    color: 'hsl(var(--chart-1))',
  },
  openRate: {
    label: 'Open Rate %',
    color: 'hsl(var(--chart-2))',
  },
  replyRate: {
    label: 'Reply Rate %',
    color: 'hsl(var(--chart-3))',
  },
};

const emailAccountChartConfig: ChartConfig = {
  active: {
    label: 'Active',
    color: 'hsl(var(--chart-1))',
  },
  warming: {
    label: 'Warming Up',
    color: 'hsl(var(--chart-2))',
  },
  paused: {
    label: 'Paused',
    color: 'hsl(var(--chart-3))',
  },
  issues: {
    label: 'Issues',
    color: 'hsl(var(--chart-4))',
  },
};

export default function DashboardPage() {
  usePageTitle('Dashboard - LeadHype');
  const { toast } = useToast();
  const { isAuthenticated, isLoading: authLoading, isAdmin } = useAuth();
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [performancePeriod, setPerformancePeriod] = useState('all');
  const [campaignPeriod, setCampaignPeriod] = useState('30');
  const [performanceTrendData, setPerformanceTrendData] = useState<TimeSeriesDataPoint[]>([]);
  const [loadingPerformanceTrend, setLoadingPerformanceTrend] = useState(false);
  const [campaignPerformanceTrendData, setCampaignPerformanceTrendData] = useState<TimeSeriesDataPoint[]>([]);
  const [loadingCampaignPerformanceTrend, setLoadingCampaignPerformanceTrend] = useState(false);
  const hasFetchedRef = useRef(false);
  const performancePeriodRef = useRef('all');
  const campaignPeriodRef = useRef('30');
  
  // Admin campaign scope state
  const [campaignScope, setCampaignScope] = useState<'all' | 'specific'>('specific');
  
  // Filter state
  const [isFilterOpen, setIsFilterOpen] = useState(false);
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [selectedClients, setSelectedClients] = useState<string[]>([]);
  const [selectedCampaigns, setSelectedCampaigns] = useState<string[]>([]);
  const [selectedEmailAccounts, setSelectedEmailAccounts] = useState<string[]>([]);
  const [selectAllUsers, setSelectAllUsers] = useState(false);
  const [selectAllClients, setSelectAllClients] = useState(false);
  const [selectAllCampaigns, setSelectAllCampaigns] = useState(false);
  const [selectAllEmailAccounts, setSelectAllEmailAccounts] = useState(false);
  
  // Real data for filters
  const [allUsers, setAllUsers] = useState<Array<{id: string, username: string, email: string, firstName?: string, lastName?: string, role: string, isActive: boolean, assignedClientIds?: string[]}>>([]);
  const [allClients, setAllClients] = useState<Array<{id: string, name: string}>>([]);
  const [allCampaigns, setAllCampaigns] = useState<Array<{id: string, name: string, clientId: string, clientName: string}>>([]);
  const [allEmailAccounts, setAllEmailAccounts] = useState<Array<{id: string, email: string, campaignIds: string[]}>>([]);
  const [loadingFilterData, setLoadingFilterData] = useState(false);
  const [userSearchQuery, setUserSearchQuery] = useState('');
  const [campaignSearchQuery, setCampaignSearchQuery] = useState('');
  
  // Campaign filters state - simplified
  const [campaignFilters, setCampaignFilters] = useState({
    minimumSent: 100,
    sortBy: 'ReplyRate',
    sortDescending: true,
    limit: 10,
    statuses: [] as string[],
    showWorst: false,
    timeRangeDays: 9999, // Default to all-time
  });
  const [topFilteredCampaigns, setTopFilteredCampaigns] = useState<any[]>([]);
  const [campaignStats, setCampaignStats] = useState<any>(null);
  const [loadingFilteredCampaigns, setLoadingFilteredCampaigns] = useState(false);
  
  // Email Account Summary state for real data
  const [realEmailAccountSummary, setRealEmailAccountSummary] = useState<EmailAccountSummary | null>(null);
  const [emailAccountSummaryLoading, setEmailAccountSummaryLoading] = useState(false);

  // Load users from API
  const loadUsers = async () => {
    try {
      setLoadingFilterData(true);
      const response = await apiClient.get<any>(ENDPOINTS.userList);
      
      console.log('User list response:', response); // Debug log
      
      // Handle different response structures
      const userData = response.data || response;
      const userArray = Array.isArray(userData) ? userData : 
                       userData.data ? userData.data : 
                       userData.items ? userData.items : [];
      
      if (userArray && userArray.length > 0) {
        const users = userArray.map((user: any) => ({
          id: user.id || user.Id || user._id,
          username: user.username || user.Username || 'Unknown',
          email: user.email || user.Email || '',
          firstName: user.firstName || user.FirstName || user.first_name,
          lastName: user.lastName || user.LastName || user.last_name,
          role: user.role || user.Role || 'User',
          isActive: user.isActive !== undefined ? user.isActive : user.IsActive !== undefined ? user.IsActive : true,
          assignedClientIds: user.assignedClientIds || user.AssignedClientIds || user.assigned_client_ids || []
        }));
        console.log('Processed users:', users); // Debug log
        setAllUsers(users);
        
        // Don't auto-select users - let the user choose
      } else {
        console.log('No users found in response'); // Debug log
        setAllUsers([]);
      }
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load users', toast);
    } finally {
      setLoadingFilterData(false);
    }
  };

  // Load clients for selected users
  const loadClientsForUsers = async (userIds: string[], usersArray?: any[]) => {
    if (userIds.length === 0) {
      setAllClients([]);
      setSelectedClients([]);
      return;
    }

    // Use provided users array or fallback to state
    const users = usersArray || allUsers;
    
    // Get all assigned client IDs from selected users
    const assignedClientIds = new Set<string>();
    userIds.forEach(userId => {
      const user = users.find(u => u.id === userId);
      if (user?.assignedClientIds) {
        user.assignedClientIds.forEach((clientId: string) => assignedClientIds.add(clientId));
      }
    });

    // Load clients from API and filter by assigned client IDs
    try {
      setLoadingFilterData(true);
      const response = await apiClient.get<any>(ENDPOINTS.clientList);
      
      const clientData = response.data || response;
      const clientArray = Array.isArray(clientData) ? clientData : 
                         clientData.data ? clientData.data : 
                         clientData.items ? clientData.items : [];
      
      if (clientArray && clientArray.length > 0) {
        const allClientsFromApi = clientArray.map((client: any) => ({
          id: client.id || client.Id || client._id,
          name: client.name || client.Name || client.clientName || 'Unknown'
        }));
        
        // Filter clients by those assigned to selected users
        const filteredClients = allClientsFromApi.filter((client: any) => 
          assignedClientIds.has(client.id)
        );
        
        setAllClients(filteredClients);
        
        // If select all clients is enabled, select all filtered clients
        if (selectAllClients) {
          const clientIds = filteredClients.map((c: any) => c.id);
          setSelectedClients(clientIds);
          // Load campaigns for filtered clients
          loadCampaigns(clientIds);
        }
      } else {
        setAllClients([]);
      }
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load clients for users', toast);
    } finally {
      setLoadingFilterData(false);
    }
  };

  // Load clients from API
  const loadClients = async () => {
    try {
      setLoadingFilterData(true);
      const response = await apiClient.get<any>(ENDPOINTS.clientList);
      
      console.log('Client list response:', response); // Debug log
      
      // Handle different response structures
      const clientData = response.data || response;
      const clientArray = Array.isArray(clientData) ? clientData : 
                         clientData.data ? clientData.data : 
                         clientData.items ? clientData.items : [];
      
      if (clientArray && clientArray.length > 0) {
        const clients = clientArray.map((client: any) => ({
          id: client.id || client.Id || client._id,
          name: client.name || client.Name || client.clientName || 'Unknown'
        }));
        console.log('Processed clients:', clients); // Debug log
        setAllClients(clients);
        
        // If select all is enabled, select all clients
        if (selectAllClients) {
          const clientIds = clients.map((c: any) => c.id);
          setSelectedClients(clientIds);
          // Load campaigns for all clients
          loadCampaigns(clientIds);
        }
      } else {
        console.log('No clients found in response'); // Debug log
        setAllClients([]);
      }
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load clients', toast);
    } finally {
      setLoadingFilterData(false);
    }
  };

  // Load campaigns based on selected clients
  const loadCampaigns = async (clientIds: string[]) => {
    try {
      setLoadingFilterData(true);
      
      // If no clients selected, clear campaigns
      if (clientIds.length === 0) {
        setAllCampaigns([]);
        setSelectedCampaigns([]);
        return;
      }
      
      // Fetch all campaigns across all pages for selected clients using POST to avoid header size issues
      const allCampaignData: any[] = [];
      let currentPage = 1;
      let hasMorePages = true;
      
      while (hasMorePages) {
        // Use POST request to send client IDs in request body instead of query parameters
        const response = await apiClient.post<any>(
          `${ENDPOINTS.campaigns}/filter`,
          {
            clientIds: clientIds,
            page: currentPage,
            pageSize: 1000 // Increased from 100 to 1000 to reduce number of requests
          }
        );
        
        // Process campaigns from current page (data field contains the array)
        if (response.data && response.data.length > 0) {
          response.data.forEach((campaign: any) => {
            const client = allClients.find(c => c.id === campaign.clientId);
            
            allCampaignData.push({
              id: campaign.id,
              name: campaign.name,
              clientId: campaign.clientId,
              clientName: client?.name || campaign.clientName || 'Unknown Client'
            });
          });
        }
        
        // Check if there are more pages using hasNext field from API response
        hasMorePages = response.hasNext === true;
        currentPage++;
        
        // Safety check to prevent infinite loops
        if (currentPage > 100) {
          console.warn('Campaign pagination stopped at 100 pages to prevent infinite loop');
          break;
        }
      }
      
      setAllCampaigns(allCampaignData);
      
      // If select all campaigns is enabled, select all campaigns
      if (selectAllCampaigns) {
        const campaignIds = allCampaignData.map(c => c.id);
        setSelectedCampaigns(campaignIds);
        // Load email accounts for all campaigns
        loadEmailAccounts(campaignIds);
      }
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load campaigns', toast);
    } finally {
      setLoadingFilterData(false);
    }
  };

  // Load email accounts based on selected campaigns
  const loadEmailAccounts = async (campaignIds: string[]) => {
    try {
      setLoadingFilterData(true);
      
      // If no campaigns selected, clear email accounts
      if (campaignIds.length === 0) {
        setAllEmailAccounts([]);
        setSelectedEmailAccounts([]);
        return;
      }
      
      // Use POST request to avoid large header issues when there are many campaign IDs
      const response = await apiClient.post<any>(`${ENDPOINTS.emailAccounts}/filter`, {
        campaignIds: campaignIds
      });
      
      const emailAccountsData: Array<{id: string, email: string, name?: string, campaignIds: string[]}> = [];
      
      if (response.data && Array.isArray(response.data)) {
        response.data.forEach((account: any) => {
          if (account && account.id && account.email) {
            emailAccountsData.push({
              id: account.id,
              email: account.email,
              name: account.name,
              campaignIds: Array.isArray(account.campaignIds) ? account.campaignIds : campaignIds // Use provided campaignIds as fallback
            });
          }
        });
      }
      
      setAllEmailAccounts(emailAccountsData);
      
      // If select all email accounts is enabled, select all email accounts
      if (selectAllEmailAccounts) {
        setSelectedEmailAccounts(emailAccountsData.map(ea => ea.id));
      }
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load email accounts', toast);
    } finally {
      setLoadingFilterData(false);
    }
  };

  const loadPerformanceTrendData = async (period: string, filters?: {
    clientIds?: string[];
    campaignIds?: string[];
    startDate?: Date;
    endDate?: Date;
  }) => {
    try {
      setLoadingPerformanceTrend(true);
      
      let response: TimeSeriesDataPoint[];
      
      // Check if admin wants all campaigns system-wide
      const isAllCampaignsScope = isAdmin && campaignScope === 'all';
      
      if (isAllCampaignsScope) {
        // Admin with "All Campaigns" scope - always use GET with allCampaigns parameter
        response = await apiClient.get<TimeSeriesDataPoint[]>(
          `${ENDPOINTS.analytics.dashboard}/performance-trend?period=${period}&allCampaigns=true`
        );
      } else if (filters && (filters.clientIds?.length || filters.campaignIds?.length || filters.startDate || filters.endDate)) {
        // Use POST request with filter data in body
        const filterRequest = {
          ClientIds: filters.clientIds || [],
          CampaignIds: filters.campaignIds || [],
          StartDate: filters.startDate?.toISOString(),
          EndDate: filters.endDate?.toISOString(),
          Period: period,
          AllCampaigns: false // Explicitly set to false for filtered requests
        };
        
        response = await apiClient.post<any>(
          ENDPOINTS.analytics.dashboard + '/filtered-performance-trend',
          filterRequest
        );
      } else {
        // Use regular GET request for no filters
        response = await apiClient.get<any>(
          `${ENDPOINTS.analytics.dashboard}/performance-trend?period=${period}`
        );
      }

      // Transform PascalCase to camelCase if needed
      const transformedData = Array.isArray(response) ? response.map((item: any) => ({
        date: item.date || item.Date,
        emailsSent: item.emailsSent || item.EmailsSent || 0,
        emailsOpened: item.emailsOpened || item.EmailsOpened || 0,
        emailsReplied: item.emailsReplied || item.EmailsReplied || 0,
        emailsBounced: item.emailsBounced || item.EmailsBounced || 0,
        openRate: item.openRate || item.OpenRate || 0,
        replyRate: item.replyRate || item.ReplyRate || 0
      })) : [];
      
      setPerformanceTrendData(transformedData);
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load performance trend data', toast);
      setPerformanceTrendData([]);
    } finally {
      setLoadingPerformanceTrend(false);
    }
  };

  const loadCampaignPerformanceTrendData = async (period: string, filters?: {
    clientIds?: string[],
    startDate?: string,
    endDate?: string
  }) => {
    try {
      setLoadingCampaignPerformanceTrend(true);
      let response;
      
      // Check if admin wants all campaigns system-wide
      const isAllCampaignsScope = isAdmin && campaignScope === 'all';
      
      if (isAllCampaignsScope) {
        // Admin with "All Campaigns" scope - use GET with allCampaigns parameter
        response = await apiClient.get<TimeSeriesDataPoint[]>(
          `${ENDPOINTS.analytics.dashboard}/campaign-performance-trend?period=${period}&allCampaigns=true`
        );
      } else if (filters?.clientIds?.length || filters?.startDate || filters?.endDate) {
        // Use POST endpoint for complex filters
        const filterData = {
          period: period,
          clientIds: filters.clientIds || [],
          startDate: filters.startDate,
          endDate: filters.endDate,
          allCampaigns: false // Explicitly set to false for filtered requests
        };
        
        response = await apiClient.post<TimeSeriesDataPoint[]>(
          ENDPOINTS.analytics.dashboard + '/filtered-campaign-performance-trend',
          filterData
        );
      } else {
        // Use simple GET endpoint
        response = await apiClient.get<TimeSeriesDataPoint[]>(
          `${ENDPOINTS.analytics.dashboard}/campaign-performance-trend?period=${period}`
        );
      }
      
      // Transform data to match chart format
      const transformedData = Array.isArray(response) ? response.map(point => ({
        ...point,
        date: point.date // Backend already returns yyyy-MM-dd format
      })) : [];
      
      console.log('Campaign performance trend data:', JSON.stringify(transformedData, null, 2));
      setCampaignPerformanceTrendData(transformedData);
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load campaign performance trend data', toast);
      setCampaignPerformanceTrendData([]);
    } finally {
      setLoadingCampaignPerformanceTrend(false);
    }
  };

  const loadFilteredCampaigns = async () => {
    try {
      setLoadingFilteredCampaigns(true);
      
      // Build query parameters from filters
      const queryParams = new URLSearchParams();
      queryParams.append('minimumSent', campaignFilters.minimumSent.toString());
      
      // Check if admin wants all campaigns system-wide
      const isAllCampaignsScope = isAdmin && campaignScope === 'all';
      if (isAllCampaignsScope) {
        queryParams.append('allCampaigns', 'true');
      }
      
      // Map rate options to backend format and determine if percentage mode is needed
      let sortBy = campaignFilters.sortBy;
      let sortMode = 'count'; // default mode
      
      switch (campaignFilters.sortBy) {
        case 'ReplyRate':
          sortBy = 'totalreplied';
          sortMode = 'percentage';
          break;
        case 'OpenRate':
          sortBy = 'totalopened';
          sortMode = 'percentage';
          break;
        case 'BounceRate':
          sortBy = 'totalbounced';
          sortMode = 'percentage';
          break;
        case 'positivereplyrate':
          // This uses the new backend implementation
          sortBy = 'positivereplyrate';
          sortMode = 'count'; // We handle the percentage calculation in the backend
          break;
        default:
          sortBy = campaignFilters.sortBy.toLowerCase();
          break;
      }
      
      queryParams.append('sortBy', sortBy);
      queryParams.append('sortMode', sortMode);
      // Invert sort direction when showing worst campaigns
      const effectiveSortDescending = campaignFilters.showWorst ? !campaignFilters.sortDescending : campaignFilters.sortDescending;
      queryParams.append('sortDescending', effectiveSortDescending.toString());
      queryParams.append('limit', campaignFilters.limit.toString());
      
      if (campaignFilters.statuses.length > 0) {
        queryParams.append('statuses', campaignFilters.statuses.join(','));
      }
      
      // Add time range filter
      if (campaignFilters.timeRangeDays && campaignFilters.timeRangeDays < 9999) {
        queryParams.append('timeRangeDays', campaignFilters.timeRangeDays.toString());
      }
      
      const response = await apiClient.get<any>(
        `/api/dashboard/filtered-top-campaigns?${queryParams.toString()}`
      );
      
      // Smooth data update without layout shift
      const newCampaigns = response.campaigns || [];
      const newStats = response.stats;
      
      // Use double requestAnimationFrame to ensure smooth transition
      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          setTopFilteredCampaigns(newCampaigns);
          setCampaignStats(newStats);
        });
      });
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load filtered campaigns', toast);
      // Don't clear existing data on error, just show error
      if (topFilteredCampaigns.length === 0) {
        setTopFilteredCampaigns([]);
        setCampaignStats(null);
      }
    } finally {
      // Delay loading state to prevent flicker
      setTimeout(() => {
        setLoadingFilteredCampaigns(false);
      }, 150);
    }
  };

  // Load real email account summary data
  const loadRealEmailAccountSummary = async () => {
    try {
      setEmailAccountSummaryLoading(true);
      
      // Check if admin wants all campaigns system-wide
      const isAllCampaignsScope = isAdmin && campaignScope === 'all';
      const endpoint = isAllCampaignsScope 
        ? `${ENDPOINTS.dashboard.emailAccountsSummary}?allCampaigns=true`
        : ENDPOINTS.dashboard.emailAccountsSummary;
        
      const response = await apiClient.get<any>(endpoint);
      
      // Map the API response to the expected interface
      const mappedData: EmailAccountSummary = {
        totalAccounts: response.totalAccounts || 0,
        activeAccounts: response.activeAccounts || 0,
        warmingUpAccounts: response.warmingUpAccounts || 0,
        warmedUpAccounts: response.warmedUpAccounts || 0,
        pausedAccounts: response.pausedAccounts || 0,
        issueAccounts: response.issueAccounts || 0,
        accountsByProvider: response.accountsByProvider || {},
        accountsByStatus: response.accountsByStatus || {}
      };
      
      setRealEmailAccountSummary(mappedData);
    } catch (error) {
      console.warn('Failed to load real email account summary:', error);
      // Keep existing fallback data from overview
    } finally {
      setEmailAccountSummaryLoading(false);
    }
  };

  const loadDashboardData = async (showRefreshLoader = false, filters?: {
    clientIds?: string[];
    campaignIds?: string[];
    startDate?: Date;
    endDate?: Date;
    period?: string;
  }) => {
    try {
      if (showRefreshLoader) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }

      // Try to load real data from API (proxy server should handle routing)
      try {
        let response: DashboardOverview;
        
        // Check if admin wants all campaigns system-wide
        const isAllCampaignsScope = isAdmin && campaignScope === 'all';
        
        if (isAllCampaignsScope) {
          // Admin with "All Campaigns" scope - use GET with allCampaigns parameter
          response = await apiClient.get<DashboardOverview>(
            `${ENDPOINTS.analytics.dashboard}?allCampaigns=true`
          );
        } else if (filters && (filters.clientIds?.length || filters.campaignIds?.length || filters.startDate || filters.endDate)) {
          // Use POST request with filter data in body
          const filterRequest = {
            ClientIds: filters.clientIds || [],
            CampaignIds: filters.campaignIds || [],
            StartDate: filters.startDate?.toISOString(),
            EndDate: filters.endDate?.toISOString(),
            Period: filters.period || "30",
            AllCampaigns: false // Explicitly set to false for filtered requests
          };
          
          response = await apiClient.post<DashboardOverview>(
            ENDPOINTS.analytics.dashboard + '/filtered-overview',
            filterRequest
          );
        } else {
          // Use regular GET request for no filters
          response = await apiClient.get<DashboardOverview>(ENDPOINTS.analytics.dashboard);
        }
        
        setOverview(response);
      } catch (apiError) {
        console.error('API call failed:', apiError);
        // No fallback to sample data - let the outer catch handle the error
        throw apiError;
      }
    } catch (error: any) {
      handleApiErrorWithToast(error, 'load dashboard data', toast);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    // Only load dashboard data if user is authenticated and we haven't fetched yet
    if (isAuthenticated && !authLoading && !hasFetchedRef.current) {
      hasFetchedRef.current = true;
      loadDashboardData();
      // Load initial performance trend data for all time
      loadPerformanceTrendData('all');
      // Load initial campaign performance trend data for 30 days
      loadCampaignPerformanceTrendData('30');
      // Load real email account summary data
      loadRealEmailAccountSummary();
      
      // Auto-refresh every 2 minutes
      const interval = setInterval(() => {
        loadDashboardData(true);
        // Also refresh performance trend data with current selected period
        loadPerformanceTrendData(performancePeriodRef.current);
        // Also refresh campaign performance trend data with current selected period
        loadCampaignPerformanceTrendData(campaignPeriodRef.current);
        // Refresh email account summary data
        loadRealEmailAccountSummary();
      }, 2 * 60 * 1000);

      return () => clearInterval(interval);
    }
  }, [isAuthenticated, authLoading]);

  const formatNumber = (num: number) => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toLocaleString();
  };

  const formatRate = (rate: number) => `${rate.toFixed(1)}%`;

  const formatCurrency = (amount: number) => `$${amount.toLocaleString()}`;

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'text-green-600 dark:text-green-400 border-green-600/30 dark:border-green-400/30 bg-green-500/10';
      case 'paused': return 'text-yellow-600 dark:text-yellow-400 border-yellow-600/30 dark:border-yellow-400/30 bg-yellow-500/10';
      case 'completed': return 'text-blue-600 dark:text-blue-400 border-blue-600/30 dark:border-blue-400/30 bg-blue-500/10';
      case 'draft': return 'text-gray-600 dark:text-gray-400 border-gray-600/30 dark:border-gray-400/30 bg-gray-500/10';
      default: return 'text-gray-600 dark:text-gray-400 border-gray-600/30 dark:border-gray-400/30 bg-gray-500/10';
    }
  };

  const getChangeIcon = (change: number) => {
    if (change > 0) return <ArrowUpRight className="w-4 h-4 text-green-500" />;
    if (change < 0) return <ArrowDownRight className="w-4 h-4 text-red-500" />;
    return <Minus className="w-4 h-4 text-gray-400" />;
  };

  const getChangeColor = (change: number) => {
    if (change > 0) return 'text-green-600';
    if (change < 0) return 'text-red-600';
    return 'text-gray-500';
  };

  // Filter functions
  const handleUserToggle = (userId: string) => {
    setSelectedUsers(prev => {
      const newSelected = prev.includes(userId) 
        ? prev.filter(id => id !== userId)
        : [...prev, userId];
      
      // Don't call loadClientsForUsers here - useEffect will handle it
      return newSelected;
    });
  };

  const handleClientToggle = (clientId: string) => {
    setSelectedClients(prev => {
      const newSelected = prev.includes(clientId) 
        ? prev.filter(id => id !== clientId)
        : [...prev, clientId];
      
      // Load campaigns for the new client selection
      loadCampaigns(newSelected);
      
      return newSelected;
    });
  };

  const handleCampaignToggle = (campaignId: string) => {
    setSelectedCampaigns(prev => {
      const newSelected = prev.includes(campaignId) 
        ? prev.filter(id => id !== campaignId)
        : [...prev, campaignId];
      
      // Load email accounts for the new campaign selection
      loadEmailAccounts(newSelected);
      
      return newSelected;
    });
  };

  const handleEmailAccountToggle = (emailAccountId: string) => {
    setSelectedEmailAccounts(prev => 
      prev.includes(emailAccountId) 
        ? prev.filter(id => id !== emailAccountId)
        : [...prev, emailAccountId]
    );
  };

  const handleSelectAllUsers = (checked: boolean) => {
    setSelectAllUsers(checked);
    if (checked) {
      const allUserIds = allUsers.map(u => u.id);
      setSelectedUsers(allUserIds);
      // Don't call loadClientsForUsers here - useEffect will handle it
    } else {
      setSelectedUsers([]);
      setAllClients([]);
      setSelectedClients([]);
      setAllCampaigns([]);
      setSelectedCampaigns([]);
    }
  };

  const handleSelectAllClients = (checked: boolean) => {
    setSelectAllClients(checked);
    if (checked) {
      const allClientIds = allClients.map(c => c.id);
      setSelectedClients(allClientIds);
      // Load campaigns for all clients
      loadCampaigns(allClientIds);
    } else {
      setSelectedClients([]);
      setAllCampaigns([]);
      setSelectedCampaigns([]);
    }
  };

  const handleSelectAllCampaigns = (checked: boolean) => {
    setSelectAllCampaigns(checked);
    if (checked) {
      // Select all filtered campaigns (visible campaigns)
      const currentlyFiltered = filterCampaignsBySearchAndClient(allCampaigns);
      const filteredCampaignIds = currentlyFiltered.map(c => c.id);
      setSelectedCampaigns(prev => {
        // Keep previously selected campaigns + add all currently visible campaigns
        const newSelection = new Set([...prev, ...filteredCampaignIds]);
        return Array.from(newSelection);
      });
    } else {
      // Deselect all filtered campaigns (visible campaigns)
      const currentlyFiltered = filterCampaignsBySearchAndClient(allCampaigns);
      const filteredCampaignIds = currentlyFiltered.map(c => c.id);
      setSelectedCampaigns(prev => prev.filter(id => !filteredCampaignIds.includes(id)));
    }
  };

  const handleSelectAllEmailAccounts = (checked: boolean) => {
    setSelectAllEmailAccounts(checked);
    if (checked) {
      const allEmailAccountIds = allEmailAccounts.map(ea => ea.id);
      setSelectedEmailAccounts(allEmailAccountIds);
    } else {
      setSelectedEmailAccounts([]);
    }
  };

  const applyFilters = async () => {
    // Validation: For specific campaigns mode, at least one campaign must be selected
    if (!(isAdmin && campaignScope === 'all') && selectedCampaigns.length === 0) {
      toast({
        variant: "destructive",
        title: "No Campaigns Selected",
        description: "Please select at least one campaign to apply filters, or switch to 'All Campaigns' mode.",
      });
      return;
    }

    // Build filter object based on admin campaign scope
    let filters: any = {};
    
    if (isAdmin && campaignScope === 'all') {
      // For admins with "All Campaigns" scope, don't include user, client or campaign filters
      filters = {
        emailAccountIds: selectedEmailAccounts.length > 0 ? selectedEmailAccounts : undefined,
      };
    } else {
      // For regular users or admins with "Specific Clients" scope
      filters = {
        userIds: selectedUsers.length > 0 ? selectedUsers : undefined,
        clientIds: selectedClients.length > 0 ? selectedClients : undefined,
        campaignIds: selectedCampaigns.length > 0 ? selectedCampaigns : undefined,
        emailAccountIds: selectedEmailAccounts.length > 0 ? selectedEmailAccounts : undefined,
      };
    }

    try {
      // Close dialog immediately to prevent flickering
      setIsFilterOpen(false);
      
      // Reload dashboard data with filters
      await loadDashboardData(false, filters);
      
      {
        await loadPerformanceTrendData(performancePeriod);
      }
      
      // Update toast message based on scope
      const scopeDescription = isAdmin && campaignScope === 'all' 
        ? 'All campaigns system-wide'
        : `${selectedUsers.length || 'all'} users, ${selectedClients.length || 'all'} clients, ${selectedCampaigns.length} campaigns`;
        
      toast({
        title: "Filters Applied",
        description: `Dashboard filtered by ${scopeDescription}, ${selectedEmailAccounts.length || 'all'} email accounts`,
      });
    } catch (error) {
      toast({
        variant: "destructive",
        title: "Filter Error",
        description: "Failed to apply filters. Please try again.",
      });
    }
  };

  const clearFilters = async () => {
    setSelectedUsers([]);
    setSelectedClients([]);
    setSelectedCampaigns([]);
    setSelectedEmailAccounts([]);
    setAllUsers([]);
    setAllClients([]);
    setAllCampaigns([]);
    setAllEmailAccounts([]);
    setUserSearchQuery('');
    setCampaignSearchQuery('');
    setSelectAllUsers(false);
    setSelectAllClients(false);
    setSelectAllCampaigns(false);
    setSelectAllEmailAccounts(false);
    
    // Reset campaign scope for admins
    if (isAdmin) {
      setCampaignScope('specific');
    }
    
    // Reload dashboard data without any filters
    try {
      await loadDashboardData(false);
      await loadPerformanceTrendData(performancePeriod);
      
      toast({
        title: "Filters Cleared",
        description: "Dashboard restored to show all data",
      });
    } catch (error) {
      toast({
        variant: "destructive",
        title: "Error",
        description: "Failed to clear filters. Please try again.",
      });
    }
  };

  // Helper function to filter campaigns based on search and client selection
  const filterCampaignsBySearchAndClient = (campaigns: any[]) => {
    return campaigns.filter(campaign => {
      const matchesSearch = (campaign.name || '').toLowerCase().includes(campaignSearchQuery.toLowerCase()) ||
        (campaign.clientName || '').toLowerCase().includes(campaignSearchQuery.toLowerCase());
      
      if (selectedClients.length === 0) {
        return matchesSearch;
      }
      
      const belongsToSelectedClient = selectedClients.includes(campaign.clientId || '');
      return matchesSearch && belongsToSelectedClient;
    });
  };

  // Filter campaigns based on search query AND selected clients
  const filteredCampaigns = filterCampaignsBySearchAndClient(allCampaigns);

  // Check if all visible campaigns are selected
  const allVisibleCampaignsSelected = filteredCampaigns.length > 0 && 
    filteredCampaigns.every(campaign => selectedCampaigns.includes(campaign.id));

  // Load users and clients when filter modal opens
  useEffect(() => {
    if (isFilterOpen && allUsers.length === 0) {
      loadUsers();
    }
  }, [isFilterOpen]);

  // Load clients when users are selected (handles initial "select all" case)
  useEffect(() => {
    if (selectedUsers.length > 0 && allUsers.length > 0) {
      loadClientsForUsers(selectedUsers, allUsers);
    }
  }, [selectedUsers, allUsers]);

  // Load filtered campaigns when filters change (debounced)
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      loadFilteredCampaigns();
    }, 300); // 300ms debounce

    return () => clearTimeout(timeoutId);
  }, [campaignFilters]);

  // Load email accounts when campaigns are selected
  useEffect(() => {
    if (selectedCampaigns.length > 0) {
      loadEmailAccounts(selectedCampaigns);
    } else {
      // Clear email accounts if no campaigns selected
      setAllEmailAccounts([]);
      setSelectedEmailAccounts([]);
    }
  }, [selectedCampaigns]);

  if (loading) {
    return <DashboardLoadingState />;
  }

  if (!overview) {
    return <DashboardErrorState onRetry={() => loadDashboardData()} />;
  }

  const { stats, topCampaigns, topClients, performanceTrend, emailAccountSummary, recentActivities } = overview;
  
  // Debug: Log the data sources
  console.log('realEmailAccountSummary:', realEmailAccountSummary);
  console.log('emailAccountSummary from overview:', emailAccountSummary);
  
  // Use overview data first if it has detailed provider data, otherwise use real data
  const hasDetailedProviderData = emailAccountSummary?.accountsByProvider && 
    Object.keys(emailAccountSummary.accountsByProvider).length > 1;
  
  const currentEmailAccountSummary = hasDetailedProviderData ? emailAccountSummary : 
                                    (realEmailAccountSummary || emailAccountSummary);

  // Helper function to handle period changes with current filters
  const handlePerformancePeriodChange = (period: string) => {
    setPerformancePeriod(period);
    performancePeriodRef.current = period;
    
    {
      loadPerformanceTrendData(period);
    }
  };

  const handleCampaignPeriodChange = (period: string) => {
    setCampaignPeriod(period);
    campaignPeriodRef.current = period;
    
    // Check if we have active filters
    const hasFilters = selectedClients.length > 0 || selectedCampaigns.length > 0 || selectedEmailAccounts.length > 0;
    if (hasFilters) {
      const filterData = {
        clientIds: selectedClients.length > 0 ? selectedClients : undefined,
        campaignIds: selectedCampaigns.length > 0 ? selectedCampaigns : undefined
      };
      loadCampaignPerformanceTrendData(period, filterData);
    } else {
      loadCampaignPerformanceTrendData(period);
    }
  };

  // Use performanceTrendData directly since it comes from the backend with the correct period
  const filteredPerformanceData = performanceTrendData
    .filter(item => item.date && !isNaN(new Date(item.date).getTime()))
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
  const filteredCampaignPerformanceData = campaignPerformanceTrendData
    .filter(item => item.date && !isNaN(new Date(item.date).getTime()))
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

  return (
    <ProtectedRoute>
      <div className="flex h-full flex-col">
        <PageHeader 
          title="Dashboard"
          description="Campaign performance and analytics overview"
          icon={BarChart3}
          actions={
<DashboardFilters
              isFilterOpen={isFilterOpen}
              setIsFilterOpen={setIsFilterOpen}
              selectedUsers={selectedUsers}
              selectedClients={selectedClients}
              selectedCampaigns={selectedCampaigns}
              selectedEmailAccounts={selectedEmailAccounts}
              selectAllUsers={selectAllUsers}
              selectAllClients={selectAllClients}
              selectAllCampaigns={selectAllCampaigns}
              selectAllEmailAccounts={selectAllEmailAccounts}
              allUsers={allUsers}
              allClients={allClients}
              allCampaigns={allCampaigns}
              allEmailAccounts={allEmailAccounts}
              loadingFilterData={loadingFilterData}
              userSearchQuery={userSearchQuery}
              setUserSearchQuery={setUserSearchQuery}
              campaignSearchQuery={campaignSearchQuery}
              setCampaignSearchQuery={setCampaignSearchQuery}
              handleUserToggle={handleUserToggle}
              handleClientToggle={handleClientToggle}
              handleCampaignToggle={handleCampaignToggle}
              handleEmailAccountToggle={handleEmailAccountToggle}
              handleSelectAllUsers={handleSelectAllUsers}
              handleSelectAllClients={handleSelectAllClients}
              handleSelectAllCampaigns={handleSelectAllCampaigns}
              handleSelectAllEmailAccounts={handleSelectAllEmailAccounts}
              applyFilters={applyFilters}
              clearFilters={clearFilters}
              refreshing={refreshing}
              onRefresh={() => loadDashboardData(true)}
              isAdmin={isAdmin}
              campaignScope={campaignScope}
              setCampaignScope={setCampaignScope}
            />
          }
        />

        <div className="flex-1 p-6 space-y-6 overflow-auto">
          <KPICards stats={stats} />

          <Separator className="my-6" />

          {/* Performance Analytics */}
          <div className="space-y-1 mb-6">
            <h2 className="text-lg font-semibold">Performance Analytics</h2>
            <p className="text-sm text-muted-foreground">Email trends and campaign insights</p>
          </div>
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            {/* Performance Chart - Takes 2 columns */}
            <Card className="lg:col-span-2 rounded-xl">
              <CardHeader className="pb-4">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-lg">Email Performance Trends</CardTitle>
                    <CardDescription className="text-sm">
                      Email activity and performance rates over the last {performancePeriod === 'all' ? 'year' : performancePeriod === '6m' ? '6 months' : performancePeriod === '1y' ? 'year' : `${performancePeriod} days`} ({filteredPerformanceData.length} days)
                    </CardDescription>
                  </div>
                  <div className="flex items-center gap-1 flex-wrap">
                    <Button 
                      variant={performancePeriod === '7' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handlePerformancePeriodChange('7')}
                      disabled={loadingPerformanceTrend}
                      className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
                    >
                      7D
                    </Button>
                    <Button 
                      variant={performancePeriod === '30' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handlePerformancePeriodChange('30')}
                      disabled={loadingPerformanceTrend}
                      className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
                    >
                      30D
                    </Button>
                    <Button 
                      variant={performancePeriod === '90' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handlePerformancePeriodChange('90')}
                      disabled={loadingPerformanceTrend}
                      className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
                    >
                      90D
                    </Button>
                    <Button 
                      variant={performancePeriod === '6m' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handlePerformancePeriodChange('6m')}
                      disabled={loadingPerformanceTrend}
                      className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
                    >
                      6M
                    </Button>
                    <Button 
                      variant={performancePeriod === '1y' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handlePerformancePeriodChange('1y')}
                      disabled={loadingPerformanceTrend}
                      className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
                    >
                      1Y
                    </Button>
                    <Button 
                      variant={performancePeriod === 'all' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handlePerformancePeriodChange('all')}
                      disabled={loadingPerformanceTrend}
                      className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
                    >
                      All
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="p-4">
                <div className="w-full h-64 relative">
                  {loadingPerformanceTrend && (
                    <div className="absolute inset-0 bg-background/80 backdrop-blur-sm flex flex-col justify-end p-4 z-10">
                      {/* Chart skeleton */}
                      <div className="space-y-2">
                        {/* Y-axis labels */}
                        <div className="flex items-end justify-between h-48">
                          {[...Array(7)].map((_, i) => {
                            const heights = ['h-16', 'h-24', 'h-32', 'h-20', 'h-28', 'h-12', 'h-36'];
                            return (
                              <div key={i} className="flex flex-col items-center space-y-1">
                                <Skeleton className={`w-6 ${heights[i]} rounded-t animate-pulse`} />
                                <Skeleton className="w-6 h-8 rounded-t opacity-70 animate-pulse" style={{animationDelay: `${i * 100}ms`}} />
                              </div>
                            );
                          })}
                        </div>
                        {/* X-axis labels */}
                        <div className="flex justify-between">
                          {[...Array(7)].map((_, i) => (
                            <Skeleton key={i} className="h-3 w-8" />
                          ))}
                        </div>
                      </div>
                    </div>
                  )}
                  <ChartContainer 
                    key={`performance-chart-${performancePeriod}`} 
                    config={performanceChartConfig} 
                    className="w-full h-full"
                  >
                    <ComposedChart 
                      data={filteredPerformanceData}
                      margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => {
                          try {
                            const date = new Date(value);
                            return isNaN(date.getTime()) ? value : date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
                          } catch (e) {
                            return value;
                          }
                        }}
                        fontSize={12}
                        interval="preserveStartEnd"
                        tick={{ fontSize: 12 }}
                      />
                      <YAxis 
                        yAxisId="count"
                        fontSize={12}
                        tick={{ fontSize: 12 }}
                      />
                      <YAxis 
                        yAxisId="rate"
                        orientation="right"
                        fontSize={12}
                        tick={{ fontSize: 12 }}
                        domain={[0, 100]}
                        tickFormatter={(value) => `${value}%`}
                      />
                      <ChartTooltip 
                        content={<ChartTooltipContent 
                          labelFormatter={(value) => {
                            try {
                              const date = new Date(value as string);
                              const dateStr = isNaN(date.getTime()) ? value as string : date.toLocaleDateString('en-US', { 
                                weekday: 'short',
                                month: 'long', 
                                day: 'numeric', 
                                year: 'numeric' 
                              });
                              return `Activity - ${dateStr}`;
                            } catch (e) {
                              return value as string;
                            }
                          }}
                        />}
                      />
                      <ChartLegend content={<ChartLegendContent />} />
                      <Area 
                        type="monotone" 
                        dataKey="emailsSent" 
                        yAxisId="count"
                        fill="var(--color-emailsSent)" 
                        fillOpacity={0.1}
                        stroke="var(--color-emailsSent)" 
                        strokeWidth={2} 
                      />
                      <Line 
                        type="monotone" 
                        dataKey="emailsOpened" 
                        yAxisId="count"
                        stroke="var(--color-emailsOpened)" 
                        strokeWidth={3} 
                        dot={{ fill: 'var(--color-emailsOpened)', strokeWidth: 2, r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="emailsReplied" 
                        yAxisId="count"
                        stroke="var(--color-emailsReplied)" 
                        strokeWidth={3} 
                        dot={{ fill: 'var(--color-emailsReplied)', strokeWidth: 2, r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="emailsBounced" 
                        yAxisId="count"
                        stroke="var(--color-emailsBounced)" 
                        strokeWidth={2} 
                        dot={{ fill: 'var(--color-emailsBounced)', strokeWidth: 2, r: 3 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="openRate" 
                        yAxisId="rate"
                        stroke="#8B5CF6" 
                        strokeWidth={2} 
                        strokeDasharray="5 5"
                        dot={{ fill: '#8B5CF6', strokeWidth: 2, r: 3 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="replyRate" 
                        yAxisId="rate"
                        stroke="#06B6D4" 
                        strokeWidth={2} 
                        strokeDasharray="3 3"
                        dot={{ fill: '#06B6D4', strokeWidth: 2, r: 3 }}
                      />
                    </ComposedChart>
                  </ChartContainer>
                </div>
                <div className="flex items-center justify-center mt-2">
                  <p className="text-xs text-muted-foreground">
                    📊 Email counts (left axis) & rates (right axis) • Dashed lines show percentages
                  </p>
                </div>
              </CardContent>
            </Card>

            {/* Quick Stats Sidebar */}
            <Card className="rounded-xl">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg">Quick Stats</CardTitle>
                <CardDescription className="text-sm">Key metrics at a glance</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3 p-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Mail className="w-4 h-4 text-blue-500" />
                    <span className="text-xs font-medium">Email Accounts</span>
                  </div>
                  <span className="text-sm font-medium">{stats.totalEmailAccounts}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Target className="w-4 h-4 text-green-500" />
                    <span className="text-xs font-medium">Total Campaigns</span>
                  </div>
                  <span className="text-sm font-medium">{stats.totalCampaigns}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Play className="w-4 h-4 text-emerald-500" />
                    <span className="text-xs font-medium">Active Campaigns</span>
                  </div>
                  <span className="text-sm font-medium">{stats.activeCampaigns}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Users className="w-4 h-4 text-purple-500" />
                    <span className="text-xs font-medium">Clients</span>
                  </div>
                  <span className="text-sm font-medium">{stats.totalClients}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Send className="w-4 h-4 text-blue-600" />
                    <span className="text-xs font-medium">Total Emails Sent</span>
                  </div>
                  <span className="text-sm font-medium">{stats.totalEmailsSent?.toLocaleString()}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Eye className="w-4 h-4 text-teal-500" />
                    <span className="text-xs font-medium">Open Rate</span>
                  </div>
                  <span className="text-sm font-medium">{formatRate(stats.openRate || 0)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Reply className="w-4 h-4 text-green-600" />
                    <span className="text-xs font-medium">Reply Rate</span>
                  </div>
                  <span className="text-sm font-medium">{formatRate(stats.replyRate || 0)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <Activity className="w-4 h-4 text-orange-500" />
                    <span className="text-xs font-medium">Click Rate</span>
                  </div>
                  <span className="text-sm font-medium">{formatRate(stats.clickRate || 0)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <AlertTriangle className="w-4 h-4 text-red-500" />
                    <span className="text-xs font-medium">Bounce Rate</span>
                  </div>
                  <span className="text-sm font-medium">{formatRate(stats.bounceRate || 0)}</span>
                </div>
              </CardContent>
            </Card>
          </div>

          <Separator className="my-6" />

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            {/* Campaign Performance Chart - Takes 2 columns */}
            <Card className="lg:col-span-2 rounded-xl">
              <CardHeader className="pb-4">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-lg">Campaign Performance Trends</CardTitle>
                    <CardDescription className="text-sm">
                      Campaign email activity and performance rates over the last {campaignPeriod === 'all' ? 'year' : campaignPeriod === '6m' ? '6 months' : campaignPeriod === '1y' ? 'year' : `${campaignPeriod} days`} ({filteredCampaignPerformanceData.length} days)
                    </CardDescription>
                  </div>
                  <div className="flex items-center gap-1 flex-wrap">
                    <Button 
                      variant={campaignPeriod === '7' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handleCampaignPeriodChange('7')}
                      disabled={loadingCampaignPerformanceTrend}
                      className="transition-all duration-200"
                    >
                      7D
                    </Button>
                    <Button 
                      variant={campaignPeriod === '30' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handleCampaignPeriodChange('30')}
                      disabled={loadingCampaignPerformanceTrend}
                      className="transition-all duration-200"
                    >
                      30D
                    </Button>
                    <Button 
                      variant={campaignPeriod === '90' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handleCampaignPeriodChange('90')}
                      disabled={loadingCampaignPerformanceTrend}
                      className="transition-all duration-200"
                    >
                      90D
                    </Button>
                    <Button 
                      variant={campaignPeriod === '6m' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handleCampaignPeriodChange('6m')}
                      disabled={loadingCampaignPerformanceTrend}
                      className="transition-all duration-200"
                    >
                      6M
                    </Button>
                    <Button 
                      variant={campaignPeriod === '1y' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handleCampaignPeriodChange('1y')}
                      disabled={loadingCampaignPerformanceTrend}
                      className="transition-all duration-200"
                    >
                      1Y
                    </Button>
                    <Button 
                      variant={campaignPeriod === 'all' ? 'default' : 'outline'} 
                      size="sm" 
                      onClick={() => handleCampaignPeriodChange('all')}
                      disabled={loadingCampaignPerformanceTrend}
                      className="transition-all duration-200"
                    >
                      All
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="p-4">
                <div className="w-full h-64 relative">
                  {loadingCampaignPerformanceTrend && (
                    <div className="absolute inset-0 bg-white/80 backdrop-blur-sm flex items-center justify-center z-10">
                      <div className="flex items-center gap-2">
                        <div className="h-4 w-4 border-2 border-primary border-t-transparent rounded-full animate-spin"></div>
                        <span className="text-sm text-muted-foreground">Loading campaign trends...</span>
                      </div>
                    </div>
                  )}
                  <ChartContainer 
                    key={`campaign-chart-${campaignPeriod}`} 
                    config={performanceChartConfig} 
                    className="w-full h-full"
                  >
                    <ComposedChart 
                      data={filteredCampaignPerformanceData}
                      margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
                    >
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => {
                          try {
                            const date = new Date(value);
                            return isNaN(date.getTime()) ? value : date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
                          } catch (e) {
                            return value;
                          }
                        }}
                        fontSize={12}
                        interval="preserveStartEnd"
                        tick={{ fontSize: 12 }}
                      />
                      <YAxis 
                        yAxisId="count"
                        fontSize={12}
                        tick={{ fontSize: 12 }}
                      />
                      <YAxis 
                        yAxisId="rate"
                        orientation="right"
                        fontSize={12}
                        tick={{ fontSize: 12 }}
                        domain={[0, 100]}
                        tickFormatter={(value) => `${value}%`}
                      />
                      <ChartTooltip 
                        content={<ChartTooltipContent 
                          labelFormatter={(value) => {
                            try {
                              const date = new Date(value as string);
                              const dateStr = isNaN(date.getTime()) ? value as string : date.toLocaleDateString('en-US', { 
                                weekday: 'short',
                                month: 'long', 
                                day: 'numeric', 
                                year: 'numeric' 
                              });
                              return `Campaign Activity - ${dateStr}`;
                            } catch (e) {
                              return value as string;
                            }
                          }}
                        />}
                      />
                      <ChartLegend content={<ChartLegendContent />} />
                      <Line 
                        type="monotone" 
                        dataKey="emailsSent" 
                        name="Emails Sent"
                        yAxisId="count"
                        stroke="var(--color-emailsSent)" 
                        strokeWidth={3} 
                        dot={{ fill: 'var(--color-emailsSent)', strokeWidth: 2, r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="emailsOpened" 
                        name="Emails Opened"
                        yAxisId="count"
                        stroke="var(--color-emailsOpened)" 
                        strokeWidth={3} 
                        dot={{ fill: 'var(--color-emailsOpened)', strokeWidth: 2, r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="emailsReplied" 
                        name="Emails Replied"
                        yAxisId="count"
                        stroke="var(--color-emailsReplied)" 
                        strokeWidth={3} 
                        dot={{ fill: 'var(--color-emailsReplied)', strokeWidth: 2, r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="emailsBounced" 
                        name="Emails Bounced"
                        yAxisId="count"
                        stroke="var(--color-emailsBounced)" 
                        strokeWidth={2} 
                        dot={{ fill: 'var(--color-emailsBounced)', strokeWidth: 2, r: 3 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="openRate" 
                        name="Open Rate %"
                        yAxisId="rate"
                        stroke="#8B5CF6" 
                        strokeWidth={2} 
                        strokeDasharray="5 5"
                        dot={{ fill: '#8B5CF6', strokeWidth: 2, r: 3 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="replyRate" 
                        name="Reply Rate %"
                        yAxisId="rate"
                        stroke="#06B6D4" 
                        strokeWidth={2} 
                        strokeDasharray="3 3"
                        dot={{ fill: '#06B6D4', strokeWidth: 2, r: 3 }}
                      />
                    </ComposedChart>
                  </ChartContainer>
                </div>
                <div className="flex items-center justify-center mt-2">
                  <p className="text-xs text-muted-foreground">
                    📈 Campaign email counts (left axis) & rates (right axis) • Dashed lines show percentages
                  </p>
                </div>
              </CardContent>
            </Card>

            {/* Top Performing Campaigns Sidebar */}
            <Card className="rounded-xl">
              <CardHeader className="pb-3 px-3 pt-3">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-sm">
                      {campaignFilters.showWorst ? 'Worst Campaigns' : 'Top Campaigns'}
                    </CardTitle>
                    <CardDescription className="text-xs">
                      {campaignStats ? (
                        <>{campaignFilters.showWorst ? 'Worst' : 'Top'} {topFilteredCampaigns.length} campaigns</>
                      ) : (
                        'Performance ranked campaigns'
                      )}
                    </CardDescription>
                  </div>
                  <div className="flex items-center gap-2">
                    {/* Top/Worst Toggle */}
                    <div className="flex items-center gap-2">
                      <Label htmlFor="show-worst" className="text-sm font-medium">
                        {campaignFilters.showWorst ? 'Worst' : 'Top'}
                      </Label>
                      <Switch
                        id="show-worst"
                        checked={campaignFilters.showWorst}
                        onCheckedChange={(checked) => setCampaignFilters(prev => ({
                          ...prev,
                          showWorst: checked
                        }))}
                      />
                    </div>
                    
                    {/* Time Range Selector */}
                    <Select
                      value={campaignFilters.timeRangeDays.toString()}
                      onValueChange={(value) => setCampaignFilters(prev => ({
                        ...prev,
                        timeRangeDays: parseInt(value)
                      }))}
                    >
                      <SelectTrigger className="w-[110px] text-sm">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="7">Last 7 days</SelectItem>
                        <SelectItem value="30">Last 30 days</SelectItem>
                        <SelectItem value="90">Last 90 days</SelectItem>
                        <SelectItem value="9999">All time</SelectItem>
                      </SelectContent>
                    </Select>
                    {campaignStats && (
                      <div className="text-xs text-muted-foreground bg-muted px-2 py-1 rounded">
                        Avg: {campaignStats.averageReplyRate?.toFixed(1)}%
                      </div>
                    )}
                  </div>
                </div>
                
                {/* Compact Filter Controls */}
                <div className="border rounded-lg bg-card mt-1">
                  <div className="p-3 space-y-2">
                    {/* Filters Row */}
                    <div className="flex items-center justify-between text-sm">
                      {/* Left Side - Sort and Volume */}
                      <div className="flex items-center gap-2">
                        {/* Sort By */}
                        <Select
                          value={campaignFilters.sortBy}
                          onValueChange={(value) => setCampaignFilters(prev => ({
                            ...prev,
                            sortBy: value
                          }))}
                        >
                          <SelectTrigger className="w-[140px] text-sm">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="ReplyRate">Reply Rate</SelectItem>
                            <SelectItem value="OpenRate">Open Rate</SelectItem>
                            <SelectItem value="TotalSent">Volume</SelectItem>
                            <SelectItem value="TotalReplied">Replies</SelectItem>
                            <SelectItem value="BounceRate">Bounce Rate</SelectItem>
                            <SelectItem value="positivereplyrate">Positive Reply Rate</SelectItem>
                          </SelectContent>
                        </Select>
                        
                        {/* Volume Filter */}
                        <Select
                          value={campaignFilters.minimumSent.toString()}
                          onValueChange={(value) => setCampaignFilters(prev => ({
                            ...prev,
                            minimumSent: parseInt(value)
                          }))}
                        >
                          <SelectTrigger className="w-[120px] text-sm">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="0">All volumes</SelectItem>
                            <SelectItem value="10">10+ emails</SelectItem>
                            <SelectItem value="25">25+ emails</SelectItem>
                            <SelectItem value="50">50+ emails</SelectItem>
                            <SelectItem value="100">100+ emails</SelectItem>
                            <SelectItem value="250">250+ emails</SelectItem>
                            <SelectItem value="500">500+ emails</SelectItem>
                            <SelectItem value="1000">1k+ emails</SelectItem>
                            <SelectItem value="2500">2.5k+ emails</SelectItem>
                            <SelectItem value="5000">5k+ emails</SelectItem>
                            <SelectItem value="10000">10k+ emails</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      
                      {/* Right Side - Status Filter */}
                      <div className="flex items-center gap-3">
                        {/* Status Filter */}
                        <div className="flex gap-1">
                          {[
                            { value: 'ACTIVE', label: 'Active' },
                            { value: 'COMPLETED', label: 'Done' },
                            { value: 'PAUSED', label: 'Paused' }
                          ].map(({ value, label }) => (
                          <Button
                            key={value}
                            variant={campaignFilters.statuses.includes(value) ? "default" : "outline"}
                            size="sm"
                            onClick={() => setCampaignFilters(prev => ({
                              ...prev,
                              statuses: prev.statuses.includes(value)
                                ? prev.statuses.filter(s => s !== value)
                                : [...prev.statuses, value]
                            }))}
                          >
                            {label}
                          </Button>
                        ))}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </CardHeader>
              
              <CardContent className="pt-1 px-3 pb-3 max-h-64 overflow-y-auto relative">
                {loadingFilteredCampaigns && topFilteredCampaigns.length === 0 ? (
                  // Professional card skeleton matching new design
                  <div className="space-y-3">
                    {[...Array(5)].map((_, index) => (
                      <Card key={index} className="bg-card border border-border/50 rounded-xl">
                        <CardContent className="p-4">
                          <div className="flex items-start gap-4">
                            {/* Ranking Badge Skeleton */}
                            <div className="flex-shrink-0">
                              <Skeleton className="w-12 h-12 rounded-xl" />
                            </div>
                            
                            {/* Campaign Info Skeleton */}
                            <div className="flex-1 min-w-0 space-y-3">
                              {/* Header Skeleton */}
                              <div className="space-y-1">
                                <div className="flex items-center gap-2">
                                  <Skeleton className="h-4 w-32" />
                                  <Skeleton className="h-5 w-16 rounded-full" />
                                </div>
                                <Skeleton className="h-3 w-24" />
                              </div>

                              {/* Metrics Grid Skeleton */}
                              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                                {[...Array(4)].map((_, metricIndex) => (
                                  <div key={metricIndex} className="p-2 rounded-lg bg-muted/30 border border-border/30 space-y-1">
                                    <Skeleton className="h-3 w-16" />
                                    <Skeleton className="h-4 w-12" />
                                  </div>
                                ))}
                              </div>

                              {/* Volume Stats Skeleton */}
                              <div className="flex items-center gap-4 pt-2 border-t border-border/50">
                                {[...Array(3)].map((_, statIndex) => (
                                  <div key={statIndex} className="flex items-center gap-1.5">
                                    <Skeleton className="w-2 h-2 rounded-full" />
                                    <Skeleton className="h-3 w-16" />
                                  </div>
                                ))}
                              </div>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                ) : topFilteredCampaigns.length === 0 ? (
                  <div className="text-center p-4">
                    <div className="text-xs text-muted-foreground">No campaigns match filters</div>
                  </div>
                ) : (
                  <>
                    {/* Loading overlay for when data exists but is refreshing */}
                    {loadingFilteredCampaigns && (
                      <div className="absolute inset-0 bg-background/80 backdrop-blur-[1px] z-10 rounded-lg pointer-events-none">
                        <div className="flex items-center justify-center h-full">
                          <div className="text-xs text-muted-foreground bg-background px-2 py-1 rounded shadow-sm border">
                            Updating...
                          </div>
                        </div>
                      </div>
                    )}
                    <div className="space-y-3" style={{ minHeight: topFilteredCampaigns.length > 0 ? 'auto' : '180px' }}>
                      {topFilteredCampaigns.map((campaign, index) => (
                        <Card key={campaign.id} className="group hover:shadow-lg hover:border-primary/20 transition-all duration-200 bg-card border border-border/50 rounded-xl">
                          <CardContent className="p-4">
                            <div className="flex items-start gap-4">
                              {/* Ranking Badge */}
                              <div className="flex-shrink-0">
                                <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-primary/10 to-primary/5 border border-primary/20 flex items-center justify-center shadow-sm group-hover:shadow-md transition-shadow">
                                  <span className="text-primary font-bold text-sm">#{index + 1}</span>
                                </div>
                              </div>
                              
                              {/* Campaign Info */}
                              <div className="flex-1 min-w-0 space-y-3">
                                {/* Header */}
                                <div className="space-y-1">
                                  <div className="flex items-center gap-2">
                                    <h4 className="font-semibold text-sm text-foreground truncate">{campaign.name}</h4>
                                    <Badge
                                      className={getStatusColor(campaign.status)}
                                      variant="outline"
                                    >
                                      {campaign.status}
                                    </Badge>
                                  </div>
                                  <p className="text-xs text-muted-foreground truncate">
                                    {campaign.clientName}
                                  </p>
                                </div>

                                {/* Metrics Grid */}
                                <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                                  <div className="p-2 rounded-lg bg-muted/50 border space-y-1">
                                    <div className="text-xs font-medium text-muted-foreground">Reply Rate</div>
                                    <div className="text-sm font-bold text-green-600 dark:text-green-500">{formatRate(campaign.replyRate)}</div>
                                  </div>

                                  {campaign.openRate !== undefined && (
                                    <div className="p-2 rounded-lg bg-muted/50 border space-y-1">
                                      <div className="text-xs font-medium text-muted-foreground">Open Rate</div>
                                      <div className="text-sm font-bold text-blue-600 dark:text-blue-500">{formatRate(campaign.openRate)}</div>
                                    </div>
                                  )}

                                  {campaign.bounceRate !== undefined && (
                                    <div className="p-2 rounded-lg bg-muted/50 border space-y-1">
                                      <div className="text-xs font-medium text-muted-foreground">Bounce Rate</div>
                                      <div className="text-sm font-bold text-red-600 dark:text-red-500">{formatRate(campaign.bounceRate)}</div>
                                    </div>
                                  )}

                                  {campaign.positiveReplyRate !== undefined && (
                                    <div className="p-2 rounded-lg bg-muted/50 border space-y-1">
                                      <div className="text-xs font-medium text-muted-foreground">Positive Replies</div>
                                      <div className="text-sm font-bold text-emerald-600 dark:text-emerald-500">{formatRate(campaign.positiveReplyRate)}</div>
                                    </div>
                                  )}

                                </div>

                                {/* Volume Stats */}
                                <div className="flex items-center gap-4 pt-2 border-t border-border/50">
                                  <div className="flex items-center gap-1.5">
                                    <div className="w-2 h-2 rounded-full bg-blue-500"></div>
                                    <span className="text-xs font-medium text-foreground">{formatNumber(campaign.totalSent)} sent</span>
                                  </div>
                                  <div className="flex items-center gap-1.5">
                                    <div className="w-2 h-2 rounded-full bg-green-500"></div>
                                    <span className="text-xs font-medium text-foreground">{formatNumber(campaign.totalReplied)} replies</span>
                                  </div>
                                  {campaign.totalOpened !== undefined && (
                                    <div className="flex items-center gap-1.5">
                                      <div className="w-2 h-2 rounded-full bg-purple-500"></div>
                                      <span className="text-xs font-medium text-foreground">{formatNumber(campaign.totalOpened)} opens</span>
                                    </div>
                                  )}
                                  {campaign.totalBounced !== undefined && campaign.totalBounced > 0 && (
                                    <div className="flex items-center gap-1.5">
                                      <div className="w-2 h-2 rounded-full bg-red-500"></div>
                                      <span className="text-xs font-medium text-foreground">{formatNumber(campaign.totalBounced)} bounced</span>
                                    </div>
                                  )}
                                </div>
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      ))}
                    </div>
                  </>
                )}
              </CardContent>
            </Card>

          </div>

          {/* Detailed Analytics Tabs */}
          <Tabs defaultValue="clients" className="space-y-3">
            <TabsList className={`grid w-full h-9 ${isAdmin ? 'grid-cols-3' : 'grid-cols-2'}`}>
              <TabsTrigger value="clients">Client Stats</TabsTrigger>
              {isAdmin && (
                <TabsTrigger value="users">User Stats</TabsTrigger>
              )}
              <TabsTrigger value="accounts">Email Accounts</TabsTrigger>
            </TabsList>


<TabsContent value="clients" className="space-y-4">
  <ClientStats 
    selectedClients={selectedClients.length > 0 ? selectedClients : undefined}
    campaignScope={campaignScope}
  />
</TabsContent>

            <TabsContent value="accounts" className="space-y-4">
              {/* Email Account Analytics - Reorganized Layout */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Account Status Chart */}
                <Card className="h-full rounded-xl">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-lg">Account Status Distribution</CardTitle>
                    <CardDescription className="text-sm">Current status of all email accounts{realEmailAccountSummary ? ' (Real-time data)' : ''}</CardDescription>
                  </CardHeader>
                  <CardContent className="p-4">
                    <div className="w-full h-64">
                      {emailAccountSummaryLoading ? (
                        <div className="flex items-center justify-center h-full">
                          <Skeleton className="h-8 w-8 rounded-full" />
                        </div>
                      ) : (
                        <ChartContainer config={emailAccountChartConfig} className="w-full h-full">
                        <BarChart 
                          data={
                            Object.entries(currentEmailAccountSummary.accountsByStatus).map(([status, data]) => ({
                              name: status === '' ? 'Unknown' : status.charAt(0).toUpperCase() + status.slice(1).toLowerCase(),
                              value: data.count,
                              percentage: data.percentage,
                              fill: status === 'ACTIVE' ? 'var(--color-active)' : 
                                   status === 'INACTIVE' ? 'var(--color-issues)' : 
                                   'var(--color-paused)'
                            }))
                          }
                          margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
                        >
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis 
                            dataKey="name" 
                            fontSize={11}
                            tick={{ fontSize: 11 }}
                          />
                          <YAxis fontSize={11} tick={{ fontSize: 11 }} />
                          <ChartTooltip content={<ChartTooltipContent 
                            formatter={(value, name, props) => [
                              `${value} accounts (${props.payload?.percentage?.toFixed(1)}%)`,
                              name
                            ]}
                          />} />
                          <Bar dataKey="value" radius={4}>
                            {[
                              { name: 'Active', fill: 'var(--color-active)' },
                              { name: 'Warming Up', fill: 'var(--color-warming)' },
                              { name: 'Paused', fill: 'var(--color-paused)' },
                              { name: 'Issues', fill: 'var(--color-issues)' }
                            ].map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Bar>
                        </BarChart>
                        </ChartContainer>
                      )}
                    </div>
                  </CardContent>
                </Card>

                {/* Email Providers Chart */}
                <Card className="h-full rounded-xl">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-lg">Email Providers Distribution</CardTitle>
                    <CardDescription className="text-sm">Distribution by domain type</CardDescription>
                  </CardHeader>
                  <CardContent className="p-4">
                    <div className="w-full h-64">
                      {emailAccountSummaryLoading ? (
                        <div className="flex items-center justify-center h-full">
                          <Skeleton className="h-8 w-8 rounded-full" />
                        </div>
                      ) : (
                        <ChartContainer config={emailAccountChartConfig} className="w-full h-full">
                        <PieChart margin={{ top: 20, right: 30, left: 20, bottom: 20 }}>
                          <Pie
                            data={(() => {
                              const providers = Object.entries(currentEmailAccountSummary.accountsByProvider || {});
                              const totalAccounts = currentEmailAccountSummary.totalAccounts || 1;
                              
                              // Debug: Log what provider data we're actually using
                              console.log('Provider data being displayed:', providers);
                              console.log('Total accounts:', totalAccounts);
                              
                              // Sort by count descending
                              const sortedProviders = providers.sort(([,a], [,b]) => b - a);
                              
                              // Show top 8 providers, group rest as "Other"
                              const topProviders = sortedProviders.slice(0, 8);
                              const otherProviders = sortedProviders.slice(8);
                              const otherCount = otherProviders.reduce((sum, [,count]) => sum + count, 0);
                              
                              const displayProviders = [
                                ...topProviders,
                                ...(otherCount > 0 ? [['other', otherCount] as [string, number]] : [])
                              ];
                              
                              return displayProviders.map(([provider, count], index) => {
                                const percentage = (count / totalAccounts) * 100;
                                const displayName = provider === 'other' ? 'Other' : 
                                                 provider === 'com' ? 'Commercial (.com)' :
                                                 provider === 'org' ? 'Organization (.org)' :
                                                 provider === 'co' ? 'Company (.co)' :
                                                 provider === 'net' ? 'Network (.net)' :
                                                 provider === 'info' ? 'Info (.info)' :
                                                 provider.charAt(0).toUpperCase() + provider.slice(1);
                                
                                return {
                                  name: displayName,
                                  value: count,
                                  percentage: percentage,
                                  fill: COLORS[index % COLORS.length]
                                };
                              });
                            })()}
                            dataKey="value"
                            cx="50%"
                            cy="50%"
                            innerRadius={40}
                            outerRadius={80}
                            paddingAngle={2}
                            label={({ name, percentage }) => `${name} (${percentage.toFixed(1)}%)`}
                          >
                            {Object.entries(currentEmailAccountSummary.accountsByProvider || {}).map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                            ))}
                          </Pie>
                          <ChartTooltip content={<ChartTooltipContent 
                            formatter={(value, name, props) => [
                              `${value} accounts (${props.payload?.percentage?.toFixed(1)}%)`,
                              name
                            ]}
                          />} />
                          <ChartLegend 
                            content={<ChartLegendContent />}
                            wrapperStyle={{ paddingTop: '10px', fontSize: '12px' }}
                          />
                        </PieChart>
                        </ChartContainer>
                      )}
                    </div>
                  </CardContent>
                </Card>
              </div>

            </TabsContent>

            {isAdmin && (
              <TabsContent value="users" className="space-y-4">
                <UserStats />
              </TabsContent>
            )}

          </Tabs>
        </div>
      </div>
    </ProtectedRoute>
  );
}