'use client';

import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '@/contexts/auth-context';
import { BarChart, TrendingUp, Mail, Reply, CheckCircle, Clock, Users, Target, ChevronDown, ArrowUp, ArrowDown, Building2, User, Activity, Filter } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
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
import { MultiSortConfig } from '@/types';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { toast } from 'sonner';

interface UserEngagementStats {
  totalSent: number;
  totalReplies: number;
  positiveReplies: number;
  emailsPerReply: number;
  emailsPerPositiveReply: number;
  repliesPerPositiveReply: number;
  positiveReplyPercentage: number;
  replyRate: number;
}

interface UserReplyTiming {
  lastReplyAt?: string;
  lastPositiveReplyAt?: string;
  lastReplyRelative?: string;
  lastPositiveReplyRelative?: string;
}

interface UserInfo {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  hasApiKey: boolean;
  apiKeyCreatedAt?: string;
  assignedClientCount: number;
  accessibleCampaignCount: number;
  activeCampaignCount: number;
  accessibleEmailAccountCount: number;
}

interface UserStats {
  user: UserInfo;
  stats: UserEngagementStats;
  timing: UserReplyTiming;
}

interface PaginationInfo {
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

interface UserStatusSummary {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  adminUsers: number;
  regularUsers: number;
  usersWithApiKeys: number;
  usersLoggedInLast30Days: number;
}

interface UserStatsResponse {
  users: UserStats[];
  totalCount: number;
  generatedAt: string;
  pagination: PaginationInfo;
  aggregatedStats?: UserEngagementStats;
  aggregatedTiming?: UserReplyTiming;
  userStatusSummary?: UserStatusSummary;
}

export function UserStats() {
  const { user, isAdmin } = useAuth();
  const [statsData, setStatsData] = useState<UserStatsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [sorting, setSorting] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('user-stats-page-size');
      return saved ? parseInt(saved, 10) : 20;
    }
    return 20;
  });
  const [multiSort, setMultiSort] = useState<MultiSortConfig>({ sorts: [] });
  const [userStatusFilter, setUserStatusFilter] = useState(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('user-stats-status-filter');
      return saved || 'all';
    }
    return 'all';
  });
  const [hideAdmins, setHideAdmins] = useState(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('user-stats-hide-admins');
      return saved === 'true';
    }
    return false;
  });

  const loadStats = useCallback(async (page: number = 1, isSort: boolean = false) => {
    try {
      if (isSort) {
        setSorting(true);
      } else {
        setLoading(true);
      }
      
      // Build sort parameters from multiSort state
      let sortBy = 'username';
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
        roleFilter: hideAdmins ? 'User' : null,
        statusFilter: userStatusFilter && userStatusFilter !== 'all' ? userStatusFilter : null,
        searchQuery: null // Can be added later for search functionality
      };
      
      // Use POST request to send filter data in body instead of query parameters
      const response = await apiClient.post<UserStatsResponse>(
        '/api/v1/users/stats',
        filterRequest
      );
      setStatsData(response);
      setCurrentPage(page);
    } catch (error: any) {
        console.error('Failed to load user stats:', error);
        
        if (error.isAuthError) {
          toast.error('Authentication required to view user statistics');
        } else if (error.status === 403) {
          toast.error('Insufficient permissions to view user statistics');
        } else if (error.isNetworkError) {
          toast.error('Unable to connect to the server');
        } else {
          toast.error('Failed to load user statistics');
        }
      } finally {
        setLoading(false);
        setSorting(false);
      }
  }, [pageSize, multiSort, userStatusFilter, hideAdmins]);

  // Handler for page size changes
  const handlePageSizeChange = useCallback((newPageSize: string) => {
    const size = parseInt(newPageSize, 10);
    setPageSize(size);
    setCurrentPage(1); // Reset to first page when changing page size
    
    // Save to localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem('user-stats-page-size', newPageSize);
    }
  }, []);

  // Handler for status filter changes
  const handleStatusFilterChange = useCallback((newStatus: string) => {
    setUserStatusFilter(newStatus);
    setCurrentPage(1); // Reset to first page when changing filter
    
    // Save to localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem('user-stats-status-filter', newStatus);
    }
  }, []);

  // Handler for hide admins toggle
  const handleHideAdminsChange = useCallback((checked: boolean) => {
    setHideAdmins(checked);
    setCurrentPage(1); // Reset to first page when changing filter
    
    // Save to localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem('user-stats-hide-admins', checked.toString());
    }
  }, []);

  // Initial load and reload when dependencies change
  useEffect(() => {
    loadStats(currentPage);
  }, [loadStats]);

  // Handler for sorting changes
  const handleSortChange = useCallback((column: string, direction: 'asc' | 'desc' | null) => {
    if (direction === null) {
      // Remove this column from sorts
      setMultiSort(prev => ({
        sorts: prev.sorts.filter(s => s.column !== column)
      }));
    } else {
      // Update or add this column
      setMultiSort(prev => {
        const existingIndex = prev.sorts.findIndex(s => s.column === column);
        if (existingIndex >= 0) {
          // Update existing sort
          const newSorts = [...prev.sorts];
          newSorts[existingIndex] = { column, direction, mode: 'count' as const };
          return { sorts: newSorts };
        } else {
          // Add new sort (limit to 3 sorts max)
          const newSorts = [{ column, direction, mode: 'count' as const }, ...prev.sorts.slice(0, 2)];
          return { sorts: newSorts };
        }
      });
    }
    
    // Trigger immediate reload with sorting flag
    loadStats(1, true);
  }, [loadStats]);

  // Pagination handlers
  const handlePageChange = useCallback((page: number) => {
    if (page >= 1 && statsData && page <= statsData.pagination.totalPages) {
      loadStats(page);
    }
  }, [loadStats, statsData]);

  const formatNumber = (num: number) => {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    } else if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  };

  const formatPercentage = (percentage: number) => {
    return `${percentage.toFixed(1)}%`;
  };

  const formatRatio = (ratio: number) => {
    return ratio === 0 ? '—' : ratio.toFixed(1);
  };

  const formatRelativeTime = (relativeTime?: string) => {
    return relativeTime || '—';
  };

  const getRoleBadgeColor = (role: string) => {
    return role === 'Admin' 
      ? 'bg-purple-100 text-purple-800 border-purple-200'
      : 'bg-blue-100 text-blue-800 border-blue-200';
  };

  const getStatusBadgeColor = (isActive: boolean) => {
    return isActive
      ? 'bg-green-100 text-green-800 border-green-200'
      : 'bg-red-100 text-red-800 border-red-200';
  };

  const getUserAvatarColor = (userId: string) => {
    const colors = [
      'bg-blue-100 text-blue-600',
      'bg-green-100 text-green-600',
      'bg-purple-100 text-purple-600',
      'bg-orange-100 text-orange-600',
      'bg-pink-100 text-pink-600',
      'bg-indigo-100 text-indigo-600',
      'bg-teal-100 text-teal-600',
      'bg-red-100 text-red-600',
      'bg-amber-100 text-amber-600',
      'bg-emerald-100 text-emerald-600',
      'bg-violet-100 text-violet-600',
      'bg-cyan-100 text-cyan-600'
    ];
    
    // Generate consistent color based on user ID
    let hash = 0;
    for (let i = 0; i < userId.length; i++) {
      hash = userId.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  };

  const UserSortHeader = () => {
    const getUserSortInfo = () => {
      const sortItem = multiSort.sorts.find(s => 
        ['username', 'email', 'role', 'assignedClientCount', 'accessibleCampaignCount'].includes(s.column)
      );
      
      if (!sortItem) return { column: null, direction: null, label: 'User' };
      
      const labels = {
        'username': 'Username',
        'email': 'Email',
        'role': 'Role',
        'assignedClientCount': 'Assigned Clients',
        'accessibleCampaignCount': 'Accessible Campaigns'
      };
      
      return {
        column: sortItem.column,
        direction: sortItem.direction,
        label: labels[sortItem.column as keyof typeof labels] || 'User'
      };
    };

    const { column, direction, label } = getUserSortInfo();
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
            onClick={() => handleSortChange('username', isActive && column === 'username' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <User className="w-3 h-3" />
              <span>Username</span>
            </div>
            {column === 'username' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('email', isActive && column === 'email' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Mail className="w-3 h-3" />
              <span>Email</span>
            </div>
            {column === 'email' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('role', isActive && column === 'role' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Building2 className="w-3 h-3" />
              <span>Role</span>
            </div>
            {column === 'role' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
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
            onClick={() => handleSortChange('totalsent', isActive && column === 'totalsent' && direction === 'asc' ? 'desc' : 'asc')}
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
            onClick={() => handleSortChange('totalreplies', isActive && column === 'totalreplies' && direction === 'asc' ? 'desc' : 'asc')}
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
            onClick={() => handleSortChange('positivereplies', isActive && column === 'positivereplies' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <CheckCircle className="w-3 h-3" />
              <span>Positive</span>
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

  const ResourcesSortHeader = () => {
    const getResourcesSortInfo = () => {
      const sortItem = multiSort.sorts.find(s => 
        ['assignedClientCount', 'accessibleCampaignCount', 'activeCampaignCount', 'accessibleEmailAccountCount'].includes(s.column)
      );
      
      if (!sortItem) return { column: null, direction: null, label: 'Resources' };
      
      const labels = {
        'assignedClientCount': 'Clients',
        'accessibleCampaignCount': 'Campaigns',
        'activeCampaignCount': 'Active Campaigns',
        'accessibleEmailAccountCount': 'Email Accounts'
      };
      
      return {
        column: sortItem.column,
        direction: sortItem.direction,
        label: labels[sortItem.column as keyof typeof labels] || 'Resources'
      };
    };

    const { column, direction, label } = getResourcesSortInfo();
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
        <DropdownMenuContent align="end" className="w-40">
          <DropdownMenuItem 
            onClick={() => handleSortChange('assignedClientCount', isActive && column === 'assignedClientCount' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Building2 className="w-3 h-3" />
              <span>Clients</span>
            </div>
            {column === 'assignedClientCount' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('accessibleCampaignCount', isActive && column === 'accessibleCampaignCount' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Target className="w-3 h-3" />
              <span>Campaigns</span>
            </div>
            {column === 'accessibleCampaignCount' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('activeCampaignCount', isActive && column === 'activeCampaignCount' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <TrendingUp className="w-3 h-3" />
              <span>Active</span>
            </div>
            {column === 'activeCampaignCount' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('accessibleEmailAccountCount', isActive && column === 'accessibleEmailAccountCount' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Mail className="w-3 h-3" />
              <span>Email Accounts</span>
            </div>
            {column === 'accessibleEmailAccountCount' && (
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
        ['replyrate', 'emailsperreply', 'emailsperpositivereply', 'positivereplypercentage'].includes(s.column)
      );
      
      if (!sortItem) return { column: null, direction: null, label: 'Performance' };
      
      const labels = {
        'replyrate': 'Reply Rate',
        'emailsperreply': 'Emails/Reply',
        'emailsperpositivereply': 'Emails/Positive',
        'positivereplypercentage': 'Positive %'
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
        <DropdownMenuContent align="end" className="w-40">
          <DropdownMenuItem 
            onClick={() => handleSortChange('replyrate', isActive && column === 'replyrate' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Activity className="w-3 h-3" />
              <span>Reply Rate</span>
            </div>
            {column === 'replyrate' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('emailsperreply', isActive && column === 'emailsperreply' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <TrendingUp className="w-3 h-3" />
              <span>Emails/Reply</span>
            </div>
            {column === 'emailsperreply' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('emailsperpositivereply', isActive && column === 'emailsperpositivereply' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <Target className="w-3 h-3" />
              <span>Emails/Positive</span>
            </div>
            {column === 'emailsperpositivereply' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
          <DropdownMenuItem 
            onClick={() => handleSortChange('positivereplypercentage', isActive && column === 'positivereplypercentage' && direction === 'asc' ? 'desc' : 'asc')}
            className="flex items-center justify-between"
          >
            <div className="flex items-center gap-2">
              <CheckCircle className="w-3 h-3" />
              <span>Positive %</span>
            </div>
            {column === 'positivereplypercentage' && (
              direction === 'asc' ? 
                <ArrowUp className="w-3 h-3 text-primary" /> : 
                <ArrowDown className="w-3 h-3 text-primary" />
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  };

  if (loading) {
    return (
      <Card className="rounded-xl">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart className="w-5 h-5" />
            User Statistics
          </CardTitle>
          <p className="text-sm text-muted-foreground">
            Loading user statistics...
          </p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="flex items-center space-x-4">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-4 w-16" />
                <Skeleton className="h-4 w-16" />
                <Skeleton className="h-4 w-16" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!statsData) {
    return (
      <Card className="rounded-xl">
        <CardContent className="flex items-center justify-center py-12">
          <div className="text-center">
            <Activity className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">Failed to Load User Statistics</h3>
            <p className="text-muted-foreground mb-4">
              There was an error loading the user statistics data.
            </p>
            <Button onClick={() => loadStats(1)}>
              Try Again
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!statsData || statsData.users.length === 0) {
    return (
      <Card className="rounded-xl">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <BarChart className="w-5 h-5" />
                User Statistics
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                Last updated: {statsData?.generatedAt ? new Date(statsData.generatedAt).toLocaleString() : 'N/A'}
              </p>
            </div>
            <div className="grid gap-4 grid-cols-[auto_auto_auto] items-center">
              <div className="flex items-center gap-2">
                <Label htmlFor="user-status-filter" className="text-sm font-medium whitespace-nowrap">
                  Status:
                </Label>
                <Select
                  value={userStatusFilter}
                  onValueChange={handleStatusFilterChange}
                >
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Users</SelectItem>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="hide-admins"
                  checked={hideAdmins}
                  onCheckedChange={handleHideAdminsChange}
                />
                <Label htmlFor="hide-admins" className="text-sm font-medium whitespace-nowrap">
                  Hide Admins
                </Label>
              </div>
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
            <p>No user statistics available yet</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* User Statistics Table */}
      <Card className="rounded-xl">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <BarChart className="w-5 h-5" />
                User Statistics
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                Last updated: {new Date(statsData.generatedAt).toLocaleString()}
              </p>
            </div>
            <div className="grid gap-4 grid-cols-[auto_auto_auto] items-center">
              <div className="flex items-center gap-2">
                <Label htmlFor="user-status-filter" className="text-sm font-medium whitespace-nowrap">
                  Status:
                </Label>
                <Select
                  value={userStatusFilter}
                  onValueChange={handleStatusFilterChange}
                >
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Users</SelectItem>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="hide-admins"
                  checked={hideAdmins}
                  onCheckedChange={handleHideAdminsChange}
                />
                <Label htmlFor="hide-admins" className="text-sm font-medium whitespace-nowrap">
                  Hide Admins
                </Label>
              </div>
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
                    <UserSortHeader />
                  </TableHead>
                  <TableHead className="hidden sm:table-cell">
                    <EngagementSortHeader />
                  </TableHead>
                  <TableHead>
                    <button 
                      className="flex items-center gap-1 font-semibold hover:text-primary"
                      onClick={() => {
                        const currentSort = multiSort.sorts.find(s => s.column === 'lastReplyAt');
                        const newDirection = currentSort && currentSort.direction === 'asc' ? 'desc' : 'asc';
                        handleSortChange('lastReplyAt', newDirection);
                      }}
                    >
                      Last Reply
                      {(() => {
                        const sort = multiSort.sorts.find(s => s.column === 'lastReplyAt');
                        if (sort) {
                          return sort.direction === 'asc' ? 
                            <ArrowUp className="w-3 h-3" /> : 
                            <ArrowDown className="w-3 h-3" />;
                        }
                        return null;
                      })()}
                    </button>
                  </TableHead>
                  <TableHead>
                    <button 
                      className="flex items-center gap-1 font-semibold hover:text-primary"
                      onClick={() => {
                        const currentSort = multiSort.sorts.find(s => s.column === 'lastLoginAt');
                        const newDirection = currentSort && currentSort.direction === 'asc' ? 'desc' : 'asc';
                        handleSortChange('lastLoginAt', newDirection);
                      }}
                    >
                      Last Login
                      {(() => {
                        const sort = multiSort.sorts.find(s => s.column === 'lastLoginAt');
                        if (sort) {
                          return sort.direction === 'asc' ? 
                            <ArrowUp className="w-3 h-3" /> : 
                            <ArrowDown className="w-3 h-3" />;
                        }
                        return null;
                      })()}
                    </button>
                  </TableHead>
                  <TableHead className="text-right">
                    <PerformanceSortHeader />
                  </TableHead>
                  <TableHead className="text-right">
                    <ResourcesSortHeader />
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody className={`transition-opacity duration-300 ${sorting ? 'opacity-60 pointer-events-none' : 'opacity-100'}`}>
                {statsData.users.map((userStat) => (
                  <TableRow key={userStat.user.id} className="hover:bg-muted/30">
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className={`w-8 h-8 rounded-full ${getUserAvatarColor(userStat.user.id)} flex items-center justify-center text-sm font-medium`}>
                          {userStat.user.username.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <div className="font-medium">{userStat.user.username}</div>
                          <div className="text-sm text-muted-foreground">{userStat.user.email}</div>
                          {(userStat.user.firstName || userStat.user.lastName) && (
                            <div className="text-xs text-muted-foreground">
                              {userStat.user.firstName} {userStat.user.lastName}
                            </div>
                          )}
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="hidden sm:table-cell">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2 text-sm font-medium">
                          <Mail className="w-4 h-4 text-muted-foreground" />
                          <span>{formatNumber(userStat.stats?.totalSent || 0)} sent</span>
                        </div>
                        <div className="flex items-center gap-2 text-sm font-medium">
                          <Reply className="w-4 h-4 text-muted-foreground" />
                          <span>{formatNumber(userStat.stats?.totalReplies || 0)} replies</span>
                        </div>
                        <div className="flex items-center gap-2 text-sm font-medium">
                          <CheckCircle className="w-4 h-4 text-muted-foreground" />
                          <span>{formatNumber(userStat.stats?.positiveReplies || 0)} positive</span>
                        </div>
                      </div>
                    </TableCell>

                    <TableCell>
                      <div className="text-sm">
                        {userStat.timing.lastReplyAt ? (
                          <div>
                            <div className="font-medium">
                              {formatRelativeTime(userStat.timing.lastReplyRelative)}
                            </div>
                            <div className="text-muted-foreground">
                              {new Date(userStat.timing.lastReplyAt).toLocaleDateString()}
                            </div>
                          </div>
                        ) : (
                          <span className="text-muted-foreground">No replies yet</span>
                        )}
                      </div>
                    </TableCell>

                    <TableCell>
                      <div className="text-sm">
                        {userStat.user.lastLoginAt ? (
                          <div>
                            <div className="font-medium">
                              {new Date(userStat.user.lastLoginAt).toLocaleDateString()}
                            </div>
                            <div className="text-muted-foreground">
                              Last active
                            </div>
                          </div>
                        ) : (
                          <span className="text-muted-foreground">Never logged in</span>
                        )}
                      </div>
                    </TableCell>

                    <TableCell className="text-right">
                      <div className="space-y-1">
                        <div className="text-sm font-medium">
                          {userStat.stats.totalReplies > 0 && !isNaN(userStat.stats.replyRate)
                            ? `${userStat.stats.replyRate.toFixed(1)}% reply rate`
                            : 'No replies'
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {userStat.stats.totalReplies > 0 && !isNaN(userStat.stats.emailsPerReply)
                            ? `${Math.round(userStat.stats.emailsPerReply).toLocaleString()} sent to get 1 reply`
                            : 'No replies'
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {userStat.stats.positiveReplies > 0 && !isNaN(userStat.stats.repliesPerPositiveReply)
                            ? `${Math.round(userStat.stats.repliesPerPositiveReply).toLocaleString()} replies to get 1 positive reply`
                            : 'No positive replies'
                          }
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {userStat.stats.positiveReplies > 0 && !isNaN(userStat.stats.positiveReplyPercentage)
                            ? `${userStat.stats.positiveReplyPercentage.toFixed(1)}% positive rate`
                            : 'No positive replies'
                          }
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="text-right">
                      <div className="space-y-1">
                        <div className="flex items-center justify-end gap-2 text-sm">
                          <Building2 className="w-4 h-4 text-muted-foreground" />
                          <span className="font-medium">{userStat.user.assignedClientCount}</span>
                          <span className="text-muted-foreground">clients</span>
                        </div>
                        <div className="flex items-center justify-end gap-2 text-sm">
                          <Target className="w-4 h-4 text-muted-foreground" />
                          <span className="font-medium">{userStat.user.accessibleCampaignCount}</span>
                          <span className="text-muted-foreground">campaigns</span>
                        </div>
                        <div className="flex items-center justify-end gap-2 text-sm">
                          <TrendingUp className="w-4 h-4 text-green-600" />
                          <span className="font-medium text-green-600">{userStat.user.activeCampaignCount}</span>
                          <span className="text-muted-foreground">active</span>
                        </div>
                        <div className="flex items-center justify-end gap-2 text-sm">
                          <Mail className="w-4 h-4 text-muted-foreground" />
                          <span className="font-medium">{userStat.user.accessibleEmailAccountCount}</span>
                          <span className="text-muted-foreground">accounts</span>
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
                          handlePageChange(currentPage - 1);
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
                            handlePageChange(pageNumber);
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
                          handlePageChange(currentPage + 1);
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
              Showing {statsData.users.length} of {statsData.totalCount} users
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