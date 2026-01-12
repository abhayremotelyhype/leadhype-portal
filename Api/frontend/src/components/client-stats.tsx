'use client';

import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '@/contexts/auth-context';
import { BarChart, TrendingUp, Mail, Reply, CheckCircle, Clock, Users, Target, ChevronDown, ArrowUp, ArrowDown, Building2, User, Activity, Filter } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { 
  Pagination, 
  PaginationContent, 
  PaginationItem, 
  PaginationLink, 
  PaginationNext, 
  PaginationPrevious,
  PaginationEllipsis
} from '@/components/ui/pagination';
import { DualSortHeader } from '@/components/dual-sort-header';
import { UserSearchFilter } from '@/components/user-search-filter';
import { MultiSortConfig } from '@/types';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { toast } from 'sonner';

interface EngagementStats {
  totalSent: number;
  totalReplies: number;
  positiveReplies: number;
  emailsPerReply: number;
  emailsPerPositiveReply: number;
  repliesPerPositiveReply: number;
  positiveReplyPercentage: number;
  replyRate: number;
}

interface ReplyTiming {
  lastReplyAt?: string;
  lastPositiveReplyAt?: string;
  lastContactedAt?: string;
  lastReplyRelative?: string;
  lastPositiveReplyRelative?: string;
  lastContactedRelative?: string;
}

interface ClientInfo {
  id: string;
  name: string;
  company?: string;
  color: string;
  status: string;
  campaignCount: number;
  activeCampaignCount: number;
  emailAccountCount: number;
}

interface ClientStats {
  client: ClientInfo;
  stats: EngagementStats;
  timing: ReplyTiming;
}

