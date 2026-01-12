'use client';

import { useState, useEffect, useMemo, useCallback, useRef } from 'react';
import { Search, BarChart3, Check, X } from 'lucide-react';
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
import { Campaign, Client } from '@/types';
import { apiClient, ENDPOINTS, debounce } from '@/lib/api';

interface CampaignSelectionModalProps {
  isOpen: boolean;
  onClose: () => void;
  campaigns: Campaign[]; // These are the selected campaigns for display
  clients: Client[];
  selectedCampaigns: Set<string>;
  onSelectionChange: (selectedCampaigns: Set<string>) => void;
  loading?: boolean;
}

export function CampaignSelectionModal({
  isOpen,
  onClose,
  campaigns,
  clients,
  selectedCampaigns,
  onSelectionChange,
  loading = false,
}: CampaignSelectionModalProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [tempSelectedCampaigns, setTempSelectedCampaigns] = useState<Set<string>>(new Set());
  const [searchResults, setSearchResults] = useState<Campaign[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [allDisplayCampaigns, setAllDisplayCampaigns] = useState<Campaign[]>([]);
  
  // Use refs to access current values without causing re-renders
  const campaignsRef = useRef(campaigns);
  const clientsRef = useRef(clients);
  
  // Update refs when props change
  useEffect(() => {
    campaignsRef.current = campaigns;
  }, [campaigns]);
  
  useEffect(() => {
    clientsRef.current = clients;
  }, [clients]);

  // Create client lookup map
  const clientMap = useMemo(() => {
    return new Map(clients.map(client => [client.id, client]));
  }, [clients]);

  // Initialize temp selection when modal opens
  useEffect(() => {
    if (isOpen) {
      setTempSelectedCampaigns(new Set(selectedCampaigns));
      setSearchQuery('');
      setSearchResults([]);
      // Show selected campaigns initially
      setAllDisplayCampaigns(campaigns);
    }
  }, [isOpen]); // Remove campaigns from dependencies to prevent infinite loop
  
  // Update display campaigns when campaigns prop changes
  useEffect(() => {
    if (isOpen && !searchQuery) {
      setAllDisplayCampaigns(campaigns);
    }
  }, [campaigns, isOpen, searchQuery]);

  // Search campaigns from API with debouncing
  const searchCampaigns = useCallback(async (query: string) => {
    if (!query.trim()) {
      setSearchResults([]);
      setAllDisplayCampaigns(campaignsRef.current); // Show selected campaigns when no search
      return;
    }

    setSearchLoading(true);
    try {
      // Get client IDs for filtering
      const clientIds = Array.from(new Set(clientsRef.current.map(c => c.id)));
      
      const response = await apiClient.get<{ data: Campaign[] }>(ENDPOINTS.campaignSearch, {
        q: query,
        clientIds: clientIds.join(','),
        limit: '50', // Limit search results
      });
      
      setSearchResults(response.data);
      
      // Combine selected campaigns with search results, avoiding duplicates
      const combinedCampaigns = new Map<string, Campaign>();
      
      // Add selected campaigns first
      campaignsRef.current.forEach(campaign => {
        combinedCampaigns.set(campaign.id, campaign);
      });
      
      // Add search results
      response.data.forEach(campaign => {
        combinedCampaigns.set(campaign.id, campaign);
      });
      
      setAllDisplayCampaigns(Array.from(combinedCampaigns.values()));
    } catch (error) {
      console.error('Failed to search campaigns:', error);
    } finally {
      setSearchLoading(false);
    }
  }, []); // No dependencies to prevent recreation

  const debouncedSearch = useMemo(
    () => debounce(searchCampaigns, 300),
    [searchCampaigns]
  );

  // Handle search query changes
  useEffect(() => {
    debouncedSearch(searchQuery);
  }, [searchQuery, debouncedSearch]);

  // Filter displayed campaigns based on search
  const filteredCampaigns = useMemo(() => {
    return allDisplayCampaigns;
  }, [allDisplayCampaigns]);

  const handleCampaignToggle = (campaignId: string) => {
    const newSelected = new Set(tempSelectedCampaigns);
    if (newSelected.has(campaignId)) {
      newSelected.delete(campaignId);
    } else {
      newSelected.add(campaignId);
    }
    setTempSelectedCampaigns(newSelected);
  };

  const handleSelectAll = () => {
    const newSelected = new Set(tempSelectedCampaigns);
    
    // Add all filtered campaigns to selection
    filteredCampaigns.forEach(campaign => newSelected.add(campaign.id));
    setTempSelectedCampaigns(newSelected);
  };

  const handleClearAll = () => {
    const filteredIds = new Set(filteredCampaigns.map(campaign => campaign.id));
    const newSelected = new Set(tempSelectedCampaigns);
    
    // Remove all filtered campaigns from selection
    filteredIds.forEach(id => newSelected.delete(id));
    setTempSelectedCampaigns(newSelected);
  };

  const handleSelectAllCampaigns = () => {
    setTempSelectedCampaigns(new Set(campaigns.map(campaign => campaign.id)));
  };

  const handleClearAllCampaigns = () => {
    setTempSelectedCampaigns(new Set());
  };

  const handleApply = () => {
    onSelectionChange(tempSelectedCampaigns);
    onClose();
  };

  const handleCancel = () => {
    setTempSelectedCampaigns(new Set(selectedCampaigns));
    onClose();
  };

  const allFilteredSelected = filteredCampaigns.length > 0 && 
    filteredCampaigns.every(campaign => tempSelectedCampaigns.has(campaign.id));
  const someFilteredSelected = filteredCampaigns.some(campaign => tempSelectedCampaigns.has(campaign.id));

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'bg-green-100 text-green-800';
      case 'paused': return 'bg-yellow-100 text-yellow-800';
      case 'completed': return 'bg-blue-100 text-blue-800';
      case 'draft': return 'bg-gray-100 text-gray-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleCancel}>
      <DialogContent className="max-w-3xl h-[80vh] flex flex-col p-4">
        <DialogHeader className="flex-shrink-0 pb-2">
          <DialogTitle className="flex items-center gap-2 text-sm">
            <BarChart3 className="w-4 h-4" />
            Select Campaigns
            <Badge variant="secondary" className="ml-2 text-xs">
              {tempSelectedCampaigns.size} selected
            </Badge>
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 flex flex-col min-h-0">
          {/* Search */}
          <div className="flex-shrink-0 space-y-3 pb-3">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-3 h-3 text-muted-foreground" />
              <Input
                placeholder="Search campaigns by name, client, or status..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 text-xs"
              />
            </div>

            {/* Bulk Actions */}
            <div className="flex items-center justify-between">
              <div className="flex gap-1">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleSelectAll}
                  disabled={loading || filteredCampaigns.length === 0 || allFilteredSelected}
                  className="text-xs h-7 px-2"
                >
                  <Check className="w-3 h-3 mr-1" />
                  Select Filtered ({filteredCampaigns.length})
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleClearAll}
                  disabled={loading || filteredCampaigns.length === 0 || !someFilteredSelected}
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
                  onClick={handleSelectAllCampaigns}
                  disabled={loading || tempSelectedCampaigns.size === campaigns.length}
                  className="text-xs h-7 px-2"
                >
                  Select All ({campaigns.length})
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleClearAllCampaigns}
                  disabled={loading || tempSelectedCampaigns.size === 0}
                  className="text-xs h-7 px-2"
                >
                  Clear All
                </Button>
              </div>
            </div>
          </div>

          {/* Campaign List */}
          <div className="flex-1 overflow-y-auto">
            {(loading || searchLoading) ? (
              <div className="flex items-center justify-center py-8">
                <div className="animate-spin w-4 h-4 border-2 border-primary border-t-transparent rounded-full"></div>
                <span className="ml-2 text-xs text-muted-foreground">
                  {searchLoading ? 'Searching campaigns...' : 'Loading campaigns...'}
                </span>
              </div>
            ) : filteredCampaigns.length === 0 ? (
              <div className="flex items-center justify-center py-8">
                <div className="text-center">
                  <BarChart3 className="w-6 h-6 text-muted-foreground mx-auto mb-2" />
                  <p className="text-xs text-muted-foreground">
                    {searchQuery ? 'No campaigns found matching your search' : 'No campaigns selected. Use search to find and select campaigns.'}
                  </p>
                </div>
              </div>
            ) : (
              <div className="space-y-1 pr-2">
                {filteredCampaigns.map((campaign) => {
                  const isSelected = tempSelectedCampaigns.has(campaign.id);
                  const client = campaign.clientId ? clientMap.get(campaign.clientId) : null;
                  
                  return (
                    <div
                      key={campaign.id}
                      className="flex items-center space-x-3 p-2 rounded-lg hover:bg-accent/50 cursor-pointer transition-colors"
                      onClick={() => handleCampaignToggle(campaign.id)}
                    >
                      <Checkbox
                        checked={isSelected}
                        className="pointer-events-none"
                      />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-xs font-medium">{campaign.name}</span>
                          <Badge 
                            className={`text-xs ${getStatusColor(campaign.status)}`}
                          >
                            {campaign.status}
                          </Badge>
                        </div>
                        <div className="flex items-center gap-4 text-xs text-muted-foreground">
                          {client && (
                            <span>Client: {client.name}</span>
                          )}
                          <span>Leads: {campaign.totalLeads.toLocaleString()}</span>
                          <span>Sent: {campaign.totalSent.toLocaleString()}</span>
                          {campaign.totalSent > 0 && (
                            <>
                              <span>Opened: {campaign.totalOpened.toLocaleString()} ({((campaign.totalOpened / campaign.totalSent) * 100).toFixed(1)}%)</span>
                              <span>Replied: {campaign.totalReplied.toLocaleString()} ({((campaign.totalReplied / campaign.totalSent) * 100).toFixed(1)}%)</span>
                            </>
                          )}
                        </div>
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
            Apply Selection ({tempSelectedCampaigns.size})
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}