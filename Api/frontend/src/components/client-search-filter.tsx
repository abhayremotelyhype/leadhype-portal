'use client';

import { useState, useEffect } from 'react';
import { Check, ChevronsUpDown, Building2, AlertCircle, Users } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { ClientListItem } from '@/types';
import { cn } from '@/lib/utils';

interface ClientSearchFilterProps {
  selectedClientId: string | null;
  onSelectionChange: (clientId: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  variant?: 'default' | 'compact';
  filterByUserId?: string | null; // User ID to filter clients by their assignments
}

export function ClientSearchFilter({
  selectedClientId,
  onSelectionChange,
  placeholder = "All clients",
  disabled = false,
  className,
  variant = 'default',
  filterByUserId = null,
}: ClientSearchFilterProps) {
  const [open, setOpen] = useState(false);
  const [clients, setClients] = useState<ClientListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isCompact = variant === 'compact';

  const loadClients = async () => {
    setLoading(true);
    setError(null);
    try {
      const params: any = {};
      if (filterByUserId) {
        params.filterByUserId = filterByUserId;
      }
      
      const response = await apiClient.get<{ data: ClientListItem[] }>(ENDPOINTS.clientList, params);
      // Handle both paginated response format and direct array format
      const clientsData = response.data || response;
      setClients(Array.isArray(clientsData) ? clientsData : []);
    } catch (error) {
      console.error('Failed to load clients:', error);
      setError('Failed to load clients');
      setClients([]); // Reset to empty array on error
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (open && clients.length === 0) {
      loadClients();
    }
  }, [open, clients.length]);

  // Load clients when there is a selected client ID but no clients loaded yet
  useEffect(() => {
    if (selectedClientId && clients.length === 0 && !loading) {
      loadClients();
    }
  }, [selectedClientId, clients.length, loading]);

  // Reload clients when filter changes
  useEffect(() => {
    if (clients.length > 0) {
      loadClients();
    }
  }, [filterByUserId]);

  const handleClientSelect = (clientId: string) => {
    if (clientId === 'all') {
      // If "All clients" is selected, clear selection
      onSelectionChange(null);
      setOpen(false);
      return;
    }

    // Single selection - either select this one or clear if already selected
    const newSelection = selectedClientId === clientId ? null : clientId;
    onSelectionChange(newSelection);
    setOpen(false);
  };

  const getDisplayText = () => {
    if (!selectedClientId) {
      return placeholder;
    }
    
    const client = clients.find(c => c.id === selectedClientId);
    return client?.name || placeholder;
  };

  const selectedClient = selectedClientId
    ? clients.find(client => client.id === selectedClientId)
    : null;

  return (
    <div className={cn("flex items-center space-x-2", className)}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn(
              "justify-between font-normal relative",
              isCompact ? "h-9 px-3" : "h-10 px-4",
              selectedClientId ? "bg-green-50 border-green-200 text-green-700" : ""
            )}
            disabled={disabled}
          >
            <div className="flex items-center space-x-2 min-w-0">
              <Building2 className="h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <span className={cn(
                "truncate",
                selectedClientId ? "text-green-700 font-medium" : "text-muted-foreground"
              )}>
                {getDisplayText()}
              </span>
            </div>
            <div className="flex items-center space-x-1 ml-2 flex-shrink-0">
              <ChevronsUpDown className="h-4 w-4 text-muted-foreground" />
            </div>
            {selectedClientId && (
              <div className="absolute -top-1 -right-1 w-2 h-2 bg-green-500 rounded-full" />
            )}
          </Button>
        </PopoverTrigger>
        
        <PopoverContent className="w-80 p-0" align="start">
          <div className="flex items-center justify-between p-3 border-b">
            <div className="flex items-center space-x-2">
              <Building2 className="h-4 w-4 text-green-500" />
              <span className="font-medium text-sm">Filter by Client</span>
            </div>
          </div>

          <div className="p-2">
            <div className="text-xs text-muted-foreground mb-3 px-1">
              Filter to view items associated with specific clients or unassigned items.
            </div>
          </div>

          <div className="max-h-72 overflow-y-auto">
            {loading ? (
              <div className="flex items-center justify-center py-6">
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                <span className="ml-2 text-sm text-muted-foreground">Loading clients...</span>
              </div>
            ) : error ? (
              <Alert variant="destructive" className="m-2">
                <AlertCircle className="h-4 w-4" />
                <AlertTitle>Error Loading Clients</AlertTitle>
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            ) : (
              <>
                {/* All Clients Option */}
                <div
                  className={cn(
                    "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors",
                    !selectedClientId ? "bg-accent" : ""
                  )}
                  onClick={() => handleClientSelect('all')}
                >
                  <Checkbox
                    checked={!selectedClientId}
                    onChange={() => {}} // Handled by the div click
                    className="flex-shrink-0"
                  />
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium">
                      All clients
                    </div>
                    <div className="text-xs text-muted-foreground">
                      Show items from all clients and unassigned
                    </div>
                  </div>
                  {!selectedClientId && (
                    <Check className="h-4 w-4 text-primary flex-shrink-0" />
                  )}
                </div>
                
                {clients.map((client) => {
                  const isSelected = selectedClientId === client.id;
                  
                  return (
                    <div
                      key={client.id}
                      className={cn(
                        "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors",
                        isSelected ? "bg-accent" : ""
                      )}
                      onClick={() => handleClientSelect(client.id)}
                    >
                      <Checkbox
                        checked={isSelected}
                        onChange={() => {}} // Handled by the div click
                        className="flex-shrink-0"
                      />
                      <div className="flex-1 min-w-0">
                        <div className="text-sm font-medium text-green-600">
                          {client.name}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          Client account
                        </div>
                      </div>
                      {isSelected && (
                        <Check className="h-4 w-4 text-primary flex-shrink-0" />
                      )}
                    </div>
                  );
                })}
              </>
            )}
          </div>

        </PopoverContent>
      </Popover>
    </div>
  );
}