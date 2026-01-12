'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { usePageTitle } from '@/hooks/use-page-title';
import { useAuth } from '@/contexts/auth-context';
import { Search, Plus, Building2, Users, Mail, Edit, Trash2, CheckCircle, XCircle, WifiOff, RefreshCw, AlertCircle, MoreVertical } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { TablePagination } from '@/components/table-pagination';
import { DataTableToolbar } from '@/components/data-table-toolbar';
import { DataTable } from '@/components/data-table';
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
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { PageHeader } from '@/components/page-header';
import { toast } from 'sonner';
import { ConfirmationDialog, useConfirmationDialog } from '@/components/confirmation-dialog';
import { useErrorHandling } from '@/hooks/use-error-handling';
import { useColumnVisibility } from '@/hooks/use-column-visibility';
import { useDataTable } from '@/hooks/use-data-table';
import { useResizableColumns } from '@/hooks/use-resizable-columns';
import { apiClient, ENDPOINTS, formatDate, debounce, PaginatedResponse } from '@/lib/api';
import { Client, SortConfig, ColumnDefinition, TableState } from '@/types';

const columnDefinitions: Record<string, ColumnDefinition> = {
  name: { label: 'Client Name', sortable: true, required: true },
  email: { label: 'Email', sortable: true },
  company: { label: 'Company', sortable: true },
  status: { label: 'Status', sortable: true, required: true },
  campaignCount: { label: 'Campaigns', sortable: true },
  emailAccountCount: { label: 'Email Accounts', sortable: true },
  createdAt: { label: 'Created Date', sortable: true },
  actions: { label: 'Actions', sortable: false, required: true },
};

const defaultSort: SortConfig = {
  column: 'name',
  direction: 'asc',
  mode: 'count',
};

