'use client';

import { useState, useEffect, useMemo, useCallback } from 'react';
import { Search, Users, Check, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Client } from '@/types';
import { apiClient, ENDPOINTS, debounce } from '@/lib/api';

interface ClientSelectionModalProps {
  isOpen: boolean;
  onClose: () => void;
  clients: Client[];
  selectedClients: Set<string>;
  onSelectionChange: (selectedClients: Set<string>) => void;
  loading?: boolean;
}

export function ClientSelectionModal({
  isOpen,
  onClose,
  clients,
  selectedClients,
  onSelectionChange,
  loading = false,
}: ClientSelectionModalProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [tempSelectedClients, setTempSelectedClients] = useState<Set<string>>(new Set());
  const [searchResults, setSearchResults] = useState<Client[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [allDisplayClients, setAllDisplayClients] = useState<Client[]>([]);

  // Initialize temp selection when modal opens
  useEffect(() => {
    if (isOpen) {
      setTempSelectedClients(new Set(selectedClients));
      setSearchQuery('');
      setSearchResults([]);
      setAllDisplayClients(clients); // Start with selected clients
    }
  }, [isOpen]); // Remove clients from dependencies to prevent infinite loop
  
  // Update display clients when clients prop changes
  useEffect(() => {
    if (isOpen && !searchQuery) {
      setAllDisplayClients(clients);
    }
  }, [clients, isOpen, searchQuery]);

  const searchClients = useCallback(async (query: string) => {
    if (!query.trim()) {
      setSearchResults([]);
      setAllDisplayClients(clients); // Show selected clients when no search
      return;
    }

    setSearchLoading(true);
    try {
      const response = await apiClient.get<{ data: Client[] }>(ENDPOINTS.clientSearch, {
        q: query,
        limit: '50', // Limit search results
      });
      
      setSearchResults(response.data);
      
      // Combine selected clients with search results, avoiding duplicates
      const combinedClients = new Map<string, Client>();
      
      clients.forEach(client => {
        combinedClients.set(client.id, client);
      });
      
      response.data.forEach(client => {
        combinedClients.set(client.id, client);
      });
      
      setAllDisplayClients(Array.from(combinedClients.values()));
    } catch (error) {
      console.error('Failed to search clients:', error);
    } finally {
      setSearchLoading(false);
    }
  }, [clients]);

  const debouncedSearch = useCallback(
    debounce((query: string) => searchClients(query), 300),
    [searchClients]
  );

  useEffect(() => {
    debouncedSearch(searchQuery);
  }, [searchQuery, debouncedSearch]);

  // Filter displayed clients based on search query for local filtering
  const filteredClients = useMemo(() => {
    return allDisplayClients;
  }, [allDisplayClients]);

  const handleClientToggle = (clientId: string) => {
    const newSelected = new Set(tempSelectedClients);
    if (newSelected.has(clientId)) {
      newSelected.delete(clientId);
    } else {
      newSelected.add(clientId);
    }
    setTempSelectedClients(newSelected);
  };

  const handleSelectAll = () => {
    const allFilteredIds = new Set(filteredClients.map(client => client.id));
    const newSelected = new Set(tempSelectedClients);
    
    // Add all filtered clients to selection
    filteredClients.forEach(client => newSelected.add(client.id));
    setTempSelectedClients(newSelected);
  };

  const handleClearAll = () => {
    const filteredIds = new Set(filteredClients.map(client => client.id));
    const newSelected = new Set(tempSelectedClients);
    
    // Remove all filtered clients from selection
    filteredIds.forEach(id => newSelected.delete(id));
    setTempSelectedClients(newSelected);
  };

  const handleSelectAllClients = () => {
    setTempSelectedClients(new Set(allDisplayClients.map(client => client.id)));
  };

  const handleClearAllClients = () => {
    setTempSelectedClients(new Set());
  };

  const handleApply = () => {
    onSelectionChange(tempSelectedClients);
    onClose();
  };

  const handleCancel = () => {
    setTempSelectedClients(new Set(selectedClients));
    onClose();
  };

  const allFilteredSelected = filteredClients.length > 0 && 
    filteredClients.every(client => tempSelectedClients.has(client.id));
  const someFilteredSelected = filteredClients.some(client => tempSelectedClients.has(client.id));

  return (
    <Dialog open={isOpen} onOpenChange={handleCancel}>
      <DialogContent className="max-w-2xl h-[80vh] flex flex-col p-4">
        <DialogHeader className="flex-shrink-0 pb-2">
          <DialogTitle className="flex items-center gap-2 text-sm">
            <Users className="w-4 h-4" />
            Select Clients
            <Badge variant="secondary" className="ml-2 text-xs">
              {tempSelectedClients.size} selected
            </Badge>
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 flex flex-col min-h-0">
          {/* Search */}
          <div className="flex-shrink-0 space-y-3 pb-3">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-3 h-3 text-muted-foreground" />
              <Input
                placeholder="Search clients by name, email, or company..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 text-xs"
              />
              {searchLoading && (
                <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                  <div className="animate-spin w-3 h-3 border border-primary border-t-transparent rounded-full"></div>
                </div>
              )}
            </div>

            {/* Bulk Actions */}
            <div className="flex items-center justify-between">
              <div className="flex gap-1">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleSelectAll}
                  disabled={loading || filteredClients.length === 0 || allFilteredSelected}
                  className="text-xs h-7 px-2"
                >
                  <Check className="w-3 h-3 mr-1" />
                  Select Filtered ({filteredClients.length})
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleClearAll}
                  disabled={loading || filteredClients.length === 0 || !someFilteredSelected}
                  className="text-xs h-7 px-2"
                >
                  <X className="w-3 h-3 mr-1" />
                  Clear Filtered
                </Button>
              </div>
              <div className="flex gap-1">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleSelectAllClients}
                  disabled={loading || tempSelectedClients.size === allDisplayClients.length}
                  className="text-xs h-7 px-2"
                >
                  Select All ({allDisplayClients.length})
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleClearAllClients}
                  disabled={loading || tempSelectedClients.size === 0}
                  className="text-xs h-7 px-2"
                >
                  Clear All
                </Button>
              </div>
            </div>
          </div>

          {/* Client List */}
          <div className="flex-1 overflow-y-auto">
            {loading ? (
              <div className="flex items-center justify-center py-8">
                <div className="animate-spin w-4 h-4 border-2 border-primary border-t-transparent rounded-full"></div>
                <span className="ml-2 text-xs text-muted-foreground">Loading clients...</span>
              </div>
            ) : filteredClients.length === 0 ? (
              <div className="flex items-center justify-center py-8">
                <div className="text-center">
                  <Users className="w-6 h-6 text-muted-foreground mx-auto mb-2" />
                  <p className="text-xs text-muted-foreground">
                    {searchQuery ? 'No clients found matching your search' : 'No clients available'}
                  </p>
                </div>
              </div>
            ) : (
              <div className="space-y-1 pr-2">
                {filteredClients.map((client) => {
                  const isSelected = tempSelectedClients.has(client.id);
                  
                  return (
                    <div
                      key={client.id}
                      className="flex items-center space-x-3 p-2 rounded-lg hover:bg-accent/50 cursor-pointer transition-colors"
                      onClick={() => handleClientToggle(client.id)}
                    >
                      <Checkbox
                        checked={isSelected}
                        className="pointer-events-none"
                      />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-medium">{client.name}</span>
                          <Badge 
                            variant={client.status === 'Active' ? 'default' : 'secondary'}
                            className="text-xs"
                          >
                            {client.status}
                          </Badge>
                        </div>
                        {(client.email || client.company) && (
                          <div className="text-xs text-muted-foreground mt-1">
                            {client.email && <span>{client.email}</span>}
                            {client.email && client.company && <span> • </span>}
                            {client.company && <span>{client.company}</span>}
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        <DialogFooter className="flex-shrink-0 pt-3">
          <Button variant="outline" onClick={handleCancel} className="text-xs h-8 px-3">
            Cancel
          </Button>
          <Button onClick={handleApply} className="text-xs h-8 px-3">
            Apply Selection ({tempSelectedClients.size})
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}