interface PaginationInfo {
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

interface ClientStatusSummary {
  activeClients: number;
  inactiveClients: number;
  totalClients: number;
  totalCampaigns: number;
  activeCampaigns: number;
}

interface ClientStatsResponse {
  clients: ClientStats[];
  totalCount: number;
  generatedAt: string;
  pagination: PaginationInfo;
  aggregatedStats?: EngagementStats;
  aggregatedTiming?: ReplyTiming;
  clientStatusSummary?: ClientStatusSummary;
}

interface ClientStatsProps {
  selectedClients?: string[];
  startDate?: Date;
  endDate?: Date;
  campaignScope?: 'all' | 'specific';
}

export function ClientStats({ selectedClients, startDate, endDate, campaignScope = 'specific' }: ClientStatsProps) {
  const { user, isAdmin } = useAuth();
  const [statsData, setStatsData] = useState<ClientStatsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [sorting, setSorting] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(() => {
    // Remember page size from localStorage
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('client-stats-page-size');
      return saved ? parseInt(saved, 10) : 20;
    }
    return 20;
  });
  const [multiSort, setMultiSort] = useState<MultiSortConfig>({ sorts: [] });
  const [clientStatusFilter, setClientStatusFilter] = useState(() => {
    // Remember status filter from localStorage
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('client-stats-status-filter');
      return saved || 'active';
    }
    return 'active';
  });
  const [selectedUserId, setSelectedUserId] = useState<string | null>(() => {
    // Start with null by default - don't restore from localStorage on initial load
    // This ensures all clients are shown by default
    return null;
  });

  // User filter always starts as null on component mount (no persistence)

  const loadStats = useCallback(async (page: number = 1, isSort: boolean = false) => {
    try {
      if (isSort) {
        setSorting(true);
      } else {
        setLoading(true);
      }
      
      // Build sort parameters from multiSort state
      let sortBy = 'name';
      let sortDescending = false;
      
      if (multiSort.sorts.length > 0) {
        const primarySort = multiSort.sorts[0];
        sortBy = primarySort.column;
        sortDescending = primarySort.direction === 'desc';
      }
      
      // Build filter request object for POST to avoid header size issues
      const filterRequest = {
        page: page,
        pageSize: pageSize,
        sortBy,
        sortDescending,
        clientIds: selectedClients && selectedClients.length > 0 ? selectedClients : null,
        startDate: startDate?.toISOString(),
        endDate: endDate?.toISOString(),
        clientStatus: clientStatusFilter && clientStatusFilter !== 'all' ? clientStatusFilter : null,
        filterByUserId: selectedUserId || null
      };
      
      // Use POST request to send filter data in body instead of query parameters
      const response = await apiClient.post<ClientStatsResponse>(
        '/api/v1/clients/stats',
        filterRequest
      );
      setStatsData(response);
      setCurrentPage(page);
    } catch (error: any) {
        console.error('Failed to load client stats:', error);
        
        if (error.isAuthError) {
          toast.error('Authentication required to view client statistics');
        } else if (error.status === 403) {
          toast.error('Insufficient permissions to view client statistics');
        } else if (error.isNetworkError) {
          toast.error('Unable to connect to the server');
        } else {
          toast.error('Failed to load client statistics');
        }
      } finally {
        setLoading(false);
        setSorting(false);
      }
  }, [pageSize, multiSort, selectedClients, startDate, endDate, clientStatusFilter, selectedUserId]);

  // Handler for page size changes
  const handlePageSizeChange = useCallback((newPageSize: string) => {
    const size = parseInt(newPageSize, 10);
    setPageSize(size);
    setCurrentPage(1); // Reset to first page when changing page size
    
    // Save to localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem('client-stats-page-size', newPageSize);
    }
  }, []);

  // Handler for status filter changes
  const handleStatusFilterChange = useCallback((newStatus: string) => {
    setClientStatusFilter(newStatus);
    setCurrentPage(1); // Reset to first page when changing filter
    
    // Save to localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem('client-stats-status-filter', newStatus);
    }
  }, []);

  // Handler for user filter changes
  const handleUserFilterChange = useCallback(async (userId: string | null) => {
    setSelectedUserId(userId);
    setCurrentPage(1); // Reset to first page when changing filter
    
    // Don't persist user filter to localStorage - always reset on refresh
    // This ensures all clients are shown by default after refresh
    
    // Immediately call loadStats with the new user ID to avoid stale state
    setLoading(true);
    try {
      // Build the request with the new user ID directly
      const filterRequest = {
        page: 1,
        pageSize: pageSize,
        sortBy: Object.keys(multiSort.sorts)[0] || 'name',
        sortDescending: Object.values(multiSort.sorts)[0]?.direction === 'desc' || false,
        clientIds: selectedClients && selectedClients.length > 0 ? selectedClients : null,
        startDate: startDate?.toISOString(),
        endDate: endDate?.toISOString(),
        clientStatus: clientStatusFilter && clientStatusFilter !== 'all' ? clientStatusFilter : null,
        filterByUserId: userId || null // Use the new userId directly
      };
      
      const response = await apiClient.post<ClientStatsResponse>(
        '/api/v1/clients/stats',
        filterRequest
      );
      
      setStatsData(response);
      setCurrentPage(1);
    } catch (error: any) {
      console.error('Failed to load client stats:', error);
      toast.error('Failed to load client stats: ' + (error.response?.data?.message || error.message));
      setStatsData(null);
    } finally {
      setLoading(false);
    }
  }, [pageSize, multiSort, selectedClients, startDate, endDate, clientStatusFilter, apiClient]);

  const handleSort = useCallback((column: string, event?: React.MouseEvent) => {
    const isShiftHeld = event?.shiftKey || false;
    const isCtrlHeld = event?.ctrlKey || event?.metaKey || false;
    
    setMultiSort(prevSort => {
      const existingSortIndex = prevSort.sorts.findIndex(s => s.column === column);
      
      if (existingSortIndex >= 0) {
        // Column is already sorted
        const existingSort = prevSort.sorts[existingSortIndex];
        const newSorts = [...prevSort.sorts];
        
        if (isCtrlHeld) {
          // Ctrl/Cmd+click removes the sort
          newSorts.splice(existingSortIndex, 1);
          return { sorts: newSorts };
        }
        
        if (existingSort.direction === 'asc') {
          // Change to desc
          newSorts[existingSortIndex] = { ...existingSort, direction: 'desc' };
        } else {
          // Change back to asc
          newSorts[existingSortIndex] = { ...existingSort, direction: 'asc' };
        }
        
        return { sorts: newSorts };
      } else {
        // Add new sort
        const newSort = { column, direction: 'asc' as const, mode: 'count' as const };
        
        if (isShiftHeld && prevSort.sorts.length > 0) {
          // Multi-sort: add to existing sorts
          return { sorts: [...prevSort.sorts, newSort] };
        } else {
          // Single sort: replace existing sorts
          return { sorts: [newSort] };
        }
      }
    });
    
    // Reset to first page when sorting changes
    setCurrentPage(1);
    
    // Trigger reload with sorting flag
    loadStats(1, true);
  }, [loadStats]);

  // Get aggregated stats from backend (calculated efficiently across ALL data, not just current page)
  const getAggregatedStats = useCallback((): { stats: EngagementStats; timing: ReplyTiming } | null => {
    if (!statsData) {
      return null;
    }
    
    // Only show combined stats if we have aggregated data from backend and multiple clients exist
    if (!statsData.aggregatedStats || !statsData.aggregatedTiming || statsData.totalCount <= 1) {
      return null;
    }
    
    // Only show combined stats if multiple clients are selected OR if no clients are specifically selected (showing all)
    if (selectedClients && selectedClients.length === 1) {
      return null;
    }

    return {
      stats: statsData.aggregatedStats,
      timing: statsData.aggregatedTiming
    };
  }, [statsData, selectedClients]);

  // Helper function to get relative time
  const getRelativeTime = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 24) {
      return `${diffInHours}h ago`;
    } else {
      const diffInDays = Math.floor(diffInHours / 24);
      return `${diffInDays}d ago`;
    }
  };

  useEffect(() => {
    if (user) {
      loadStats(currentPage);
    } else {
      setLoading(false);
    }
  }, [user, currentPage, loadStats, multiSort]);

  if (loading) {
    return (
      <Card className="rounded-xl">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="w-5 h-5" />
            Client Statistics
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="flex items-center justify-between p-3 border rounded">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-8 w-8 rounded" />
                  <div>
                    <Skeleton className="h-4 w-32 mb-1" />
                    <Skeleton className="h-3 w-24" />
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-20" />
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!user) {
    return (
      <Card className="rounded-xl">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="w-5 h-5" />
            Client Statistics
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            <Users className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>Please log in to view client statistics</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!statsData || statsData.clients.length === 0) {
    return (
      <Card className="rounded-xl">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <BarChart className="w-5 h-5" />
                Client Statistics
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                Last updated: {statsData?.generatedAt ? new Date(statsData.generatedAt).toLocaleString() : 'N/A'}
                {selectedUserId && (
                  <span className="ml-2 text-blue-600">
                    • Filtered by user
                  </span>
                )}
              </p>
            </div>
            <div className={`grid gap-4 ${isAdmin ? 'grid-cols-[auto_1fr_auto]' : 'grid-cols-[auto_auto]'} items-center`}>
              <div className="flex items-center gap-2">
                <Label htmlFor="client-status-filter" className="text-sm font-medium whitespace-nowrap">
                  Status:
                </Label>
                <Select
                  value={clientStatusFilter}
                  onValueChange={handleStatusFilterChange}
                >
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Clients</SelectItem>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              {isAdmin && (
                <div className="flex items-center gap-2 justify-self-center max-w-sm">
                  <Label htmlFor="user-filter" className="text-sm font-medium whitespace-nowrap">
                    User:
                  </Label>
                  <UserSearchFilter
                    selectedUserId={selectedUserId}
                    onSelectionChange={handleUserFilterChange}
                    placeholder="All users"
                    variant="compact"
                    className="w-full min-w-0"
                  />
                </div>
              )}
              <div className="flex items-center gap-2 justify-self-end">
                <Label htmlFor="page-size-filter" className="text-sm font-medium whitespace-nowrap">
                  Show:
                </Label>
                <Select
                  value={pageSize.toString()}
                  onValueChange={handlePageSizeChange}
                >
                  <SelectTrigger className="w-20">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="10">10</SelectItem>
                    <SelectItem value="20">20</SelectItem>
                    <SelectItem value="50">50</SelectItem>
                    <SelectItem value="100">100</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            <BarChart className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>No client statistics available yet</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const getStatusBadge = (status: string) => {
    const variant = status.toLowerCase() === 'active' ? 'default' : 'secondary';
    return <Badge variant={variant} className="text-xs pointer-events-none">{status.toUpperCase()}</Badge>;
  };

  const EngagementSortHeader = () => {
    const getEngagementSortInfo = () => {
      const sortItem = multiSort.sorts.find(s => 
        ['totalsent', 'totalreplies', 'positivereplies'].includes(s.column)
      );
      
      if (!sortItem) return { column: null, direction: null, label: 'Engagement' };
      
      const labels = {
        'totalsent': 'Sent',
        'totalreplies': 'Replies', 
        'positivereplies': 'Positive Replies'
      };
      
      return {
        column: sortItem.column,
        direction: sortItem.direction,
        label: labels[sortItem.column as keyof typeof labels] || 'Engagement'
      };
    };

    const { column, direction, label } = getEngagementSortInfo();
    const isActive = column !== null;

    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button 
            className="flex items-center gap-1 w-full px-1 py-2 text-left ui-table-header hover:bg-muted/30 group justify-between"
          >
            <div className="flex items-center gap-2">
              <span>{label}</span>
              {isActive && (
                <div className="flex items-center">
                  {direction === 'asc' ? 
                    <ArrowUp className="w-3 h-3 text-primary" /> : 
                    <ArrowDown className="w-3 h-3 text-primary" />
                  }
                </div>
              )}
            </div>
            <ChevronDown className="w-3 h-3 opacity-50" />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-40">
          <DropdownMenuItem 
            onClick={() => handleSort('totalsent')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Mail className="w-3 h-3" />
              <span>Sent</span>
            </div>
            {column === 'totalsent' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('totalreplies')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Reply className="w-3 h-3" />
              <span>Replies</span>
            </div>
            {column === 'totalreplies' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('positivereplies')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <CheckCircle className="w-3 h-3" />
              <span>Positive Replies</span>
            </div>
            {column === 'positivereplies' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  };

  const PerformanceSortHeader = () => {
    const getPerformanceSortInfo = () => {
      const sortItem = multiSort.sorts.find(s => 
        ['emailsperreply', 'emailsperpositivereply', 'positivereplypercentage', 'replyrate'].includes(s.column)
      );
      
      if (!sortItem) return { column: null, direction: null, label: 'Performance' };
      
      const labels = {
        'emailsperreply': 'Emails/Reply',
        'emailsperpositivereply': 'Emails/Positive',
        'positivereplypercentage': 'Positive %',
        'replyrate': 'Reply Rate %'
      };
      
      return {
        column: sortItem.column,
        direction: sortItem.direction,
        label: labels[sortItem.column as keyof typeof labels] || 'Performance'
      };
    };

    const { column, direction, label } = getPerformanceSortInfo();
    const isActive = column !== null;

    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button 
            className="flex items-center gap-1 w-full px-1 py-2 text-left ui-table-header hover:bg-muted/30 group justify-between"
          >
            <div className="flex items-center gap-2">
              <span>{label}</span>
              {isActive && (
                <div className="flex items-center">
                  {direction === 'asc' ? 
                    <ArrowUp className="w-3 h-3 text-primary" /> : 
                    <ArrowDown className="w-3 h-3 text-primary" />
                  }
                </div>
              )}
            </div>
            <ChevronDown className="w-3 h-3 opacity-50" />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-44">
          <DropdownMenuItem 
            onClick={() => handleSort('emailsperreply')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Mail className="w-3 h-3" />
              <span>Emails per Reply</span>
            </div>
            {column === 'emailsperreply' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('emailsperpositivereply')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <TrendingUp className="w-3 h-3" />
              <span>Emails per Positive</span>
            </div>
            {column === 'emailsperpositivereply' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('positivereplypercentage')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <CheckCircle className="w-3 h-3" />
              <span>Positive Reply %</span>
            </div>
            {column === 'positivereplypercentage' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('replyrate')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Reply className="w-3 h-3" />
              <span>Reply Rate %</span>
            </div>
            {column === 'replyrate' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  };

  const ClientSortHeader = () => {
    const getClientSortInfo = () => {
      const sortItem = multiSort.sorts.find(s => 
        ['name', 'company', 'status', 'campaigncount', 'activecampaigncount', 'emailaccountcount'].includes(s.column)
      );
      
      if (!sortItem) return { column: null, direction: null, label: 'Client' };
      
      const labels = {
        'name': 'Name',
        'company': 'Company',
        'status': 'Status',
        'campaigncount': 'Campaigns',
        'activecampaigncount': 'Active Campaigns',
        'emailaccountcount': 'Email Accounts'
      };
      
      return {
        column: sortItem.column,
        direction: sortItem.direction,
        label: labels[sortItem.column as keyof typeof labels] || 'Client'
      };
    };

    const { column, direction, label } = getClientSortInfo();
    const isActive = column !== null;

    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button 
            className="flex items-center gap-1 w-full px-1 py-2 text-left ui-table-header hover:bg-muted/30 group justify-between"
          >
            <div className="flex items-center gap-2">
              <span>{label}</span>
              {isActive && (
                <div className="flex items-center">
                  {direction === 'asc' ? 
                    <ArrowUp className="w-3 h-3 text-primary" /> : 
                    <ArrowDown className="w-3 h-3 text-primary" />
                  }
                </div>
              )}
            </div>
            <ChevronDown className="w-3 h-3 opacity-50" />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-44">
          <DropdownMenuItem 
            onClick={() => handleSort('name')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <User className="w-3 h-3" />
              <span>Name</span>
            </div>
            {column === 'name' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('company')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Building2 className="w-3 h-3" />
              <span>Company</span>
            </div>
            {column === 'company' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('status')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <CheckCircle className="w-3 h-3" />
              <span>Status</span>
            </div>
            {column === 'status' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('campaigncount')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Target className="w-3 h-3" />
              <span>Campaigns</span>
            </div>
            {column === 'campaigncount' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('activecampaigncount')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Activity className="w-3 h-3" />
              <span>Active Campaigns</span>
            </div>
            {column === 'activecampaigncount' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSort('emailaccountcount')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Mail className="w-3 h-3" />
              <span>Email Accounts</span>
            </div>
            {column === 'emailaccountcount' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  };

  return (
    <div className="space-y-6">
      {/* Client Statistics Table */}
      <Card className="rounded-xl">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <BarChart className="w-5 h-5" />
                Client Statistics
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                Last updated: {new Date(statsData.generatedAt).toLocaleString()}
                {selectedUserId && (
                  <span className="ml-2 text-blue-600">
                    • Filtered by user
                  </span>
                )}
              </p>
            </div>
            <div className={`grid gap-4 ${isAdmin ? 'grid-cols-[auto_1fr_auto]' : 'grid-cols-[auto_auto]'} items-center`}>
              <div className="flex items-center gap-2">
                <Label htmlFor="client-status-filter" className="text-sm font-medium whitespace-nowrap">
                  Status:
                </Label>
                <Select
                  value={clientStatusFilter}
                  onValueChange={handleStatusFilterChange}
                >
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Clients</SelectItem>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              {isAdmin && (
                <div className="flex items-center gap-2 justify-self-center max-w-sm">
                  <Label htmlFor="user-filter" className="text-sm font-medium whitespace-nowrap">
                    User:
                  </Label>
                  <UserSearchFilter
                    selectedUserId={selectedUserId}
                    onSelectionChange={handleUserFilterChange}
                    placeholder="All users"
                    variant="compact"
                    className="w-full min-w-0"
                  />
                </div>
              )}
              <div className="flex items-center gap-2 justify-self-end">
                <Label htmlFor="page-size-filter" className="text-sm font-medium whitespace-nowrap">
                  Show:
                </Label>
                <Select
                  value={pageSize.toString()}
                  onValueChange={handlePageSizeChange}
                >
                  <SelectTrigger className="w-20">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="10">10</SelectItem>
                    <SelectItem value="20">20</SelectItem>
                    <SelectItem value="50">50</SelectItem>
                    <SelectItem value="100">100</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="relative rounded-lg border">
            {sorting && (
              <div className="absolute inset-0 bg-background/50 backdrop-blur-[0.5px] rounded-lg z-10 flex items-center justify-center">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
                  Sorting...
                </div>
              </div>
            )}
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <ClientSortHeader />
                  </TableHead>
                  <TableHead className="hidden sm:table-cell">
                    <EngagementSortHeader />
                  </TableHead>
                  <TableHead className="hidden md:table-cell">
                    <DualSortHeader
                      column="lastreply"
                      label="Last Reply"
                      multiSort={multiSort}
                      onSort={handleSort}
                    />
                  </TableHead>
                  <TableHead className="hidden lg:table-cell">
                    <DualSortHeader
                      column="lastpositivereply"
                      label="Last Positive Reply"
                      multiSort={multiSort}
                      onSort={handleSort}
                    />
                  </TableHead>
                  <TableHead className="hidden xl:table-cell">
                    <DualSortHeader
                      column="lastcontacted"
                      label="Last Contacted"
                      multiSort={multiSort}
                      onSort={handleSort}
                    />
                  </TableHead>
                  <TableHead className="text-right">
                    <PerformanceSortHeader />
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody className={`transition-opacity duration-300 ${sorting ? 'opacity-60 pointer-events-none' : 'opacity-100'}`}>
                {/* Summary row for multiple clients */}
                {(() => {
                  const aggregatedStats = getAggregatedStats();
                  if (aggregatedStats) {
                    return (
                      <TableRow className="bg-muted/50 border-b-2 font-medium">
                        <TableCell>
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-white text-sm font-medium">
                              Σ
                            </div>
                            <div>
                              <div className="font-semibold">Combined Total</div>
                              <div className="text-sm text-muted-foreground">
                                {selectedClients && selectedClients.length > 0 
                                  ? `${selectedClients.length} selected clients`
                                  : `${statsData.totalCount} total clients`
                                }
                              </div>
                              {statsData.clientStatusSummary && (
                                <div className="text-xs text-muted-foreground mt-1">
                                  {statsData.clientStatusSummary.activeCampaigns} active campaigns
                                </div>
                              )}
                            </div>
                          </div>
                        </TableCell>

                        <TableCell className="hidden sm:table-cell">
                          <div className="space-y-1">
                            <div className="flex items-center gap-2 text-sm font-medium">
                              <Mail className="w-4 h-4 text-muted-foreground" />
                              <span>{aggregatedStats.stats.totalSent.toLocaleString()} sent</span>
                            </div>
                            <div className="flex items-center gap-2 text-sm font-medium">
                              <Reply className="w-4 h-4 text-muted-foreground" />
                              <span>{aggregatedStats.stats.totalReplies.toLocaleString()} replies</span>
                            </div>
                            <div className="flex items-center gap-2 text-sm font-medium">
                              <CheckCircle className="w-4 h-4 text-green-600" />
                              <span className="text-green-600">{aggregatedStats.stats.positiveReplies.toLocaleString()} positive</span>
                            </div>
                          </div>
                        </TableCell>

                        <TableCell className="hidden md:table-cell">
                          <div className="flex items-center gap-2">
                            <Clock className="w-4 h-4 text-muted-foreground" />
                            <span className="text-sm font-medium">
                              {aggregatedStats.timing.lastReplyRelative || 'No replies yet'}
                            </span>
                          </div>
                        </TableCell>

                        <TableCell className="hidden lg:table-cell">
                          <div className="flex items-center gap-2">
                            <TrendingUp className="w-4 h-4 text-green-600" />
                            <span className="text-sm font-medium">
                              {aggregatedStats.timing.lastPositiveReplyRelative || 'No positive replies yet'}
                            </span>
                          </div>
                        </TableCell>

                        <TableCell className="hidden xl:table-cell">
                          <div className="flex items-center gap-2">
                            <Activity className="w-4 h-4 text-blue-600" />
                            <span className="text-sm font-medium">
                              {aggregatedStats.timing.lastContactedRelative || 'No contacts yet'}
                            </span>
                          </div>
                        </TableCell>

                        <TableCell className="text-right">
                          <div className="space-y-1">
                            <div className="text-sm font-semibold">
                              {aggregatedStats.stats.totalReplies > 0 && !isNaN(aggregatedStats.stats.replyRate)
                                ? `${aggregatedStats.stats.replyRate.toFixed(1)}% reply rate`
                                : 'No replies'
                              }
                            </div>
                            <div className="text-sm font-medium text-muted-foreground">
                              {aggregatedStats.stats.totalReplies > 0 && !isNaN(aggregatedStats.stats.emailsPerReply)
                                ? `${Math.round(aggregatedStats.stats.emailsPerReply).toLocaleString()} sent to get 1 reply`
                                : 'No replies'
                              }
                            </div>
                            <div className="text-sm font-medium text-muted-foreground">
                              {aggregatedStats.stats.positiveReplies > 0 && !isNaN(aggregatedStats.stats.repliesPerPositiveReply)
                                ? `${Math.round(aggregatedStats.stats.repliesPerPositiveReply).toLocaleString()} replies to get 1 positive reply`
                                : 'No positive replies'
                              }
                            </div>
                            <div className="text-xs font-medium text-muted-foreground">
                              {aggregatedStats.stats.positiveReplies > 0 && !isNaN(aggregatedStats.stats.positiveReplyPercentage)
                                ? `${aggregatedStats.stats.positiveReplyPercentage.toFixed(1)}% positive rate`
                                : 'No positive replies'
                              }
                            </div>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  }
                  return null;
                })()}

                {statsData.clients.map((clientStat) => (
                  <TableRow key={clientStat.client.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div 
                          className="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-medium"
                          style={{ backgroundColor: clientStat.client.color }}
                        >
                          {clientStat.client.name.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <div className="font-semibold text-base">{clientStat.client.name}</div>
                            {getStatusBadge(clientStat.client.status)}
                          </div>
                          <div className="text-sm text-muted-foreground">
                            {clientStat.client.company || 'No company'}
                          </div>
                          <div className="flex items-center gap-3 mt-2 text-xs text-muted-foreground">
                            <div className="flex items-center gap-1">
                              <Target className="w-3 h-3" />
                              <span>{clientStat.client.activeCampaignCount} active, {clientStat.client.campaignCount} total campaigns</span>
                            </div>
                            <div className="flex items-center gap-1">
                              <Mail className="w-3 h-3" />
                              <span>{clientStat.client.emailAccountCount} accounts</span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="hidden sm:table-cell">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2 text-sm">
                          <Mail className="w-4 h-4 text-muted-foreground" />
                          <span>{clientStat.stats.totalSent.toLocaleString()} sent</span>
                        </div>
                        <div className="flex items-center gap-2 text-sm">
                          <Reply className="w-4 h-4 text-muted-foreground" />
                          <span>{clientStat.stats.totalReplies.toLocaleString()} replies</span>
                        </div>
                        <div className="flex items-center gap-2 text-sm">
                          <CheckCircle className="w-4 h-4 text-green-600" />
                          <span className="text-green-600">{clientStat.stats.positiveReplies.toLocaleString()} positive</span>
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="hidden md:table-cell">
                      <div className="flex items-center gap-2">
                        <Clock className="w-4 h-4 text-muted-foreground" />
                        <span className="text-sm">
                          {clientStat.timing.lastReplyRelative || 'No replies yet'}
                        </span>
                      </div>
                    </TableCell>

                    <TableCell className="hidden lg:table-cell">
                      <div className="flex items-center gap-2">
                        <TrendingUp className="w-4 h-4 text-green-600" />
                        <span className="text-sm">
                          {clientStat.timing.lastPositiveReplyRelative || 'No positive replies yet'}
                        </span>
                      </div>
                    </TableCell>

                    <TableCell className="hidden xl:table-cell">
                      <div className="flex items-center gap-2">
                        <Activity className="w-4 h-4 text-blue-600" />
                        <span className="text-sm">
                          {clientStat.timing.lastContactedRelative || 'No contacts yet'}
                        </span>
                      </div>
                    </TableCell>

                    <TableCell className="text-right">
                      <div className="space-y-1">
                        <div className="text-sm font-medium">
                          {clientStat.stats.totalReplies > 0 && !isNaN(clientStat.stats.replyRate)
                            ? `${clientStat.stats.replyRate.toFixed(1)}% reply rate`
                            : 'No replies'
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {clientStat.stats.totalReplies > 0 && !isNaN(clientStat.stats.emailsPerReply)
                            ? `${Math.round(clientStat.stats.emailsPerReply).toLocaleString()} sent to get 1 reply`
                            : 'No replies'
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {clientStat.stats.positiveReplies > 0 && !isNaN(clientStat.stats.repliesPerPositiveReply)
                            ? `${Math.round(clientStat.stats.repliesPerPositiveReply).toLocaleString()} replies to get 1 positive reply`
                            : 'No positive replies'
                          }
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {clientStat.stats.positiveReplies > 0 && !isNaN(clientStat.stats.positiveReplyPercentage)
                            ? `${clientStat.stats.positiveReplyPercentage.toFixed(1)}% positive rate`
                            : 'No positive replies'
                          }
                        </div>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          
          {/* Pagination */}
          {statsData && statsData.pagination && statsData.pagination.totalPages > 1 && (
            <div className="mt-6 flex justify-center">
              <Pagination>
                <PaginationContent>
                  {statsData.pagination.hasPreviousPage && (
                    <PaginationItem>
                      <PaginationPrevious 
                        href="#"
                        onClick={(e) => {
                          e.preventDefault();
                          loadStats(currentPage - 1);
                        }}
                      />
                    </PaginationItem>
                  )}
                  
                  {/* Page Numbers */}
                  {Array.from({ length: Math.min(5, statsData.pagination.totalPages) }, (_, i) => {
                    const pageNumber = Math.max(1, Math.min(
                      statsData.pagination.totalPages - 4,
                      Math.max(1, currentPage - 2)
                    )) + i;
                    
                    if (pageNumber > statsData.pagination.totalPages) return null;
                    
                    return (
                      <PaginationItem key={pageNumber}>
                        <PaginationLink
                          href="#"
                          isActive={pageNumber === currentPage}
                          onClick={(e) => {
                            e.preventDefault();
                            loadStats(pageNumber);
                          }}
                        >
                          {pageNumber}
                        </PaginationLink>
                      </PaginationItem>
                    );
                  })}
                  
                  {statsData.pagination.hasNextPage && (
                    <PaginationItem>
                      <PaginationNext
                        href="#"
                        onClick={(e) => {
                          e.preventDefault();
                          loadStats(currentPage + 1);
                        }}
                      />
                    </PaginationItem>
                  )}
                </PaginationContent>
              </Pagination>
            </div>
          )}
          
          {/* Pagination Info */}
          {statsData && (
            <div className="mt-4 text-center text-sm text-muted-foreground">
              Showing {statsData.clients.length} of {statsData.totalCount} clients
              {statsData.pagination && statsData.pagination.totalPages > 1 && (
                <span> • Page {currentPage} of {statsData.pagination.totalPages}</span>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}