export default function ClientsPage() {
  usePageTitle('Clients - LeadHype');
  const router = useRouter();
  const { isAdmin } = useAuth();
  const { error, handleApiError, handleSuccess } = useErrorHandling({ resetOnSuccess: true, showToast: false });
  const [clients, setClients] = useState<Client[]>([]);
  const [loading, setLoading] = useState(true);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const [showAddModal, setShowAddModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [newClient, setNewClient] = useState({
    name: '',
    email: '',
    company: '',
    notes: ''
  });
  const [editingClient, setEditingClient] = useState<Client | null>(null);
  const [editClient, setEditClient] = useState({
    name: '',
    email: '',
    company: '',
    status: 'active',
    notes: ''
  });
  const [originalTotalCount, setOriginalTotalCount] = useState<number>(0);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  
  // Confirmation dialog state
  const confirmDialog = useConfirmationDialog();
  const [clientToDelete, setClientToDelete] = useState<Client | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  
  const abortControllerRef = useRef<AbortController | null>(null);
  const loadingRef = useRef(false);

  const pageSizeOptions = [10, 25, 50, 100, 200, 500, 1000];

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
    storageKey: 'clients',
    defaultPageSize: 200,
    defaultSort,
  });

  // Column visibility management
  const { visibleColumns, toggleColumn, resetColumns, isColumnVisible } = useColumnVisibility({
    columns: columnDefinitions,
    storageKey: 'clients-visible-columns',
  });

  // Resizable columns management
  const defaultColumnWidths = {
    name: 300,
    email: 200,
    company: 150,
    status: 100,
    campaignCount: 120,
    emailAccountCount: 140,
    createdAt: 120,
    actions: 80,
  };

  const {
    columnWidths,
    updateColumnWidth,
    resetColumnWidths,
    handleMouseDown,
    getColumnStyle,
  } = useResizableColumns({
    storageKey: 'clients-column-widths',
    defaultWidths: defaultColumnWidths,
    minWidth: 50,
    maxWidth: 400,
  });

  // We'll keep a simple loadClients for manual refresh
  const loadClients = useCallback(() => {
    setRefreshTrigger(prev => prev + 1);
  }, []);

  const getStatusBadge = (status: Client['status']) => {
    const statusLower = status?.toLowerCase();
    
    if (statusLower === 'active') {
      return (
        <Badge className="bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-50 hover:text-emerald-700 font-medium">
          ACTIVE
        </Badge>
      );
    } else if (statusLower === 'inactive') {
      return (
        <Badge className="bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-100 hover:text-slate-600 font-medium">
          INACTIVE
        </Badge>
      );
    } else {
      return (
        <Badge variant="secondary" className="hover:bg-secondary hover:text-secondary-foreground font-medium">
          {status?.toUpperCase() || 'UNKNOWN'}
        </Badge>
      );
    }
  };

  const handleAddClient = () => {
    setShowAddModal(true);
  };

  const handleCreateClient = async () => {
    if (!newClient.name.trim()) {
      toast.error('Client name is required');
      return;
    }

    // Prepare client data with correct parameter names for backend
    const clientData = {
      Name: newClient.name.trim(),
      Company: newClient.company.trim(),
      Notes: newClient.notes.trim(),
      ...(newClient.email.trim() && { Email: newClient.email.trim() })
    };

    try {
      const response = await apiClient.post(ENDPOINTS.clients, clientData);
      
      toast.success('Client created successfully');

      setShowAddModal(false);
      setNewClient({ name: '', email: '', company: '', notes: '' });
      loadClients(); // Refresh the list
    } catch (error) {
      console.error('Failed to create client:', error);
      toast.error('Failed to create client');
    }
  };

  const handleCloseModal = () => {
    setShowAddModal(false);
    setNewClient({ name: '', email: '', company: '', notes: '' });
  };

  const handleEditClient = (client: Client) => {
    setEditingClient(client);
    setEditClient({
      name: client.name || '',
      email: client.email || '',
      company: client.company || '',
      status: client.status || 'active',
      notes: client.notes || ''
    });
    setShowEditModal(true);
  };

  const handleUpdateClient = async () => {
    if (!editingClient || !editClient.name.trim()) {
      toast.error('Client name is required');
      return;
    }

    // Prepare client data with correct parameter names for backend
    const clientData = {
      Name: editClient.name.trim(),
      Company: editClient.company.trim(),
      Status: editClient.status,
      Notes: editClient.notes.trim(),
      ...(editClient.email.trim() && { Email: editClient.email.trim() })
    };

    try {
      await apiClient.put(`${ENDPOINTS.clients}/${editingClient.id}`, clientData);
      
      toast.success('Client updated successfully');

      setShowEditModal(false);
      setEditingClient(null);
      setEditClient({ name: '', email: '', company: '', status: 'active', notes: '' });
      loadClients(); // Refresh the list
    } catch (error) {
      console.error('Failed to update client:', error);
      toast.error('Failed to update client');
    }
  };

  const handleCloseEditModal = () => {
    setShowEditModal(false);
    setEditingClient(null);
    setEditClient({ name: '', email: '', company: '', status: 'active', notes: '' });
  };

  const handleDeleteClient = (client: Client) => {
    setClientToDelete(client);
    confirmDialog.openDialog();
  };

  const handleConfirmDelete = async () => {
    if (!clientToDelete) return;

    setIsDeleting(true);
    try {
      await apiClient.delete(`${ENDPOINTS.clients}/${clientToDelete.id}`);
      toast.success('Client deleted successfully');
      loadClients(); // Refresh the list
      confirmDialog.closeDialog();
      setClientToDelete(null);
    } catch (error: any) {
      console.error('Failed to delete client:', error);
      
      // Extract error message from API response
      let errorMessage = 'Failed to delete client';
      if (error?.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error?.message) {
        errorMessage = error.message;
      }
      
      toast.error(errorMessage);
    } finally {
      setIsDeleting(false);
    }
  };

  const handleViewCampaigns = (client: Client) => {
    router.push(`/campaigns?client=${client.id}`);
  };

  const handleViewEmailAccounts = (client: Client) => {
    router.push(`/email-accounts?client=${client.id}`);
  };

  // Update select all to work with current data
  const handleSelectAllWithData = (checked: boolean) => {
    if (checked) {
      clients.forEach(client => handleSelectOne(client.id, true));
    } else {
      clients.forEach(client => handleSelectOne(client.id, false));
    }
  };

  const renderClientCell = (client: Client, columnKey: string) => {
    const style = columnKey === 'name' 
      ? { minWidth: `${columnWidths.name || defaultColumnWidths.name}px` }
      : getColumnStyle(columnKey);
    
    switch (columnKey) {
      case 'name':
        return (
          <div style={style} className="pr-4">
            <div className="flex items-center">
              <div className="mr-2 sm:mr-3 flex h-8 w-8 sm:h-10 sm:w-10 items-center justify-center rounded-lg bg-primary/10 flex-shrink-0">
                {client.company ? (
                  <Building2 className="w-4 h-4 sm:w-5 sm:h-5 text-primary" />
                ) : (
                  <Users className="w-4 h-4 sm:w-5 sm:h-5 text-primary" />
                )}
              </div>
              <div className="min-w-0 flex-1">
                <div className="font-medium text-sm sm:text-base break-words">{client.name}</div>
                <div className="text-xs text-muted-foreground truncate">{client.id}</div>
              </div>
            </div>
          </div>
        );
      case 'email':
        return (
          <div style={style}>
            {client.email ? (
              <div className="flex items-center">
                <Mail className="w-4 h-4 text-muted-foreground mr-2" />
                <span className="text-muted-foreground text-sm truncate">{client.email}</span>
              </div>
            ) : (
              <span className="text-muted-foreground text-sm">N/A</span>
            )}
          </div>
        );
      case 'company':
        return (
          <div style={style}>
            <span className="text-muted-foreground text-sm">{client.company || 'N/A'}</span>
          </div>
        );
      case 'status':
        return (
          <div style={style}>
            {getStatusBadge(client.status)}
          </div>
        );
      case 'campaignCount':
        return (
          <div style={style}>
            {(client.campaignCount || 0) > 0 ? (
              <Button
                variant="ghost"
                className="h-auto p-0 hover:bg-blue-50 hover:text-blue-700"
                onClick={() => handleViewCampaigns(client)}
              >
                <span className="font-medium text-blue-600 text-sm hover:underline cursor-pointer">
                  {client.campaignCount || 0}
                </span>
              </Button>
            ) : (
              <span className="font-medium text-muted-foreground text-sm">
                0
              </span>
            )}
          </div>
        );
      case 'emailAccountCount':
        return (
          <div style={style}>
            {(client.emailAccountCount || 0) > 0 ? (
              <Button
                variant="ghost"
                className="h-auto p-0 hover:bg-green-50 hover:text-green-700"
                onClick={() => handleViewEmailAccounts(client)}
              >
                <span className="font-medium text-green-600 text-sm hover:underline cursor-pointer">
                  {client.emailAccountCount || 0}
                </span>
              </Button>
            ) : (
              <span className="font-medium text-muted-foreground text-sm">
                0
              </span>
            )}
          </div>
        );
      case 'createdAt':
        return (
          <div style={style}>
            <span 
              className="text-xs text-muted-foreground truncate block"
              title={new Date(client.createdAt).toLocaleString()}
            >
              {formatDate(client.createdAt)}
            </span>
          </div>
        );
      case 'actions':
        return (
          <div style={style}>
            {isAdmin ? (
              <div className="flex items-center gap-0.5">
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleEditClient(client)}
                      className="h-7 w-7 p-0"
                    >
                      <Edit className="w-3 h-3" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>Edit client</p>
                  </TooltipContent>
                </Tooltip>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 w-7 p-0"
                    >
                      <MoreVertical className="w-3 h-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48">
                    <DropdownMenuItem onClick={() => handleEditClient(client)}>
                      <Edit className="w-4 h-4 mr-2" />
                      Edit Client
                    </DropdownMenuItem>
                    {client.email && (
                      <DropdownMenuItem>
                        <Mail className="w-4 h-4 mr-2" />
                        Send Email
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuItem 
                      onClick={() => handleDeleteClient(client)}
                      className="text-red-600 focus:text-red-600"
                    >
                      <Trash2 className="w-4 h-4 mr-2" />
                      Delete Client
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            ) : (
              <div className="flex items-center justify-center">
                <span className="text-xs text-muted-foreground">View Only</span>
              </div>
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
      
      // Only show loading spinner for initial load or when we have no data
      if (clients.length === 0 || isInitialLoad) {
        setLoading(true);
      }
      
      const params: Record<string, string> = {
        page: tableState.currentPage.toString(),
        limit: tableState.pageSize.toString(),
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
      
      try {
        const response = await apiClient.get<{
          clients: Client[];
          totalCount: number;
          totalPages: number;
          page: number;
          limit: number;
        }>(
          ENDPOINTS.clients,
          params,
          { signal: abortController.signal }
        );

        setClients(response.clients || []);
        updateTableData({
          totalPages: response.totalPages || 1,
          totalCount: response.totalCount || 0,
        });
        
        // Store original total count when no search is active
        if (!tableState.searchQuery.trim()) {
          setOriginalTotalCount(response.totalCount || 0);
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
        
        console.error('Initial clients load failed:', err);
        handleApiError(err, 'load clients');
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
  }, [tableState.currentPage, tableState.pageSize, tableState.searchQuery, tableState.sort, refreshTrigger]);

  return (
    <div className="flex h-full flex-col bg-background/95">
      <PageHeader 
        title="Client Management"
        description="Organize clients, track campaign assignments, and monitor engagement metrics"
        mobileDescription="Client management"
        icon={Users}
        itemCount={tableState.totalCount}
        itemLabel="clients"
        originalTotalCount={originalTotalCount}
        searchQuery={tableState.searchQuery}
        actions={isAdmin ? (
          <Button onClick={handleAddClient} size="sm" className="h-8 w-8 sm:w-auto p-0 sm:px-3">
            <Plus className="w-4 h-4 sm:mr-2" />
            <span className="hidden sm:inline">Add Client</span>
          </Button>
        ) : undefined}
      />

      <DataTableToolbar
        searchPlaceholder="Search by name, email, company..."
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
        itemLabel="clients"
        onClearSelection={clearSelection}
        sortConfig={tableState.sort}
        multiSort={tableState.multiSort}
        defaultSort={defaultSort}
        onResetSort={resetSort}
        onClearAllSorts={clearAllSorts}
      />

      <DataTable
        data={clients}
        columns={columnDefinitions}
        visibleColumns={visibleColumns}
        loading={loading}
        error={error || undefined}
        selectedItems={tableState.selectedItems}
        sortConfig={tableState.sort}
        multiSort={tableState.multiSort}
        onSelectAll={handleSelectAllWithData}
        onSelectOne={handleSelectOne}
        onSort={handleSort}
        onRetry={loadClients}
        renderCell={renderClientCell}
        emptyMessage="No clients found"
        emptyDescription="Get started by creating your first client to organize campaigns and email accounts."
        emptyAction={isAdmin ? (
          <Button onClick={handleAddClient} size="sm" className="text-xs">
            <Plus className="w-4 h-4 mr-2" />
            Add Your First Client
          </Button>
        ) : undefined}
        getId={(client) => client.id}
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

      {/* Add Client Modal */}
      <Dialog open={showAddModal} onOpenChange={setShowAddModal}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Add Client</DialogTitle>
            <DialogDescription>
              Create a new client to organize your campaigns and email accounts.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Client Name *</label>
              <Input
                value={newClient.name}
                onChange={(e) => setNewClient(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Enter client name"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Email</label>
              <Input
                type="email"
                value={newClient.email}
                onChange={(e) => setNewClient(prev => ({ ...prev, email: e.target.value }))}
                placeholder="Enter email address (optional)"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Company</label>
              <Input
                value={newClient.company}
                onChange={(e) => setNewClient(prev => ({ ...prev, company: e.target.value }))}
                placeholder="Enter company name"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Notes</label>
              <textarea
                className="w-full px-3 py-2 border border-input bg-background rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={newClient.notes}
                onChange={(e) => setNewClient(prev => ({ ...prev, notes: e.target.value }))}
                placeholder="Additional notes..."
                rows={3}
              />
            </div>
            <div className="flex space-x-3 pt-2">
              <Button variant="outline" onClick={handleCloseModal} className="flex-1">
                Cancel
              </Button>
              <Button onClick={handleCreateClient} className="flex-1">
                Add Client
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit Client Modal */}
      <Dialog open={showEditModal && editingClient !== null} onOpenChange={setShowEditModal}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Edit Client</DialogTitle>
            <DialogDescription>
              Update the client information below.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Client Name *</label>
              <Input
                value={editClient.name}
                onChange={(e) => setEditClient(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Enter client name"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Email</label>
              <Input
                type="email"
                value={editClient.email}
                onChange={(e) => setEditClient(prev => ({ ...prev, email: e.target.value }))}
                placeholder="Enter email address (optional)"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Company</label>
              <Input
                value={editClient.company}
                onChange={(e) => setEditClient(prev => ({ ...prev, company: e.target.value }))}
                placeholder="Enter company name"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Status</label>
              <select
                className="w-full px-3 py-2 border border-input bg-background rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={editClient.status}
                onChange={(e) => setEditClient(prev => ({ ...prev, status: e.target.value }))}
              >
                <option value="active">Active</option>
                <option value="inactive">Inactive</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Notes</label>
              <textarea
                className="w-full px-3 py-2 border border-input bg-background rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={editClient.notes}
                onChange={(e) => setEditClient(prev => ({ ...prev, notes: e.target.value }))}
                placeholder="Additional notes..."
                rows={3}
              />
            </div>
            <div className="flex space-x-3 pt-2">
              <Button variant="outline" onClick={handleCloseEditModal} className="flex-1">
                Cancel
              </Button>
              <Button onClick={handleUpdateClient} className="flex-1">
                Update Client
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <ConfirmationDialog
        open={confirmDialog.isOpen}
        onOpenChange={confirmDialog.setIsOpen}
        title="Delete Client"
        description={
          clientToDelete 
            ? `Are you sure you want to delete "${clientToDelete.name}"? This action cannot be undone and will remove all associated campaigns and email accounts.`
            : ''
        }
        confirmLabel="Delete Client"
        cancelLabel="Cancel"
        variant="destructive"
        onConfirm={handleConfirmDelete}
        loading={isDeleting}
      />
    </div>
  );
}