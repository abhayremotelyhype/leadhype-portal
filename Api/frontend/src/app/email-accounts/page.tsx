'use client';

import React, { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { useSearchParams } from 'next/navigation';
import { MoreHorizontal, Settings, ExternalLink, Building2, UserX, UserPlus, UserMinus, CheckCircle, XCircle, Clock, Mail, Download, Calendar, Tags, Megaphone, Activity, FileText } from 'lucide-react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { TablePagination } from '@/components/table-pagination';
import { PageHeader } from '@/components/page-header';
import { DataTableToolbar } from '@/components/data-table-toolbar';
import { DataTable } from '@/components/data-table';
import { StatsCell } from '@/components/stats-cell';
import { StatusBadge } from '@/components/status-badge';
import { AssignClientModal } from '@/components/assign-client-modal';
import { NotesModal } from '@/components/notes-modal';
import { toast } from 'sonner';
import { ClientSearchFilter } from '@/components/client-search-filter';
import { UserSearchFilter } from '@/components/user-search-filter';
import { VolumeFilter, VolumeRange } from '@/components/volume-filter';
import { DisconnectedAccountsFilter } from '@/components/disconnected-accounts-filter';
import { WorstPerformingFilter } from '@/components/worst-performing-filter';
import { ConfirmationDialog, useConfirmationDialog } from '@/components/confirmation-dialog';
import { ColumnCustomizationModal } from '@/components/column-customization-modal';
import { useColumnVisibility } from '@/hooks/use-column-visibility';
import { useDataTable } from '@/hooks/use-data-table';
import { useErrorHandling } from '@/hooks/use-error-handling';
import { useResizableColumns } from '@/hooks/use-resizable-columns';
import { apiClient, ENDPOINTS, formatDate, debounce, PaginatedResponse } from '@/lib/api';
import { useAuth } from '@/contexts/auth-context';
import { cn } from '@/lib/utils';
import { EmailAccount, Client, SortConfig, ColumnDefinition, TableState, ClientListItem, UserListItem } from '@/types';
import { ClientSelectionDropdown } from '@/components/client-selection-dropdown';
import { TagSelector } from '@/components/tag-selector';
import { TimeRangeSelector, TimeRangeOption, defaultTimeRangeOptions } from '@/components/time-range-selector';
import { DownloadModal } from '@/components/download-modal';

// Dynamic column definitions based on selected time range - to be used after selectedTimeRange is defined

const defaultColumnWidths = {
  select: 40,
  email: 200,
  name: 150,
  status: 120,
  client: 150,
  tags: 200,
  tagcount: 80,
  campaigns: 100,
  sent: 80,
  opened: 100,
  replied: 100,
  bounced: 100,
  warmupSent: 100,
  warmupReplied: 120,
  warmupSpamCount: 120,
  warmupSavedFromSpam: 150,
  createdAt: 120,
  updatedAt: 120,
};

const defaultSort: SortConfig = {
  column: 'email',
  direction: 'asc',
  mode: 'count',
};

export default function EmailAccountsPage() {
  const { isAdmin } = useAuth();
  const { error, handleApiError, handleSuccess } = useErrorHandling({ resetOnSuccess: true, showToast: false });
  const searchParams = useSearchParams();
  const [emailAccounts, setEmailAccounts] = useState<EmailAccount[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [loading, setLoading] = useState(true);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const prevParamsRef = useRef<{
    page: number;
    search: string;
    clientFilter: string[];
    userFilter: string[];
    timeRange: number;
    volumeRange: string | null;
    disconnectedOnly: boolean;
    worstPerforming: string | null;
  }>({
    page: 1,
    search: '',
    clientFilter: [],
    userFilter: [],
    timeRange: 7,
    volumeRange: null,
    disconnectedOnly: false,
    worstPerforming: null,
  });
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  
  // Email IDs filter state
  const [emailIdsFilter, setEmailIdsFilter] = useState<string[]>([]);
  const [campaignIdFilter, setCampaignIdFilter] = useState<string | null>(null);
  
  // Client filter state for enhanced search (single selection)
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  const [clientsLoading, setClientsLoading] = useState(false);

  // User filter state for enhanced search (single selection)
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [usersLoading, setUsersLoading] = useState(false);

  // Volume filter state with localStorage persistence (single selection)
  const [selectedVolumeRange, setSelectedVolumeRange] = useState<string | null>(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('email-accounts-volume-filter');
      try {
        return saved ? JSON.parse(saved) : null;
      } catch {
        return null;
      }
    }
    return null;
  });
  
  // Disconnected accounts filter state
  const [showDisconnectedOnly, setShowDisconnectedOnly] = useState(false);
  
  // Worst performing filter state
  const [selectedWorstPerformingFilter, setSelectedWorstPerformingFilter] = useState<string | null>(null);
  
  const [isAssignClientModalOpen, setIsAssignClientModalOpen] = useState(false);

  // Set page title
  useEffect(() => {
    document.title = 'Email Accounts - LeadHype';
  }, []);

  // Handle client filter from URL parameter
  useEffect(() => {
    const clientId = searchParams.get('client');
    if (clientId) {
      setSelectedClientId(clientId);
    }
  }, [searchParams]);
  const [selectedAccountForAssignment, setSelectedAccountForAssignment] = useState<EmailAccount | null>(null);
  const [isAssigning, setIsAssigning] = useState(false);
  
  // Notes modal state
  const [isNotesModalOpen, setIsNotesModalOpen] = useState(false);
  const [selectedAccountForNotes, setSelectedAccountForNotes] = useState<EmailAccount | null>(null);
  const [originalTotalCount, setOriginalTotalCount] = useState<number>(0);
  
  // Confirmation dialog state for unassigning client
  const confirmDialog = useConfirmationDialog();
  const [accountToUnassign, setAccountToUnassign] = useState<EmailAccount | null>(null);
  const [isUnassigning, setIsUnassigning] = useState(false);
  
  // Download modal state
  const [showDownloadModal, setShowDownloadModal] = useState(false);
  
  // Time range state for period-based statistics
  const [selectedTimeRange, setSelectedTimeRange] = useState<TimeRangeOption>(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('email-accounts-time-range-preference');
      return saved ? JSON.parse(saved) : { value: '7d', label: '7 Days', days: 7 };
    }
    return { value: '7d', label: '7 Days', days: 7 };
  });
  
  const abortControllerRef = useRef<AbortController | null>(null);
  const loadingRef = useRef(false);

  const pageSizeOptions = [10, 25, 50, 100, 200];

  // Helper function to calculate time range values from email dictionaries
  const getTimeRangeValue = (account: EmailAccount, metric: 'sent' | 'opened' | 'replied' | 'bounced'): number => {
    const dictionary = account[`${metric}Emails` as keyof EmailAccount] as { [date: string]: number } | undefined;
    
    if (!dictionary || selectedTimeRange.days === 9999) {
      // Return total value for 'all time'
      return account[metric] || 0;
    }
    
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - (selectedTimeRange.days || 7));
    
    let total = 0;
    for (const [dateStr, count] of Object.entries(dictionary)) {
      const date = new Date(dateStr);
      if (date >= cutoffDate) {
        total += count;
      }
    }
    
    return total;
  };

  // Dynamic column definitions based on selected time range - memoized to prevent infinite re-renders
  const columnDefinitions = useMemo((): Record<string, ColumnDefinition> => {
    const timeLabel = selectedTimeRange.days === 1 ? '24h' : 
                     selectedTimeRange.days === 9999 ? 'All Time' : 
                     `${selectedTimeRange.days}d`;
    
    return {
      select: { label: '', sortable: false, required: true },
      email: { label: 'Email Address', sortable: true },
      name: { label: 'Account Name', sortable: true },
      status: { label: 'Warmup Status', sortable: true },
      client: { label: 'Assigned Client', sortable: true },
      tags: { label: 'Tags', sortable: true, required: true },
      tagcount: { label: 'Tag Count', sortable: true },
      campaigns: { label: 'Campaigns', sortable: true, required: true },
      activeCampaigns: { label: 'Active Campaigns', sortable: true },
      sendingActualEmails: { label: 'Sending Status', sortable: true, required: true },
      sent: { label: `Sent (${timeLabel})`, sortable: true },
      opened: { label: `Opened (${timeLabel})`, sortable: true, dualSort: true },
      replied: { label: `Replied (${timeLabel})`, sortable: true, dualSort: true },
      positiveReplies: { label: 'Positive Replies', sortable: true, dualSort: true },
      bounced: { label: `Bounced (${timeLabel})`, sortable: true, dualSort: true },
      warmupSent: { label: 'Warmup Sent', sortable: true, dualSort: true },
      warmupReplied: { label: 'Warmup Replied', sortable: true, dualSort: true },
      warmupSpamCount: { label: 'Warmup Spam Rate', sortable: true, dualSort: true },
      warmupSavedFromSpam: { label: 'Warmup Saved from Spam', sortable: true, dualSort: true },
      notes: { label: 'Notes', sortable: true, required: true },
      createdAt: { label: 'Created Date', sortable: true },
      updatedAt: { label: 'Updated At', sortable: true, required: true },
    };
  }, [selectedTimeRange.days]); // Only recalculate when selectedTimeRange.days changes

  // Use the reusable data table hook
  const {
    tableState,
    handleSort,
    handleSearch,
    handlePageChange,
    handlePageSizeChange,
    handleSelectAll,
    handleSelectOne,
    updateTableData,
    clearSelection,
    resetSort,
    clearAllSorts,
  } = useDataTable({
    storageKey: 'email-accounts',
    defaultPageSize: 25,
    defaultSort,
  });

  // Map frontend column names to backend expected names
  const mapSortColumn = (column: string): string => {
    const mapping: Record<string, string> = {
      warmupSent: 'warmupsent',
      warmupReplied: 'warmupreplied', 
      warmupSavedFromSpam: 'warmupsavedfromspam',
      createdAt: 'createdat',
      campaigns: 'campaignCount',
      sendingActualEmails: 'issendingactualemails', // Map to the backend field with proper sorting logic
    };
    return mapping[column] || column;
  };

  // Column visibility and ordering management
  const { 
    visibleColumns, 
    columnOrder,
    toggleColumn, 
    resetColumns, 
    resetColumnOrder,
    reorderColumns,
    isColumnVisible,
    getOrderedVisibleColumns 
  } = useColumnVisibility({
    columns: columnDefinitions,
    storageKey: 'email-accounts-visible-columns',
    orderStorageKey: 'email-accounts-column-order',
  });

  // Resizable columns management
  const {
    columnWidths,
    updateColumnWidth,
    resetColumnWidths,
    handleMouseDown,
    getColumnStyle,
  } = useResizableColumns({
    storageKey: 'email-accounts-column-widths',
    defaultWidths: defaultColumnWidths,
    minWidth: 50,
    maxWidth: 400,
  });

  const loadClients = useCallback(async () => {
    setClientsLoading(true);
    try {
      const response = await apiClient.get<{ clients: Client[] }>(ENDPOINTS.clients, {
        limit: '10000' // Load all clients for the dropdown
      });
      setClients(response.clients);
    } catch (error) {
      console.error('Failed to load clients:', error);
      // Error handling is managed by main load function to avoid duplicate errors
    } finally {
      setClientsLoading(false);
    }
  }, []);

  // Load users for cascading client filter
  const loadUsers = useCallback(async () => {
    if (!isAdmin) return; // Only admins can see user filter
    
    setUsersLoading(true);
    try {
      const response = await apiClient.get<{ data: UserListItem[] }>(ENDPOINTS.userList);
      const usersData = response.data || response;
      setUsers(Array.isArray(usersData) ? usersData : []);
    } catch (error) {
      console.error('Failed to load users:', error);
      setUsers([]);
    } finally {
      setUsersLoading(false);
    }
  }, [isAdmin]);

  // Simple loadEmailAccounts for manual refresh
  const loadEmailAccounts = useCallback(() => {
    setRefreshTrigger(prev => prev + 1);
  }, []);


  // Load users when component mounts (for admins only)
  useEffect(() => {
    if (isAdmin) {
      loadUsers();
    }
  }, [isAdmin, loadUsers]);

  // Search is now debounced in DataTableToolbar component

  const showSortModeNotification = (mode: string, direction: string) => {
    toast.info(`Sorting by ${mode} ${direction === 'asc' ? '↑' : '↓'}`);
  };

  const handleOpenAssignClientModal = (account?: EmailAccount) => {
    if (!isAdmin) {
      toast.error('Access denied: Only administrators can assign or unassign clients');
      return;
    }
    if (account) {
      setSelectedAccountForAssignment(account);
    } else {
      setSelectedAccountForAssignment(null);
    }
    setIsAssignClientModalOpen(true);
    if (!clients || clients.length === 0) {
      loadClients();
    }
  };

  const handleAssignClient = async (clientId: string) => {
    if (!clientId) {
      toast.error('Please select a client');
      return;
    }

    setIsAssigning(true);
    try {
      const emailAccountIds = selectedAccountForAssignment 
        ? [selectedAccountForAssignment.id]
        : Array.from(tableState.selectedItems);

      // Ensure all IDs are strings that can be parsed as numbers
      const validatedIds = emailAccountIds.map(id => String(id));

      // Use the correct backend endpoint
      await apiClient.post(`${ENDPOINTS.emailAccounts}/assign-client`, {
        ClientId: clientId,
        EmailAccountIds: validatedIds
      });
      
      const count = emailAccountIds.length;
      toast.success(`Client assigned to ${count} ${count === 1 ? 'account' : 'accounts'}`);
      
      // Clear selection if bulk assignment
      if (!selectedAccountForAssignment) {
        clearSelection();
      }
      
      // Refresh data
      loadEmailAccounts();
      setIsAssignClientModalOpen(false);
    } catch (error) {
      console.error('Failed to assign client:', error);
      toast.error('Failed to assign client');
    } finally {
      setIsAssigning(false);
    }
  };

  const handleUnassignClient = (account: EmailAccount) => {
    if (!isAdmin) {
      toast.error('Access denied: Only administrators can assign or unassign clients');
      return;
    }
    setAccountToUnassign(account);
    confirmDialog.openDialog();
  };

  const handleConfirmUnassign = async () => {
    if (!accountToUnassign) return;

    setIsUnassigning(true);
    try {
      // Use the assign-client endpoint with null ClientId to unassign
      await apiClient.post(`${ENDPOINTS.emailAccounts}/assign-client`, {
        ClientId: null,
        EmailAccountIds: [String(accountToUnassign.id)]
      });
      
      toast.success('Client unassigned successfully');
      
      // Refresh data
      loadEmailAccounts();
      confirmDialog.closeDialog();
      setAccountToUnassign(null);
    } catch (error) {
      console.error('Failed to unassign client:', error);
      toast.error('Failed to unassign client');
    } finally {
      setIsUnassigning(false);
    }
  };

  const handleDownload = () => {
    setShowDownloadModal(true);
  };

  const handleNotesUpdated = (notes: string | null) => {
    if (selectedAccountForNotes) {
      setEmailAccounts(prevAccounts =>
        prevAccounts.map(account =>
          account.id === selectedAccountForNotes.id
            ? { ...account, notes: notes ?? undefined }
            : account
        )
      );
    }
  };


  const renderStatsColumn = (account: EmailAccount, column: keyof EmailAccount, baseValue?: keyof EmailAccount) => {
    const count = account[column] as number || 0;
    const base = baseValue ? (account[baseValue] as number) : account.sent;
    const percentage = base > 0 ? ((count / base) * 100).toFixed(1) : '0';

    const isActive = tableState.sort.column === column;
    const showPercentageFirst = isActive && tableState.sort.mode === 'percentage';

    const primaryValue = showPercentageFirst ? `${percentage}%` : count.toString();
    const secondaryValue = showPercentageFirst ? count.toString() : `${percentage}%`;

    const getColors = (col: string) => {
      const colors = {
        opened: { primary: showPercentageFirst ? 'text-purple-600' : 'text-blue-600', secondary: showPercentageFirst ? 'text-blue-500' : 'text-muted-foreground' },
        replied: { primary: showPercentageFirst ? 'text-purple-600' : 'text-green-600', secondary: showPercentageFirst ? 'text-green-500' : 'text-muted-foreground' },
        bounced: { primary: showPercentageFirst ? 'text-purple-600' : 'text-red-600', secondary: showPercentageFirst ? 'text-red-500' : 'text-muted-foreground' },
      };
      return colors[col as keyof typeof colors] || { primary: 'text-foreground', secondary: 'text-muted-foreground' };
    };

    const colors = getColors(column as string);

    return (
      <div className="flex flex-col">
        <span className={`text-xs font-semibold ${colors.primary}`}>
          {primaryValue}
        </span>
        <div className={`text-[10px] ${colors.secondary}`}>
          {secondaryValue}
        </div>
      </div>
    );
  };

  const getClientBadge = (clientName: string | undefined) => {
    if (!clientName) {
      return (
        <Badge className="bg-gray-100 text-gray-600 hover:bg-gray-100 flex items-center gap-1">
          <UserX className="w-3 h-3" />
          Unassigned
        </Badge>
      );
    }

    return (
      <Badge className="bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-100 flex items-center gap-1">
        <Building2 className="w-3 h-3" />
        {clientName}
      </Badge>
    );
  };

  const getStatusBadge = (status: EmailAccount['status']) => {
    if (!status) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
          <XCircle className="w-3 h-3" />
          Not Defined
        </span>
      );
    }

    const statusConfig = {
      Active: { 
        className: 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800',
        icon: <CheckCircle className="w-3 h-3" />
      },
      Inactive: { 
        className: 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800',
        icon: <XCircle className="w-3 h-3" />
      },
      Warming: { 
        className: 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800',
        icon: <Clock className="w-3 h-3" />
      },
    };

    const config = statusConfig[status] || { 
      className: 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700',
      icon: <XCircle className="w-3 h-3" />
    };

    return (
      <span className={config.className}>
        {config.icon}
        {status}
      </span>
    );
  };

  // Update select all to work with current data
  const handleSelectAllWithData = (checked: boolean) => {
    if (checked) {
      emailAccounts.forEach(account => handleSelectOne(account.id, true));
    } else {
      emailAccounts.forEach(account => handleSelectOne(account.id, false));
    }
  };

  const renderEmailAccountCell = (account: EmailAccount, columnKey: string) => {
    switch (columnKey) {
      case 'select':
        return (
          <Checkbox
            checked={tableState.selectedItems.has(account.id)}
            onCheckedChange={(checked) => handleSelectOne(account.id, checked as boolean)}
          />
        );
      case 'email':
        return (
          <Link href={`/email-accounts/${account.id}/analytics`}>
            <div className="flex items-center cursor-pointer hover:text-primary transition-colors min-w-0">
              <div className="mr-2 flex h-6 w-6 sm:h-6 sm:w-6 items-center justify-center rounded-full bg-primary/10 flex-shrink-0">
                <span className="text-[10px] sm:text-[10px] font-semibold text-primary">
                  {account.email?.charAt(0)?.toUpperCase() || '@'}
                </span>
              </div>
              <div className="min-w-0 flex-1">
                <div className="text-xs font-medium truncate" title={account.email}>{account.email}</div>
                <div className="text-[10px] text-muted-foreground truncate" title={account.id.toString()}>{account.id}</div>
              </div>
            </div>
          </Link>
        );
      case 'name':
        return <span className="text-xs text-muted-foreground truncate block" title={account.name || 'N/A'}>{account.name || 'N/A'}</span>;
      case 'status':
        return <StatusBadge status={account.status} type="emailAccount" />;
      case 'client':
        return account.clientName ? (
          <div className="group relative inline-flex items-center min-w-0">
            <div 
              className="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium border cursor-pointer transition-all duration-200 group-hover:pr-7 hover:opacity-80 min-w-0 max-w-full"
                onClick={isAdmin ? () => handleOpenAssignClientModal(account) : undefined}
              title={isAdmin ? `${account.clientName} - Click to change client` : `${account.clientName} (View Only)`}
              style={{
                backgroundColor: account.clientColor ? `${account.clientColor}15` : '#3B82F615',
                color: account.clientColor || '#3B82F6',
                borderColor: account.clientColor ? `${account.clientColor}40` : '#3B82F640',
                cursor: isAdmin ? 'pointer' : 'default'
              }}
            >
              <Building2 className="w-3 h-3 mr-1 flex-shrink-0" />
              <span className="truncate">{account.clientName}</span>
            </div>
            {isAdmin && (
              <Button
                variant="ghost"
                size="sm"
                onClick={(e) => {
                  e.stopPropagation();
                  handleUnassignClient(account);
                }}
                className="absolute right-1 h-5 w-5 p-0 opacity-0 group-hover:opacity-100 transition-opacity duration-200 text-red-500 hover:text-red-600 rounded-full"
                title="Unassign client"
              >
                <span className="text-xs font-bold">−</span>
              </Button>
            )}
          </div>
        ) : isAdmin ? (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleOpenAssignClientModal(account)}
            className="h-6 px-2 text-xs"
          >
            <UserPlus className="w-3 h-3 mr-1" />
            Assign
          </Button>
        ) : (
          <Badge className="bg-gray-100 text-gray-600 hover:bg-gray-100 flex items-center gap-1">
            <UserX className="w-3 h-3" />
            Unassigned
          </Badge>
        );
      case 'sent':
        return <span className="text-xs font-medium">{account.sent || 0}</span>;
      case 'opened':
        return <StatsCell count={account.opened || 0} baseValue={account.sent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="opened" />;
      case 'replied':
        return <StatsCell count={account.replied || 0} baseValue={account.sent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />;
      case 'positiveReplies':
        return <StatsCell count={account.positiveReplies || 0} baseValue={account.replied || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="positive" />;
      case 'bounced':
        return <StatsCell count={account.bounced || 0} baseValue={account.sent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="bounced" />;
      case 'warmupSent':
        return <span className="text-xs font-medium text-orange-600">{account.warmupSent || 0}</span>;
      case 'warmupReplied':
        return <StatsCell count={account.warmupReplied || 0} baseValue={account.warmupSent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />;
      case 'warmupSpamCount':
        const spamRate = account.warmupSent > 0 ? ((account.warmupSpamCount || 0) / account.warmupSent * 100) : 0;
        return (
          <div className="text-xs">
            <div className={`font-medium ${spamRate > 10 ? 'text-red-600' : spamRate > 5 ? 'text-yellow-600' : 'text-green-600'}`}>
              {spamRate.toFixed(1)}%
            </div>
            <div className="text-muted-foreground text-[10px]">
              {account.warmupSpamCount || 0}/{account.warmupSent || 0}
            </div>
          </div>
        );
      case 'warmupSavedFromSpam':
        return <StatsCell count={account.warmupSavedFromSpam || 0} baseValue={account.warmupSent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />;
      case 'tags':
        return (
          <div className="min-w-[200px]">
            <TagSelector
              entityType="email-account"
              entityId={account.id}
              selectedTags={account.tags || []}
              onTagsChange={(tags) => {
                // Update the email account tags in the local state
                setEmailAccounts(prev => prev.map(a => 
                  a.id === account.id ? { ...a, tags } : a
                ));
              }}
            />
          </div>
        );
      case 'tagcount':
        return (
          <span className="text-sm font-medium">
            {account.tags?.length || 0}
          </span>
        );
      case 'campaigns':
        // Don't render anything if no campaigns
        if (account.campaignCount === 0) {
          return null;
        }
        
        return (
          <Link
            href={`/campaigns?emailAccountId=${account.id}`}
            className="flex items-center text-xs font-medium hover:text-primary transition-colors"
            onClick={(e) => e.stopPropagation()}
          >
            <Megaphone className="w-3 h-3 mr-1 flex-shrink-0" />
            <span>{account.campaignCount}</span>
          </Link>
        );
      case 'activeCampaigns':
        return (
          <div className="flex items-center text-xs font-medium">
            <Activity className="w-3 h-3 mr-1 flex-shrink-0 text-green-600" />
            <span>{account.activeCampaignCount || 0}</span>
          </div>
        );
      case 'sendingActualEmails':
        const isSending = account.isSendingActualEmails;
        // Handle null, false, and true values correctly
        // null = inactive, false = warmup only, true = active
        
        if (isSending === true) {
          return (
            <Badge className="bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400 border-green-200 dark:border-green-800 text-xs px-2 py-0.5 hover:bg-green-100 dark:hover:bg-green-900/30">
              <CheckCircle className="w-3 h-3 mr-1" />
              Active
            </Badge>
          );
        } else if (isSending === false || (isSending === null && account.warmupSent > 0)) {
          // Show warmup only if explicitly false OR if null but has warmup activity
          return (
            <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400 border-amber-200 dark:border-amber-800 text-xs px-2 py-0.5 hover:bg-amber-100 dark:hover:bg-amber-900/30">
              <Clock className="w-3 h-3 mr-1" />
              Warmup Only
            </Badge>
          );
        } else {
          return (
            <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-900/30 dark:text-gray-400 border-gray-200 dark:border-gray-800 text-xs px-2 py-0.5 hover:bg-gray-100 dark:hover:bg-gray-900/30">
              <XCircle className="w-3 h-3 mr-1" />
              Inactive
            </Badge>
          );
        }
      case 'notes':
        return (
          <Button
            variant="ghost"
            size="sm"
            className={`h-8 w-8 p-0 ${account.notes ? 'text-blue-600 hover:text-blue-700' : 'text-muted-foreground hover:text-foreground'}`}
            onClick={() => {
              setSelectedAccountForNotes(account);
              setIsNotesModalOpen(true);
            }}
            title={account.notes ? 'View/edit notes' : 'Add notes'}
          >
            <FileText className="h-4 w-4" />
          </Button>
        );
      case 'createdAt':
        return <span className="text-xs text-muted-foreground truncate block" title={formatDate(account.createdAt)}>{formatDate(account.createdAt)}</span>;
      case 'updatedAt':
        return account.updatedAt ? (
          <span 
            className="text-xs text-muted-foreground cursor-pointer truncate block"
            title={new Date(account.updatedAt).toLocaleString()}
          >
            {formatDate(account.updatedAt)}
          </span>
        ) : (
          <span className="text-xs text-muted-foreground">-</span>
        );
      default:
        return <span className="text-xs text-muted-foreground">-</span>;
    }
  };

  // Memoize table state to avoid infinite re-renders
  const stableTableParams = useMemo(() => ({
    currentPage: tableState.currentPage,
    pageSize: tableState.pageSize,
    searchQuery: tableState.searchQuery,
    sort: tableState.sort,
    multiSort: tableState.multiSort,
  }), [
    tableState.currentPage,
    tableState.pageSize,
    tableState.searchQuery,
    tableState.sort,
    tableState.multiSort,
  ]);

  // Handle URL parameters for email ID or campaign ID filtering
  useEffect(() => {
    const emailIds = searchParams?.get('emailIds');
    const campaignId = searchParams?.get('campaignId');
    
    if (emailIds) {
      setEmailIdsFilter(emailIds.split(','));
      setCampaignIdFilter(null);
    } else if (campaignId) {
      setCampaignIdFilter(campaignId);
      setEmailIdsFilter([]);
    } else {
      setEmailIdsFilter([]);
      setCampaignIdFilter(null);
    }
  }, [searchParams]);

  // Use a single useEffect with proper debouncing
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (loadingRef.current) {
        return;
      }

      // Cancel any in-flight requests
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }

      // Create new AbortController for this request
      const abortController = new AbortController();
      abortControllerRef.current = abortController;

      const loadData = async () => {
        loadingRef.current = true;
        
        // Determine what type of load this is by comparing with previous parameters
        const currentParams = {
          page: stableTableParams.currentPage,
          search: stableTableParams.searchQuery,
          clientFilter: selectedClientId ? [selectedClientId] : [],
          userFilter: selectedUserId ? [selectedUserId] : [],
          timeRange: selectedTimeRange.days || 7,
          volumeRange: selectedVolumeRange,
          disconnectedOnly: showDisconnectedOnly,
          worstPerforming: selectedWorstPerformingFilter,
        };
        
        const prevParams = prevParamsRef.current;
        let isPagination = false;
        let isFilterOrSearch = false;
        
        if (!isInitialLoad && emailAccounts.length > 0) {
          // Check if only page changed (pagination)
          isPagination = currentParams.page !== prevParams.page && 
                        currentParams.search === prevParams.search &&
                        JSON.stringify(currentParams.clientFilter) === JSON.stringify(prevParams.clientFilter) &&
                        JSON.stringify(currentParams.userFilter) === JSON.stringify(prevParams.userFilter) &&
                        currentParams.timeRange === prevParams.timeRange &&
                        currentParams.volumeRange === prevParams.volumeRange &&
                        currentParams.disconnectedOnly === prevParams.disconnectedOnly &&
                        currentParams.worstPerforming === prevParams.worstPerforming;
          
          // Check if filters or search changed
          isFilterOrSearch = currentParams.search !== prevParams.search ||
                            JSON.stringify(currentParams.clientFilter) !== JSON.stringify(prevParams.clientFilter) ||
                            JSON.stringify(currentParams.userFilter) !== JSON.stringify(prevParams.userFilter) ||
                            currentParams.timeRange !== prevParams.timeRange ||
                            currentParams.volumeRange !== prevParams.volumeRange ||
                            currentParams.disconnectedOnly !== prevParams.disconnectedOnly ||
                            currentParams.worstPerforming !== prevParams.worstPerforming;
        }
        
        // Update previous parameters
        prevParamsRef.current = { ...currentParams };
        
        // Only show full loading for initial load
        // For pagination and filters, just use the overlay
        if (emailAccounts.length === 0 || isInitialLoad) {
          setLoading(true);
        }
        
        try {
          const params: Record<string, string> = {
            page: stableTableParams.currentPage.toString(),
            pageSize: stableTableParams.pageSize.toString(),
          };

          // Use single-column sorting only
          if (stableTableParams.sort && stableTableParams.sort.column) {
            params.sortBy = mapSortColumn(stableTableParams.sort.column);
            params.sortDirection = stableTableParams.sort.direction;
            params.sortMode = stableTableParams.sort.mode || 'count';
          }

          if (stableTableParams.searchQuery.trim()) {
            params.search = stableTableParams.searchQuery.trim();
          }

          // Add timeRangeDays parameter for time range filtering
          params.timeRangeDays = (selectedTimeRange.days || 7).toString();

          // Add email IDs filter parameters
          if (emailIdsFilter.length > 0) {
            params.emailIds = emailIdsFilter.join(',');
          }
          
          // Add campaign ID filter parameter
          if (campaignIdFilter) {
            params.campaignId = campaignIdFilter;
          }
          
          // Add client filter parameter
          if (selectedClientId) {
            params.filterByClientIds = selectedClientId;
          }

          // Add user filter parameter
          if (selectedUserId) {
            params.filterByUserIds = selectedUserId;
          }

          // Add volume filter parameter (backend filtering)
          if (selectedVolumeRange) {
            const volumeRanges = [
              { id: '10+', minSent: 10 },
              { id: '25+', minSent: 25 },
              { id: '50+', minSent: 50 },
              { id: '100+', minSent: 100 },
              { id: '250+', minSent: 250 },
              { id: '500+', minSent: 500 },
              { id: '1k+', minSent: 1000 },
              { id: '2.5k+', minSent: 2500 },
              { id: '5k+', minSent: 5000 },
              { id: '10k+', minSent: 10000 },
            ];

            const range = volumeRanges.find(r => r.id === selectedVolumeRange);
            if (range) {
              params.minSent = range.minSent.toString();
            }
          }

          // Add disconnected accounts filter parameter
          if (showDisconnectedOnly) {
            params.warmupStatus = 'inactive';
          }

          // Add worst performing filter parameters
          if (selectedWorstPerformingFilter) {
            const performanceFilters = [
              { id: 'poor-100', minSent: 100, maxReplyRate: 2 },
              { id: 'poor-250', minSent: 250, maxReplyRate: 2 },
              { id: 'poor-500', minSent: 500, maxReplyRate: 2 },
              { id: 'poor-1k', minSent: 1000, maxReplyRate: 2 },
              { id: 'worst-100', minSent: 100, maxReplyRate: 1 },
              { id: 'worst-250', minSent: 250, maxReplyRate: 1 },
              { id: 'worst-500', minSent: 500, maxReplyRate: 1 },
              { id: 'worst-1k', minSent: 1000, maxReplyRate: 1 },
            ];

            const filter = performanceFilters.find(f => f.id === selectedWorstPerformingFilter);
            if (filter) {
              params.performanceFilterMinSent = filter.minSent.toString();
              params.performanceFilterMaxReplyRate = filter.maxReplyRate.toString();
            }
          }

          const response = await apiClient.get<PaginatedResponse<EmailAccount>>(
            ENDPOINTS.emailAccounts,
            params,
            { signal: abortController.signal }
          );

          setEmailAccounts(response.data);
          updateTableData({
            totalPages: response.totalPages,
            totalCount: response.totalCount,
          });
          
          // Store original total count when no search is active
          if (!stableTableParams.searchQuery.trim()) {
            setOriginalTotalCount(response.totalCount);
          }
          
          // Handle success to reset error state
          handleSuccess();
          
          // Mark initial load as complete
          if (isInitialLoad) {
            setIsInitialLoad(false);
          }
        } catch (err: any) {
          // Ignore abort errors - they're expected when requests are canceled
          if (err.name === 'AbortError') {
            return;
          }
          handleApiError(err, 'load email accounts');
        } finally {
          setLoading(false);
          loadingRef.current = false;
        }
      };

      loadData();
    }, 0); // No debounce for non-search operations
    
    // Cleanup function
    return () => {
      clearTimeout(timeoutId);
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [stableTableParams, refreshTrigger, emailIdsFilter, campaignIdFilter, selectedClientId, selectedUserId, selectedVolumeRange, selectedTimeRange.days, showDisconnectedOnly, selectedWorstPerformingFilter]); // Include filters


  return (
    <div className="flex h-full flex-col bg-background/95">
      <PageHeader 
        title="Email Accounts"
        description={
          emailIdsFilter.length > 0 
            ? `Showing ${emailIdsFilter.length} specific email accounts - Monitor sender reputation and deliverability metrics`
            : campaignIdFilter
            ? `Showing email accounts for campaign - Monitor sender reputation and deliverability metrics`
            : (selectedClientId || selectedVolumeRange)
            ? `Filtered by ${[
                ...(selectedClientId ? ['client'] : []),
                ...(selectedVolumeRange ? ['volume range'] : [])
              ].join(' and ')} - Monitor sender reputation and deliverability metrics`
            : "Monitor sender reputation, deliverability metrics, and manage client assignments"
        }
        mobileDescription="Email management"
        icon={Mail}
        itemCount={tableState.totalCount}
        itemLabel="accounts"
        originalTotalCount={originalTotalCount}
        searchQuery={tableState.searchQuery}
        actions={null}
      />

      <DataTableToolbar
        searchPlaceholder="Search email accounts..."
        searchQuery={tableState.searchQuery}
        onSearch={handleSearch}
        columns={columnDefinitions}
        visibleColumns={visibleColumns}
        onColumnToggle={toggleColumn}
        onResetColumns={resetColumns}
        onResetWidths={resetColumnWidths}
        selectedCount={tableState.selectedItems.size}
        totalCount={tableState.totalCount}
        originalTotalCount={originalTotalCount}
        itemLabel="accounts"
        onBulkAssign={isAdmin ? () => handleOpenAssignClientModal() : undefined}
        onClearSelection={clearSelection}
        showBulkAssign={isAdmin}
        sortConfig={tableState.sort}
        multiSort={tableState.multiSort}
        defaultSort={defaultSort}
        onResetSort={resetSort}
        onClearAllSorts={clearAllSorts}
        customColumnAction={
          <ColumnCustomizationModal
            columns={columnDefinitions}
            visibleColumns={visibleColumns}
            columnOrder={columnOrder}
            onColumnToggle={toggleColumn}
            onColumnReorder={reorderColumns}
            onResetColumns={resetColumns}
            onResetOrder={resetColumnOrder}
            title="Customize Email Account Columns"
            description="Show, hide, and reorder columns to customize your email accounts view."
          />
        }
        customActions={
          <>
            {isAdmin && (
              <UserSearchFilter
                selectedUserId={selectedUserId}
                onSelectionChange={(userId) => {
                  setSelectedUserId(userId);
                  setRefreshTrigger(prev => prev + 1);
                }}
                placeholder="All users"
                variant="compact"
                className="h-9"
              />
            )}
            <ClientSearchFilter
              selectedClientId={selectedClientId}
              onSelectionChange={(clientId) => {
                setSelectedClientId(clientId);
                setRefreshTrigger(prev => prev + 1);
              }}
              placeholder="All clients"
              variant="compact"
              className="h-9"
              filterByUserId={selectedUserId}
            />
            <VolumeFilter
              selectedVolumeRange={selectedVolumeRange}
              onSelectionChange={(volumeRange) => {
                setSelectedVolumeRange(volumeRange);
                localStorage.setItem('email-accounts-volume-filter', JSON.stringify(volumeRange));
                setRefreshTrigger(prev => prev + 1);
              }}
              placeholder="All volumes"
              variant="compact"
              className="h-9"
            />
            <DisconnectedAccountsFilter
              showDisconnected={showDisconnectedOnly}
              onToggle={(showDisconnected) => {
                setShowDisconnectedOnly(showDisconnected);
                setRefreshTrigger(prev => prev + 1);
              }}
              placeholder="All accounts"
              variant="compact"
              className="h-9"
            />
            <WorstPerformingFilter
              selectedFilter={selectedWorstPerformingFilter}
              onSelectionChange={(filterId) => {
                setSelectedWorstPerformingFilter(filterId);
                setRefreshTrigger(prev => prev + 1);
              }}
              placeholder="All accounts"
              variant="compact"
              className="h-9"
            />
            <TimeRangeSelector
              selectedTimeRange={selectedTimeRange}
              onTimeRangeChange={(option) => {
                setSelectedTimeRange(option);
                localStorage.setItem('email-accounts-time-range-preference', JSON.stringify(option));
                // Refresh data with new time range
                setRefreshTrigger(prev => prev + 1);
              }}
              description="Choose the time range for displaying recent email account statistics in columns"
              examples={['2 weeks', '6 months', '1 year']}
            />
          </>
        }
        onDownload={handleDownload}
      />


      <DataTable
        data={emailAccounts}
        columns={columnDefinitions}
        visibleColumns={visibleColumns}
        orderedVisibleColumns={getOrderedVisibleColumns()}
        loading={loading}
        error={error || undefined}
        selectedItems={tableState.selectedItems}
        sortConfig={tableState.sort}
        multiSort={tableState.multiSort}
        onSelectAll={handleSelectAllWithData}
        onSelectOne={handleSelectOne}
        onSort={handleSort}
        onRetry={loadEmailAccounts}
        renderCell={renderEmailAccountCell}
        emptyMessage="No email accounts found"
        getId={(account) => account.id}
        getColumnStyle={getColumnStyle}
        onColumnResize={handleMouseDown}
      />

      <TablePagination
        currentPage={tableState.currentPage}
        totalPages={tableState.totalPages}
        totalCount={tableState.totalCount}
        pageSize={tableState.pageSize}
        pageSizeOptions={pageSizeOptions}
        onPageChange={handlePageChange}
        onPageSizeChange={handlePageSizeChange}
      />

      <AssignClientModal
        isOpen={isAssignClientModalOpen}
        onClose={() => setIsAssignClientModalOpen(false)}
        selectedItems={Array.from(tableState.selectedItems).map(id => emailAccounts.find(a => a.id === id)).filter((item): item is EmailAccount => Boolean(item))}
        selectedItemForAssignment={selectedAccountForAssignment}
        clients={clients}
        isLoading={clientsLoading}
        isAssigning={isAssigning}
        onAssign={handleAssignClient}
        entityType="account"
        getEntityName={(account) => account.email}
      />

      <NotesModal
        isOpen={isNotesModalOpen}
        onClose={() => setIsNotesModalOpen(false)}
        itemType="emailAccount"
        itemId={selectedAccountForNotes?.id || ''}
        itemName={selectedAccountForNotes?.email || ''}
        initialNotes={selectedAccountForNotes?.notes || null}
        onNotesUpdated={handleNotesUpdated}
      />

      {/* Unassign Client Confirmation Dialog */}
      <ConfirmationDialog
        open={confirmDialog.isOpen}
        onOpenChange={confirmDialog.setIsOpen}
        title="Unassign Client"
        description={
          accountToUnassign 
            ? `Are you sure you want to unassign the client from "${accountToUnassign.email}"? The email account will no longer be associated with any client.`
            : ''
        }
        confirmLabel="Unassign Client"
        cancelLabel="Cancel"
        variant="warning"
        onConfirm={handleConfirmUnassign}
        loading={isUnassigning}
      />

      <DownloadModal
        isOpen={showDownloadModal}
        onClose={() => setShowDownloadModal(false)}
        entityType="email-accounts"
        entityLabel="email accounts"
      />
    </div>
  );
}