'use client';

import React, { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { useSearchParams } from 'next/navigation';
import { usePageTitle } from '@/hooks/use-page-title';
import Link from 'next/link';
import { UserPlus, UserX, Building2, UserMinus, Megaphone, Download, Calendar, Tags, Clock, AtSign, FileText } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
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
import { WorstPerformingCampaignFilter } from '@/components/worst-performing-campaign-filter';
import { ConfirmationDialog, useConfirmationDialog } from '@/components/confirmation-dialog';
import { ColumnCustomizationModal } from '@/components/column-customization-modal';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useColumnVisibility } from '@/hooks/use-column-visibility';
import { useDataTable } from '@/hooks/use-data-table';
import { useErrorHandling } from '@/hooks/use-error-handling';
import { useResizableColumns } from '@/hooks/use-resizable-columns';
import { apiClient, ENDPOINTS, formatDate, debounce, PaginatedResponse } from '@/lib/api';
import { useAuth } from '@/contexts/auth-context';
import { Campaign, Client, SortConfig, ColumnDefinition, TableState, ClientListItem, UserListItem } from '@/types';
import { ClientSelectionDropdown } from '@/components/client-selection-dropdown';
import { TagSelector } from '@/components/tag-selector';
import { TimeRangeSelector, TimeRangeOption } from '@/components/time-range-selector';
import { DownloadModal } from '@/components/download-modal';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

// Move columnDefinitions inside component to access selectedTimeRange
// Will be defined inside the component

const defaultSort: SortConfig = {
  column: 'createdAt',
  direction: 'desc',
  mode: 'count',
};

