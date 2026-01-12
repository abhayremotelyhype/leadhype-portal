'use client';

import { useState, useEffect } from 'react';
import { Check, ChevronsUpDown, Users, X, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { ClientListItem } from '@/types';
import { cn } from '@/lib/utils';

interface ClientSelectionDropdownProps {
  selectedClientIds: string[];
  onSelectionChange: (clientIds: string[]) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}

export function ClientSelectionDropdown({
  selectedClientIds,
  onSelectionChange,
  placeholder = "Select clients",
  disabled = false,
  className,
}: ClientSelectionDropdownProps) {
  const [open, setOpen] = useState(false);
  const [clients, setClients] = useState<ClientListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadClients = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiClient.get<ClientListItem[]>(ENDPOINTS.clientList);
      setClients(response);
    } catch (error) {
      console.error('Failed to load clients:', error);
      setError('Failed to load clients');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (open && clients.length === 0) {
      loadClients();
    }
  }, [open, clients.length]);

  const isSelected = (clientId: string) => selectedClientIds.includes(clientId);
  const allSelected = clients.length > 0 && selectedClientIds.length === clients.length;
  const someSelected = selectedClientIds.length > 0 && selectedClientIds.length < clients.length;

  const handleSelectClient = (clientId: string) => {
    if (isSelected(clientId)) {
      onSelectionChange(selectedClientIds.filter(id => id !== clientId));
    } else {
      onSelectionChange([...selectedClientIds, clientId]);
    }
  };

  const handleSelectAll = () => {
    if (allSelected) {
      onSelectionChange([]);
    } else {
      onSelectionChange(clients.map(client => client.id));
    }
  };

  const handleClearSelection = () => {
    onSelectionChange([]);
  };

  const getSelectedClientNames = () => {
    return clients
      .filter(client => selectedClientIds.includes(client.id))
      .map(client => client.name);
  };

  const renderSelectedText = () => {
    if (selectedClientIds.length === 0) {
      return <span className="text-muted-foreground">{placeholder}</span>;
    }
    
    if (selectedClientIds.length === clients.length && clients.length > 0) {
      return <span>All clients ({clients.length})</span>;
    }
    
    if (selectedClientIds.length === 1) {
      const selectedNames = getSelectedClientNames();
      return <span>{selectedNames[0]}</span>;
    }
    
    return <span>{selectedClientIds.length} clients selected</span>;
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          size={className?.includes('h-7') ? 'sm' : 'default'}
          className={cn(
            "w-full justify-between text-left font-normal",
            selectedClientIds.length === 0 && "text-muted-foreground",
            className?.includes('h-7') && "h-7 px-2",
            className
          )}
          disabled={disabled}
        >
          <div className="flex items-center gap-2 flex-1 min-w-0">
            <Users className={cn(
              "flex-shrink-0",
              className?.includes('h-7') ? "h-3 w-3" : "h-4 w-4"
            )} />
            <span className={cn(
              "truncate",
              className?.includes('h-7') ? "text-xs" : ""
            )}>{renderSelectedText()}</span>
          </div>
          <ChevronsUpDown className={cn(
            "ml-2 shrink-0 opacity-50",
            className?.includes('h-7') ? "h-3 w-3" : "h-4 w-4"
          )} />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0" align="start">
        <div className="p-3">
          <div className="flex items-center justify-between mb-3">
            <span className="text-sm font-medium">Select Clients</span>
            {selectedClientIds.length > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleClearSelection}
                className="h-6 px-2 text-xs"
              >
                <X className="h-3 w-3 mr-1" />
                Clear
              </Button>
            )}
          </div>
          
          {loading ? (
            <div className="flex items-center justify-center py-6">
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
              <span className="ml-2 text-sm text-muted-foreground">Loading clients...</span>
            </div>
          ) : error ? (
            <Alert variant="destructive" className="m-2">
              <AlertCircle />
              <AlertTitle>Error Loading Clients</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : clients.length === 0 ? (
            <Alert className="m-2">
              <Users />
              <AlertTitle>No Clients Found</AlertTitle>
              <AlertDescription>No clients are available</AlertDescription>
            </Alert>
          ) : (
            <>
              {/* Select All Option */}
              <div className="flex items-center space-x-2 p-2 rounded-md hover:bg-accent cursor-pointer" onClick={handleSelectAll}>
                <Checkbox
                  checked={allSelected}
                  ref={(el) => {
                    if (el) {
                      const checkboxElement = el.querySelector('input[type="checkbox"]') as HTMLInputElement;
                      if (checkboxElement) {
                        checkboxElement.indeterminate = someSelected;
                      }
                    }
                  }}
                  onChange={handleSelectAll}
                />
                <span className="text-sm font-medium">
                  {allSelected ? 'Deselect All' : 'Select All'} ({clients.length})
                </span>
              </div>
              
              <Separator className="my-2" />
              
              {/* Client List */}
              <div className="max-h-48 overflow-y-auto space-y-1">
                {clients.map((client) => (
                  <div
                    key={client.id}
                    className="flex items-center space-x-2 p-2 rounded-md hover:bg-accent cursor-pointer"
                    onClick={() => handleSelectClient(client.id)}
                  >
                    <Checkbox
                      checked={isSelected(client.id)}
                      onChange={() => handleSelectClient(client.id)}
                    />
                    <span className="text-sm flex-1">{client.name}</span>
                    {isSelected(client.id) && (
                      <Check className="h-4 w-4 text-primary" />
                    )}
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
        
        {selectedClientIds.length > 0 && (
          <>
            <Separator />
            <div className="p-3 bg-muted/30">
              <div className="flex items-center justify-between text-xs">
                <span className="text-muted-foreground">Selected:</span>
                <span className="font-medium">{selectedClientIds.length} of {clients.length}</span>
              </div>
            </div>
          </>
        )}
      </PopoverContent>
    </Popover>
  );
}