export default function CampaignsPage() {
  usePageTitle('Campaigns - LeadHype');
  const router = useRouter();
  const searchParams = useSearchParams();
  const { isAdmin } = useAuth();
  const { error, handleApiError, handleSuccess } = useErrorHandling({ resetOnSuccess: true, showToast: false });
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const prevParamsRef = useRef<{
    page: number;
    search: string;
    clientFilter: string[];
    userFilter: string[];
    timeRange: number;
    worstPerforming: string | null;
  }>({
    page: 1,
    search: '',
    clientFilter: [],
    userFilter: [],
    timeRange: 1,
    worstPerforming: null,
  });
  const [clientsLoading, setClientsLoading] = useState(false);
  const [isAssignClientModalOpen, setIsAssignClientModalOpen] = useState(false);
  const [selectedCampaignForAssignment, setSelectedCampaignForAssignment] = useState<Campaign | null>(null);
  const [isAssigning, setIsAssigning] = useState(false);
  const [originalTotalCount, setOriginalTotalCount] = useState<number>(0);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  
  // Notes modal state
  const [isNotesModalOpen, setIsNotesModalOpen] = useState(false);
  const [selectedCampaignForNotes, setSelectedCampaignForNotes] = useState<Campaign | null>(null);
  
  // Confirmation dialog state for unassigning client
  const confirmDialog = useConfirmationDialog();
  const [campaignToUnassign, setCampaignToUnassign] = useState<Campaign | null>(null);
  const [isUnassigning, setIsUnassigning] = useState(false);
  
  // Download modal state
  const [showDownloadModal, setShowDownloadModal] = useState(false);
  
  const [selectedTimeRange, setSelectedTimeRange] = useState<TimeRangeOption>(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('campaigns-time-range-preference');
      return saved ? JSON.parse(saved) : { value: '24h', label: '24 Hours', days: 1 };
    }
    return { value: '24h', label: '24 Hours', days: 1 };
  });
  
  // Email account filter state
  const [emailAccountFilter, setEmailAccountFilter] = useState<string>('');
  
  // Client filter state for enhanced search (single selection)
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  
  // User filter state for enhanced search (single selection)
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  
  // Worst performing filter state
  const [selectedWorstPerformingFilter, setSelectedWorstPerformingFilter] = useState<string | null>(null);
  
  const abortControllerRef = useRef<AbortController | null>(null);
  const loadingRef = useRef(false);

  const pageSizeOptions = [10, 25, 50, 100, 200];


  // Dynamic column definitions based on selected time range - memoized to prevent infinite re-renders
  const columnDefinitions = useMemo((): Record<string, ColumnDefinition> => {
    const timeLabel = selectedTimeRange.days === 1 ? '24h' : 
                     selectedTimeRange.days === 9999 ? 'All Time' : 
                     `${selectedTimeRange.days}d`;
    
    return {
      select: { label: '', sortable: false, required: true },
      campaign: { label: 'Campaign', sortable: true, required: true },
      client: { label: 'Client', sortable: true },
      tags: { label: 'Tags', sortable: true, required: true },
      tagcount: { label: 'Tag Count', sortable: true },
      totalLeads: { label: 'Total Leads', sortable: true, required: true },
      emailAccounts: { label: 'Email Accounts', sortable: true },
      // Main columns now show data for selected time range
      totalSent: { label: `Sent (${timeLabel})`, sortable: true },
      totalOpened: { label: `Opened (${timeLabel})`, sortable: true, dualSort: true },
      totalReplied: { label: `Replied (${timeLabel})`, sortable: true, dualSort: true },
      totalPositiveReplies: { label: `Positive Reply (${timeLabel})`, sortable: true, dualSort: true },
      totalBounced: { label: `Bounced (All Time)`, sortable: true, dualSort: true },
      totalClicked: { label: `Clicked (${timeLabel})`, sortable: true, dualSort: true },
      status: { label: 'Status', sortable: true },
      notes: { label: 'Notes', sortable: true, required: true },
      createdAt: { label: 'Created', sortable: true },
      lastUpdatedAt: { label: 'Last Updated', sortable: true, required: true },
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
    storageKey: 'campaigns',
    defaultPageSize: 50,
    defaultSort,
  });

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
    storageKey: 'campaigns-visible-columns',
    orderStorageKey: 'campaigns-column-order',
  });

  // Resizable columns management
  const defaultColumnWidths = {
    select: 40,
    campaign: 200,
    client: 150,
    totalLeads: 100,
    emailAccounts: 120,
    totalSent: 80,
    totalOpened: 100,
    totalReplied: 100,
    totalPositiveReplies: 120,
    totalBounced: 100,
    totalClicked: 100,
    sent24Hours: 80,
    opened24Hours: 100,
    replied24Hours: 100,
    bounced24h: 100,
    sent7Days: 80,
    opened7Days: 100,
    replied7Days: 100,
    bounced7d: 100,
    status: 100,
    tags: 200,
    tagcount: 80,
    createdAt: 100,
    lastUpdatedAt: 100,
  };

  const {
    columnWidths,
    updateColumnWidth,
    resetColumnWidths,
    handleMouseDown,
    getColumnStyle,
  } = useResizableColumns({
    storageKey: 'campaigns-column-widths',
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
      // Error handling is managed by loadCampaigns to avoid duplicate errors
    } finally {
      setClientsLoading(false);
    }
  }, []);

  const loadUsers = useCallback(async () => {
    try {
      const response = await apiClient.get<PaginatedResponse<UserListItem>>(ENDPOINTS.userList, {
        limit: '1000' // Load all users for the dropdown
      });
      const usersData = response.data || response;
      setUsers(Array.isArray(usersData) ? usersData : []);
    } catch (error) {
      console.error('Failed to load users:', error);
      setUsers([]);
    }
  }, []);

  // We'll keep a simple loadCampaigns for manual refresh
  const loadCampaigns = useCallback(() => {
    setRefreshTrigger(prev => prev + 1);
  }, []);

  // Search is now debounced in DataTableToolbar component


  const showSortModeNotification = (mode: string, direction: string) => {
    toast.info(`Sorting by ${mode} ${direction === 'asc' ? '↑' : '↓'}`);
  };

  const handleOpenAssignClientModal = (campaign?: Campaign) => {
    if (!isAdmin) {
      toast.error('Access denied: Only administrators can assign or unassign clients');
      return;
    }
    if (campaign) {
      setSelectedCampaignForAssignment(campaign);
    } else {
      setSelectedCampaignForAssignment(null);
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
      if (selectedCampaignForAssignment) {
        // Single campaign assignment - Use update endpoint with Name and ClientId
        await apiClient.put(`${ENDPOINTS.campaigns}/${selectedCampaignForAssignment.id}`, {
          Name: selectedCampaignForAssignment.name,
          ClientId: clientId
        });
        
        toast.success('Client assigned successfully');
      } else {
        // Bulk assignment using new bulk endpoint
        const selectedCampaignIds = Array.from(tableState.selectedItems);
        
        await apiClient.put(`${ENDPOINTS.campaigns}/bulk-assign-client`, {
          CampaignIds: selectedCampaignIds,
          ClientId: clientId
        });
        
        toast.success(`Client assigned to ${selectedCampaignIds.length} campaigns`);
        
        // Clear selection
        clearSelection();
      }
      
      // Refresh data
      loadCampaigns();
      setIsAssignClientModalOpen(false);
    } catch (error) {
      console.error('Failed to assign client:', error);
      toast.error('Failed to assign client');
    } finally {
      setIsAssigning(false);
    }
  };

  const handleUnassignClient = (campaign: Campaign) => {
    if (!isAdmin) {
      toast.error('Access denied: Only administrators can assign or unassign clients');
      return;
    }
    setCampaignToUnassign(campaign);
    confirmDialog.openDialog();
  };

  const handleConfirmUnassign = async () => {
    if (!campaignToUnassign) return;

    setIsUnassigning(true);
    try {
      // Update campaign with null ClientId to unassign
      await apiClient.put(`${ENDPOINTS.campaigns}/${campaignToUnassign.id}`, {
        Name: campaignToUnassign.name,
        ClientId: null
      });
      
      toast.success('Client unassigned successfully');
      
      // Refresh data
      loadCampaigns();
      confirmDialog.closeDialog();
      setCampaignToUnassign(null);
    } catch (error) {
      console.error('Failed to unassign client:', error);
      toast.error('Failed to unassign client');
    } finally {
      setIsUnassigning(false);
    }
  };

  const handleNotesUpdated = (notes: string | null) => {
    if (selectedCampaignForNotes) {
      setCampaigns(prevCampaigns =>
        prevCampaigns.map(campaign =>
          campaign.id === selectedCampaignForNotes.id
            ? { ...campaign, notes: notes ?? undefined }
            : campaign
        )
      );
    }
  };

  const handleDownload = () => {
    setShowDownloadModal(true);
  };

  // Update select all to work with current data
  const handleSelectAllWithData = (checked: boolean) => {
    if (checked) {
      campaigns.forEach(campaign => handleSelectOne(campaign.id, true));
    } else {
      campaigns.forEach(campaign => handleSelectOne(campaign.id, false));
    }
  };

  const renderCampaignCell = (campaign: Campaign, columnKey: string) => {
    const style = getColumnStyle(columnKey);
    
    switch (columnKey) {
      case 'select':
        return (
          <div style={style}>
            <Checkbox
              checked={tableState.selectedItems.has(campaign.id)}
              onCheckedChange={(checked) => handleSelectOne(campaign.id, checked as boolean)}
            />
          </div>
        );
      case 'campaign':
        return (
          <div style={style}>
            <Link
              href={`/campaigns/${campaign.campaignId}`}
              className="h-auto p-0 justify-start hover:bg-transparent group w-full text-left block"
            >
              <div className="flex items-center">
                <div className="mr-2 flex h-6 w-6 sm:h-6 sm:w-6 items-center justify-center rounded-full bg-primary/10 flex-shrink-0 group-hover:bg-primary/20 transition-colors">
                  <span className="text-[10px] sm:text-[10px] font-semibold text-primary">
                    {campaign.name?.charAt(0) || '?'}
                  </span>
                </div>
                <span 
                  className="text-xs font-medium truncate group-hover:text-primary transition-colors" 
                  title={campaign.name || 'Unnamed Campaign'}
                >
                  {campaign.name || 'Unnamed Campaign'}
                </span>
              </div>
            </Link>
          </div>
        );
      case 'client':
        return (
          <div style={style}>
            {campaign.clientName ? (
              <div className="group relative inline-flex items-center">
                <div 
                  className="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium border cursor-pointer transition-all duration-200 group-hover:pr-7 hover:opacity-80 max-w-full"
                  onClick={isAdmin ? () => handleOpenAssignClientModal(campaign) : undefined}
                  title={isAdmin ? `${campaign.clientName} - Click to change client` : `${campaign.clientName} (View Only)`}
                  style={{
                    backgroundColor: campaign.clientColor ? `${campaign.clientColor}15` : '#3B82F615',
                    color: campaign.clientColor || '#3B82F6',
                    borderColor: campaign.clientColor ? `${campaign.clientColor}40` : '#3B82F640',
                    cursor: isAdmin ? 'pointer' : 'default'
                  }}
                >
                  <Building2 className="w-3 h-3 mr-1 flex-shrink-0" />
                  <span className="truncate">{campaign.clientName}</span>
                </div>
                {isAdmin && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleUnassignClient(campaign);
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
                onClick={() => handleOpenAssignClientModal(campaign)}
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
            )}
          </div>
        );
      case 'totalLeads':
        return (
          <div style={style}>
            <span className="text-xs font-medium">{campaign.totalLeads}</span>
          </div>
        );
      case 'emailAccounts':
        return (
          <div style={style}>
            {campaign.emailIds && campaign.emailIds.length > 0 ? (
              <Link
                href={`/email-accounts?campaignId=${campaign.campaignId}`}
                className="flex items-center text-xs font-medium hover:text-primary transition-colors"
                onClick={(e) => e.stopPropagation()}
              >
                <AtSign className="w-3 h-3 mr-1 flex-shrink-0" />
                <span>{campaign.emailIds.length}</span>
              </Link>
            ) : null}
          </div>
        );
      case 'totalSent':
        return (
          <div style={style}>
            <span className="text-xs font-medium">{campaign.totalSent}</span>
          </div>
        );
      case 'totalOpened':
        return (
          <div style={style}>
            <StatsCell count={campaign.totalOpened || 0} baseValue={campaign.totalSent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="opened" />
          </div>
        );
      case 'totalReplied':
        return (
          <div style={style}>
            <StatsCell count={campaign.totalReplied || 0} baseValue={campaign.totalSent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />
          </div>
        );
      case 'totalPositiveReplies':
        return (
          <div style={style}>
            <StatsCell count={campaign.totalPositiveReplies || 0} baseValue={campaign.totalReplied || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />
          </div>
        );
      case 'totalBounced':
        return (
          <div style={style}>
            <StatsCell count={campaign.totalBounced || 0} baseValue={campaign.totalSent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="bounced" />
          </div>
        );
      case 'totalClicked':
        return (
          <div style={style}>
            <StatsCell count={campaign.totalClicked || 0} baseValue={campaign.totalSent || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="clicked" />
          </div>
        );
      // 24h stats
      case 'sent24Hours':
        return (
          <div style={style}>
            <span className="text-xs font-medium">{campaign.sent24Hours || 0}</span>
          </div>
        );
      case 'opened24Hours':
        return (
          <div style={style}>
            <StatsCell count={campaign.opened24Hours || 0} baseValue={campaign.sent24Hours || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="opened" />
          </div>
        );
      case 'replied24Hours':
        return (
          <div style={style}>
            <StatsCell count={campaign.replied24Hours || 0} baseValue={campaign.sent24Hours || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />
          </div>
        );
      // case 'bounced24h': // No day-by-day bounced data available
      //   return (
      //     <div style={style}>
      //       <StatsCell count={campaign.bounced24Hours || 0} baseValue={campaign.sent24Hours || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="bounced" />
      //     </div>
      //   );
      // 7d stats
      case 'sent7Days':
        return (
          <div style={style}>
            <span className="text-xs font-medium">{campaign.sent7Days || 0}</span>
          </div>
        );
      case 'opened7Days':
        return (
          <div style={style}>
            <StatsCell count={campaign.opened7Days || 0} baseValue={campaign.sent7Days || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="opened" />
          </div>
        );
      case 'replied7Days':
        return (
          <div style={style}>
            <StatsCell count={campaign.replied7Days || 0} baseValue={campaign.sent7Days || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="replied" />
          </div>
        );
      // case 'bounced7d': // No day-by-day bounced data available
      //   return (
      //     <div style={style}>
      //       <StatsCell count={campaign.bounced7Days || 0} baseValue={campaign.sent7Days || 0} column={columnKey} sortConfig={tableState.sort} colorScheme="bounced" />
      //     </div>
      //   );
      case 'notes':
        return (
          <div style={style}>
            <Button
              variant="ghost"
              size="sm"
              className={`h-8 w-8 p-0 ${campaign.notes ? 'text-blue-600 hover:text-blue-700' : 'text-muted-foreground hover:text-foreground'}`}
              onClick={() => {
                setSelectedCampaignForNotes(campaign);
                setIsNotesModalOpen(true);
              }}
              title={campaign.notes ? 'View/edit notes' : 'Add notes'}
            >
              <FileText className="h-4 w-4" />
            </Button>
          </div>
        );
      case 'status':
        return (
          <div style={style}>
            <StatusBadge status={campaign.status} type="campaign" />
          </div>
        );
      case 'tags':
        return (
          <div style={style}>
            <TagSelector
              entityType="campaign"
              entityId={campaign.id}
              selectedTags={campaign.tags || []}
              onTagsChange={(tags) => {
                // Update the campaign tags in the local state
                setCampaigns(prev => prev.map(c => 
                  c.id === campaign.id ? { ...c, tags } : c
                ));
              }}
            />
          </div>
        );
      case 'tagcount':
        return (
          <div style={style}>
            <span className="text-sm font-medium">
              {campaign.tags?.length || 0}
            </span>
          </div>
        );
      case 'createdAt':
        return (
          <div style={style}>
            <span 
              className="text-xs text-muted-foreground truncate block"
              title={new Date(campaign.createdAt).toLocaleString()}
            >
              {formatDate(campaign.createdAt)}
            </span>
          </div>
        );
      case 'lastUpdatedAt':
        return (
          <div style={style}>
            {campaign.lastUpdatedAt ? (
              <span 
                className="text-xs text-muted-foreground cursor-pointer truncate block"
                title={new Date(campaign.lastUpdatedAt).toLocaleString()}
              >
                {formatDate(campaign.lastUpdatedAt)}
              </span>
            ) : (
              <span className="text-xs text-muted-foreground">-</span>
            )}
          </div>
        );
      default:
        return (
          <div style={style}>
            <span className="text-xs text-muted-foreground">-</span>
          </div>
        );
    }
  };

  // Load users when component mounts or when needed for filtering
  useEffect(() => {
    if (isAdmin && users.length === 0) {
      loadUsers();
    }
  }, [isAdmin, users.length, loadUsers]);

  // Handle URL parameters for email account and client filtering
  useEffect(() => {
    const emailAccountId = searchParams?.get('emailAccountId');
    const clientId = searchParams?.get('client');
    
    if (emailAccountId) {
      setEmailAccountFilter(emailAccountId);
    } else {
      setEmailAccountFilter('');
    }

    if (clientId) {
      setSelectedClientId(clientId);
    }
  }, [searchParams]);

  // Load data without debounce for non-search changes
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
        page: tableState.currentPage,
        search: tableState.searchQuery,
        clientFilter: selectedClientId ? [selectedClientId] : [],
        userFilter: selectedUserId ? [selectedUserId] : [],
        timeRange: selectedTimeRange.days || 1,
        worstPerforming: selectedWorstPerformingFilter,
      };
      
      const prevParams = prevParamsRef.current;
      let isPagination = false;
      let isFilterOrSearch = false;
      
      if (!isInitialLoad && campaigns.length > 0) {
        // Check if only page changed (pagination)
        isPagination = currentParams.page !== prevParams.page && 
                      currentParams.search === prevParams.search &&
                      JSON.stringify(currentParams.clientFilter) === JSON.stringify(prevParams.clientFilter) &&
                      JSON.stringify(currentParams.userFilter) === JSON.stringify(prevParams.userFilter) &&
                      currentParams.timeRange === prevParams.timeRange &&
                      currentParams.worstPerforming === prevParams.worstPerforming;
        
        // Check if filters or search changed
        isFilterOrSearch = currentParams.search !== prevParams.search ||
                          JSON.stringify(currentParams.clientFilter) !== JSON.stringify(prevParams.clientFilter) ||
                          JSON.stringify(currentParams.userFilter) !== JSON.stringify(prevParams.userFilter) ||
                          currentParams.timeRange !== prevParams.timeRange ||
                          currentParams.worstPerforming !== prevParams.worstPerforming;
      }
      
      // Update previous parameters
      prevParamsRef.current = { ...currentParams };
      
      // Only show full loading for initial load
      // For pagination and filters, just use the overlay
      if (campaigns.length === 0 || isInitialLoad) {
        setLoading(true);
      }
      
      const params: Record<string, string> = {
        page: tableState.currentPage.toString(),
        pageSize: tableState.pageSize.toString(),
      };

      // Use single-column sorting only
      if (tableState.sort && tableState.sort.column) {
        params.sortBy = tableState.sort.column;
        params.sortDirection = tableState.sort.direction;
        params.sortMode = tableState.sort.mode || 'count';
      }

      if (tableState.searchQuery.trim()) {
        params.search = tableState.searchQuery.trim();
      }

      // Add timeRangeDays parameter for time range filtering
      params.timeRangeDays = (selectedTimeRange.days || 1).toString();

      // Add email account filter parameter
      if (emailAccountFilter) {
        params.emailAccountId = emailAccountFilter;
      }
      
      // Add client filter parameter
      if (selectedClientId) {
        params.filterByClientIds = selectedClientId;
      }

      // Add user filter parameter
      if (selectedUserId) {
        params.filterByUserIds = selectedUserId;
      }

      // Add worst performing filter parameters
      if (selectedWorstPerformingFilter) {
        const performanceFilters = [
          { id: 'poor-100', minSent: 100, maxReplyRate: 2 },
          { id: 'poor-250', minSent: 250, maxReplyRate: 2 },
          { id: 'poor-500', minSent: 500, maxReplyRate: 2 },
          { id: 'poor-1k', minSent: 1000, maxReplyRate: 2 },
          { id: 'poor-2.5k', minSent: 2500, maxReplyRate: 2 },
          { id: 'worst-100', minSent: 100, maxReplyRate: 1 },
          { id: 'worst-250', minSent: 250, maxReplyRate: 1 },
          { id: 'worst-500', minSent: 500, maxReplyRate: 1 },
          { id: 'worst-1k', minSent: 1000, maxReplyRate: 1 },
          { id: 'worst-2.5k', minSent: 2500, maxReplyRate: 1 },
        ];

        const filter = performanceFilters.find(f => f.id === selectedWorstPerformingFilter);
        if (filter) {
          params.performanceFilterMinSent = filter.minSent.toString();
          params.performanceFilterMaxReplyRate = filter.maxReplyRate.toString();
        }
      }
      
      try {
        const response = await apiClient.get<PaginatedResponse<Campaign>>(
          ENDPOINTS.campaigns,
          params,
          { signal: abortController.signal }
        );

        setCampaigns(response.data);
        updateTableData({
          totalPages: response.totalPages,
          totalCount: response.totalCount,
        });
        
        // Store original total count when no search is active
        if (!tableState.searchQuery.trim()) {
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
        
        console.error('Initial campaigns load failed:', err);
        handleApiError(err, 'load campaigns');
      } finally {
        setLoading(false);
        loadingRef.current = false;
      }
    };

      loadData();
    }, 50); // Small delay to prevent race conditions
    
    // Cleanup function
    return () => {
      clearTimeout(timeoutId);
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [tableState.currentPage, tableState.pageSize, tableState.searchQuery, tableState.multiSort, refreshTrigger, selectedTimeRange.days, emailAccountFilter, selectedClientId, selectedUserId, selectedWorstPerformingFilter]); // Include filters


  return (
    <div className="flex h-full flex-col bg-background/95">
      <PageHeader 
        title="Email Campaigns"
        description={
          selectedClientId
            ? `Filtered by client - Create, manage, and monitor email outreach campaigns`
            : "Create, manage, and monitor your email outreach campaigns across all clients"
        }
        mobileDescription="Campaign management"
        icon={Megaphone}
        itemCount={tableState.totalCount}
        itemLabel="campaigns"
        originalTotalCount={originalTotalCount}
        searchQuery={tableState.searchQuery}
        actions={null}
      />

      <DataTableToolbar
        searchPlaceholder="Search by name, client, status, or tags..."
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
        itemLabel="campaigns"
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
            title="Customize Campaign Columns"
            description="Show, hide, and reorder columns to customize your campaigns view."
          />
        }
        customActions={
          <>
            {isAdmin && (
              <UserSearchFilter
                selectedUserId={selectedUserId}
                onSelectionChange={(userId) => {
                  setSelectedUserId(userId);
                  // Clear client selection when user changes to ensure consistency
                  if (selectedClientId && userId !== selectedUserId) {
                    setSelectedClientId(null);
                  }
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
            <WorstPerformingCampaignFilter
              selectedFilter={selectedWorstPerformingFilter}
              onSelectionChange={(filterId) => {
                setSelectedWorstPerformingFilter(filterId);
                loadCampaigns();
              }}
              placeholder="All campaigns"
              variant="compact"
              className="h-9"
            />
            <TimeRangeSelector
              selectedTimeRange={selectedTimeRange}
              onTimeRangeChange={(option) => {
                setSelectedTimeRange(option);
                localStorage.setItem('campaigns-time-range-preference', JSON.stringify(option));
                // Refresh data with new time range
                loadCampaigns();
              }}
              description="Choose the time range for displaying recent campaign statistics in columns"
              examples={['2 weeks', '6 months', '1 year']}
            />
          </>
        }
        onDownload={handleDownload}
      />

      <DataTable
        data={campaigns}
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
        onRetry={loadCampaigns}
        renderCell={renderCampaignCell}
        emptyMessage="No campaigns found"
        getId={(campaign) => campaign.id}
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
        selectedItems={Array.from(tableState.selectedItems).map(id => campaigns.find(c => c.id === id)).filter((c): c is Campaign => Boolean(c))}
        selectedItemForAssignment={selectedCampaignForAssignment}
        clients={clients}
        isLoading={clientsLoading}
        isAssigning={isAssigning}
        onAssign={handleAssignClient}
        entityType="campaign"
        getEntityName={(campaign) => campaign.name}
      />

      <NotesModal
        isOpen={isNotesModalOpen}
        onClose={() => setIsNotesModalOpen(false)}
        itemType="campaign"
        itemId={selectedCampaignForNotes?.id || ''}
        itemName={selectedCampaignForNotes?.name || ''}
        initialNotes={selectedCampaignForNotes?.notes || null}
        onNotesUpdated={handleNotesUpdated}
      />

      {/* Unassign Client Confirmation Dialog */}
      <ConfirmationDialog
        open={confirmDialog.isOpen}
        onOpenChange={confirmDialog.setIsOpen}
        title="Unassign Client"
        description={
          campaignToUnassign 
            ? `Are you sure you want to unassign the client from "${campaignToUnassign.name}"? The campaign will no longer be associated with any client.`
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
        entityType="campaigns"
        entityLabel="campaigns"
      />

    </div>
  );
}