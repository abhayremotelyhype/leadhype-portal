'use client';

import React, { useState, useEffect } from 'react';
import { X, Mail, User, MessageCircle, Clock, Loader2, ChevronLeft, ChevronRight, Reply, Filter, Check } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';

interface EmailHistory {
  subject: string;
  body: string;
  sequenceNumber: number;
  type?: string;
  time?: string;
  createdAt: string;
  // RevReply classification fields
  classificationResult?: string;
  classifiedAt?: string;
  isClassified?: boolean;
}

interface LeadData {
  id: string;
  status: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  messageCount: number;
  customFields: Array<{ key: string; value: string }>;
  emailHistory: EmailHistory[];
}

interface LeadHistoryData {
  campaign: {
    id: string;
    campaignId: number;
    name: string;
  };
  leads: LeadData[];
  totalLeads: number;
  totalMessages: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

interface LeadHistoryModalProps {
  isOpen: boolean;
  onClose: () => void;
  campaignId: string;
  campaignName?: string;
}

export function LeadHistoryModal({ isOpen, onClose, campaignId, campaignName }: LeadHistoryModalProps) {
  const [loading, setLoading] = useState(false);
  const [paginationLoading, setPaginationLoading] = useState(false);
  const [filterLoading, setFilterLoading] = useState(false);
  const [data, setData] = useState<LeadHistoryData | null>(null);
  const [selectedLead, setSelectedLead] = useState<string | null>(null);
  const [activeView, setActiveView] = useState<'conversation' | 'details'>('conversation');
  const [currentPage, setCurrentPage] = useState(1);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [fetchedLeads, setFetchedLeads] = useState<Set<string>>(new Set());
  const [withRepliesOnly, setWithRepliesOnly] = useState(false);
  const { toast } = useToast();

  useEffect(() => {
    if (isOpen && campaignId) {
      // Reset state when modal opens
      setSelectedLead(null);
      setFetchedLeads(new Set());
      setCurrentPage(1);
      fetchLeadHistory(1);
    }
  }, [isOpen, campaignId]);

  // Separate effect for filter changes
  useEffect(() => {
    if (isOpen && campaignId && data) {
      // When filter changes, reload with filter loading state
      setCurrentPage(1);
      fetchLeadHistoryWithFilter(1);
    }
  }, [withRepliesOnly]);

  const fetchLeadHistory = async (page: number = currentPage) => {
    if (page === 1) {
      setLoading(true);
    } else {
      setPaginationLoading(true);
    }
    try {
      const response = await apiClient.get<{ success: boolean; data: LeadHistoryData }>(
        `${ENDPOINTS.v1.campaigns}/${campaignId}/lead-history?page=${page}&pageSize=20&withRepliesOnly=${withRepliesOnly}`
      );
      
      if (response.success && response.data) {
        setData(response.data);
        setCurrentPage(response.data.currentPage);
        // Auto-select first lead if available
        if (response.data.leads.length > 0) {
          const firstLeadId = response.data.leads[0]?.id;
          if (firstLeadId) {
            setSelectedLead(firstLeadId);
            // Fetch email history for the first lead using the response data directly
            fetchEmailHistoryForLeadWithData(firstLeadId, response.data);
          }
        }
      }
    } catch (error) {
      console.error('Failed to fetch lead history:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load lead history',
      });
    } finally {
      setLoading(false);
      setPaginationLoading(false);
    }
  };

  const fetchLeadHistoryWithFilter = async (page: number = currentPage) => {
    setFilterLoading(true);
    try {
      const response = await apiClient.get<{ success: boolean; data: LeadHistoryData }>(
        `${ENDPOINTS.v1.campaigns}/${campaignId}/lead-history?page=${page}&pageSize=20&withRepliesOnly=${withRepliesOnly}`
      );

      if (response.success && response.data) {
        setData(response.data);
        setCurrentPage(response.data.currentPage);
        // Auto-select first lead if available
        if (response.data.leads.length > 0) {
          const firstLeadId = response.data.leads[0]?.id;
          if (firstLeadId) {
            setSelectedLead(firstLeadId);
            // Fetch email history for the first lead using the response data directly
            fetchEmailHistoryForLeadWithData(firstLeadId, response.data);
          }
        }
      }
    } catch (error) {
      console.error('Failed to fetch lead history:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load lead history',
      });
    } finally {
      setFilterLoading(false);
    }
  };

  const fetchEmailHistoryForLead = async (leadId: string) => {
    if (!data) return;
    
    setLoadingHistory(true);
    try {
      const response = await apiClient.get<{ success: boolean; data: { leadId: string; emailHistory: EmailHistory[] } }>(
        `${ENDPOINTS.v1.campaigns}/${campaignId}/leads/${leadId}/history`
      );
      
      if (response.success && response.data) {
        // Update the specific lead's email history
        setData(prevData => {
          if (!prevData) return null;
          
          const updatedLeads = prevData.leads.map(lead => {
            if (lead.id === leadId) {
              return {
                ...lead,
                emailHistory: response.data.emailHistory
              };
            }
            return lead;
          });
          
          return {
            ...prevData,
            leads: updatedLeads
          };
        });
        
        // Mark this lead as fetched
        setFetchedLeads(prev => new Set(prev).add(leadId));
      }
    } catch (error) {
      console.error('Failed to fetch email history for lead:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load email history for this lead',
      });
    } finally {
      setLoadingHistory(false);
    }
  };

  const fetchEmailHistoryForLeadWithData = async (leadId: string, currentData: LeadHistoryData) => {
    setLoadingHistory(true);
    try {
      const response = await apiClient.get<{ success: boolean; data: { leadId: string; emailHistory: EmailHistory[] } }>(
        `${ENDPOINTS.v1.campaigns}/${campaignId}/leads/${leadId}/history`
      );
      
      if (response.success && response.data) {
        // Update the specific lead's email history using the provided data
        const updatedLeads = currentData.leads.map(lead => {
          if (lead.id === leadId) {
            return {
              ...lead,
              emailHistory: response.data.emailHistory
            };
          }
          return lead;
        });
        
        const updatedData = {
          ...currentData,
          leads: updatedLeads
        };
        
        setData(updatedData);
        
        // Mark this lead as fetched
        setFetchedLeads(prev => new Set(prev).add(leadId));
      }
    } catch (error) {
      console.error('Failed to fetch email history for lead:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load email history for this lead',
      });
    } finally {
      setLoadingHistory(false);
    }
  };

  const selectedLeadData = data?.leads.find(lead => lead.id && lead.id === selectedLead);

  const getLeadDisplayName = (lead: LeadData) => {
    // First check if we have direct name fields from the API
    if (lead.firstName || lead.lastName) {
      return `${lead.firstName || ''} ${lead.lastName || ''}`.trim();
    }
    
    // If we have email, extract name from it
    if (lead.email && lead.email.includes('@')) {
      const emailName = lead.email.split('@')[0];
      if (emailName.includes('.')) {
        const nameParts = emailName.split('.').map(part => 
          part.charAt(0).toUpperCase() + part.slice(1).toLowerCase()
        );
        return nameParts.join(' ');
      } else if (emailName.includes('_')) {
        const nameParts = emailName.split('_').map(part => 
          part.charAt(0).toUpperCase() + part.slice(1).toLowerCase()
        );
        return nameParts.join(' ');
      } else {
        return emailName.charAt(0).toUpperCase() + emailName.slice(1).toLowerCase();
      }
    }
    
    // Fallback to searching custom fields (existing logic as backup)
    let firstName = '';
    let lastName = '';
    let emailAddress = '';
    let fullName = '';
    
    lead.customFields?.forEach(field => {
      const key = field.key.toLowerCase();
      let value = field.value;
      
      if (!value || value === '--' || value === '') return;
      
      try {
        if (value.startsWith('{') && value.endsWith('}')) {
          const parsed = JSON.parse(value);
          Object.entries(parsed).forEach(([jsonKey, jsonValue]) => {
            const jsonKeyLower = jsonKey.toLowerCase();
            const stringValue = String(jsonValue).trim();
            
            if (stringValue && stringValue !== '--') {
              if (jsonKeyLower.includes('first') || jsonKeyLower === 'fname' || jsonKeyLower === 'firstname') {
                firstName = stringValue;
              } else if (jsonKeyLower.includes('last') || jsonKeyLower === 'lname' || jsonKeyLower === 'lastname') {
                lastName = stringValue;
              } else if (jsonKeyLower.includes('email') || jsonKeyLower === 'email_address') {
                emailAddress = stringValue;
              } else if ((jsonKeyLower === 'name' || jsonKeyLower === 'full_name' || jsonKeyLower === 'fullname') && !firstName && !lastName) {
                fullName = stringValue;
                const nameParts = stringValue.split(' ');
                firstName = nameParts[0] || '';
                lastName = nameParts.slice(1).join(' ') || '';
              }
            }
          });
        }
      } catch (e) {
        // Not JSON, continue with direct field matching
      }
      
      if (key.includes('first') || key.includes('fname') || key === 'firstname') {
        firstName = value;
      } else if (key.includes('last') || key.includes('lname') || key === 'lastname') {
        lastName = value;
      } else if (key.includes('email') || key === 'email_address') {
        emailAddress = value;
      } else if ((key === 'name' || key === 'full_name' || key === 'fullname') && !firstName && !lastName) {
        fullName = value;
        const nameParts = value.split(' ');
        firstName = nameParts[0] || '';
        lastName = nameParts.slice(1).join(' ') || '';
      }
    });
    
    if (firstName || lastName) {
      return `${firstName} ${lastName}`.trim();
    }
    
    if (fullName) {
      return fullName;
    }
    
    if (emailAddress && emailAddress.includes('@')) {
      const emailName = emailAddress.split('@')[0];
      if (emailName.includes('.')) {
        const nameParts = emailName.split('.').map(part => 
          part.charAt(0).toUpperCase() + part.slice(1).toLowerCase()
        );
        return nameParts.join(' ');
      } else if (emailName.includes('_')) {
        const nameParts = emailName.split('_').map(part => 
          part.charAt(0).toUpperCase() + part.slice(1).toLowerCase()
        );
        return nameParts.join(' ');
      } else {
        return emailName.charAt(0).toUpperCase() + emailName.slice(1).toLowerCase();
      }
    }
    
    return `Lead ${lead.id?.slice(-6) || 'Unknown'}`;
  };

  const getStatusColor = (status: string) => {
    if (!status) return 'border-transparent';
    switch (status.toLowerCase()) {
      case 'inprogress':
        return 'border-transparent';
      case 'completed':
        return 'border-transparent';
      case 'contacted':
        return 'border-transparent';
      case 'replied':
        return 'border-transparent';
      case 'interested':
        return 'border-transparent';
      case 'not_interested':
        return 'border-transparent';
      default:
        return 'border-transparent';
    }
  };

  const getStatusStyle = (status: string): React.CSSProperties => {
    if (!status) return { backgroundColor: '#f3f4f6', color: '#1f2937' };

    const isDark = document.documentElement.classList.contains('dark');

    switch (status.toLowerCase()) {
      case 'inprogress':
        return isDark
          ? { backgroundColor: 'rgba(59, 130, 246, 0.15)', color: '#60a5fa' }
          : { backgroundColor: '#dbeafe', color: '#1e40af' };
      case 'completed':
        return isDark
          ? { backgroundColor: 'rgba(34, 197, 94, 0.15)', color: '#4ade80' }
          : { backgroundColor: '#dcfce7', color: '#166534' };
      case 'contacted':
        return isDark
          ? { backgroundColor: 'rgba(6, 182, 212, 0.15)', color: '#22d3ee' }
          : { backgroundColor: '#cffafe', color: '#155e75' };
      case 'replied':
        return isDark
          ? { backgroundColor: 'rgba(16, 185, 129, 0.15)', color: '#34d399' }
          : { backgroundColor: '#d1fae5', color: '#065f46' };
      case 'interested':
        return isDark
          ? { backgroundColor: 'rgba(168, 85, 247, 0.15)', color: '#c084fc' }
          : { backgroundColor: '#f3e8ff', color: '#6b21a8' };
      case 'not_interested':
        return isDark
          ? { backgroundColor: 'rgba(239, 68, 68, 0.15)', color: '#f87171' }
          : { backgroundColor: '#fee2e2', color: '#991b1b' };
      default:
        return isDark
          ? { backgroundColor: 'rgba(107, 114, 128, 0.2)', color: '#d1d5db' }
          : { backgroundColor: '#f3f4f6', color: '#1f2937' };
    }
  };

  const getClassificationColor = (classification: string) => {
    if (!classification) return 'bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200 border-gray-200 dark:border-gray-700';

    const lowerClassification = classification.toLowerCase();

    // Map common classification types to colors
    if (lowerClassification.includes('positive') || lowerClassification.includes('interested')) {
      return 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-300 border-green-200 dark:border-green-800';
    } else if (lowerClassification.includes('negative') || lowerClassification.includes('not interested')) {
      return 'bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-300 border-red-200 dark:border-red-800';
    } else if (lowerClassification.includes('neutral') || lowerClassification.includes('maybe')) {
      return 'bg-yellow-50 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 border-yellow-200 dark:border-yellow-800';
    } else if (lowerClassification.includes('reply') || lowerClassification.includes('response')) {
      return 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 border-blue-200 dark:border-blue-800';
    } else {
      return 'bg-purple-50 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800';
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-6xl max-h-[90vh] overflow-hidden">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MessageCircle className="h-5 w-5" />
            Lead Conversations - {campaignName}
          </DialogTitle>
        </DialogHeader>
        
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin" />
            <span className="ml-2">Loading lead history...</span>
          </div>
        ) : data ? (
          <div className="flex gap-4 h-[70vh] relative">
            {/* Filter Loading Overlay */}
            {filterLoading && (
              <div className="absolute inset-0 bg-background/70 backdrop-blur-sm z-10 flex items-center justify-center transition-all duration-300 animate-in fade-in">
                <div className="flex items-center gap-2 bg-background border rounded-lg px-4 py-2 shadow-lg animate-in zoom-in-90 duration-300">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  <span className="text-sm">Applying filter...</span>
                </div>
              </div>
            )}

            {/* Lead List Sidebar */}
            <div className={`w-1/3 border-r pr-4 transition-all duration-300 ${filterLoading ? 'opacity-30 scale-[0.98]' : 'opacity-100 scale-100'}`}>
              <div className="mb-4">
                <h3 className="font-semibold text-sm text-muted-foreground mb-2">
                  Leads ({data.totalLeads}) • Messages ({data.totalMessages})
                </h3>
                <div className="flex items-center justify-between text-xs text-muted-foreground mb-2">
                  <span>Page {data.currentPage} of {data.totalPages} • Showing {((data.currentPage - 1) * data.pageSize) + 1}-{Math.min(data.currentPage * data.pageSize, data.totalLeads)} of {data.totalLeads}</span>
                  <div className="flex items-center gap-1">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-6 w-6 p-0"
                        >
                          <Filter className="h-3 w-3" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem
                          onClick={() => setWithRepliesOnly(false)}
                          className="flex items-center justify-between cursor-pointer"
                        >
                          <span>Show All Leads</span>
                          {!withRepliesOnly && <Check className="h-4 w-4 ml-2" />}
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          onClick={() => setWithRepliesOnly(true)}
                          className="flex items-center justify-between cursor-pointer"
                        >
                          <span>Show Replied Only</span>
                          {withRepliesOnly && <Check className="h-4 w-4 ml-2" />}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-6 w-6 p-0"
                      onClick={() => {
                        const newPage = data.currentPage - 1;
                        setCurrentPage(newPage);
                        fetchLeadHistory(newPage);
                      }}
                      disabled={data.currentPage <= 1 || loading}
                    >
                      <ChevronLeft className="h-3 w-3" />
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-6 w-6 p-0"
                      onClick={() => {
                        const newPage = data.currentPage + 1;
                        setCurrentPage(newPage);
                        fetchLeadHistory(newPage);
                      }}
                      disabled={data.currentPage >= data.totalPages || loading}
                    >
                      <ChevronRight className="h-3 w-3" />
                    </Button>
                  </div>
                </div>
              </div>
              <div className="space-y-2 overflow-y-auto max-h-[calc(70vh-100px)] pr-2">
                {paginationLoading ? (
                  // Skeleton loading for pagination - show 20 items to match page size
                  Array.from({ length: 20 }, (_, index) => (
                    <div key={index} className="p-3 rounded-lg border bg-muted/20">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <Skeleton className="h-4 w-4" />
                          <Skeleton className="h-4 w-20" />
                        </div>
                        <Skeleton className="h-5 w-16 rounded-full" />
                      </div>
                      <div className="flex items-center gap-2">
                        <Skeleton className="h-3 w-32" />
                      </div>
                    </div>
                  ))
                ) : data ? (
                  data.leads.map((lead) => (
                  <div
                    key={lead.id || Math.random()}
                    className={`p-3 rounded-lg border cursor-pointer transition-all ${
                      selectedLead === lead.id && lead.id 
                        ? 'border-primary bg-primary/5 shadow-sm' 
                        : 'border-border hover:bg-muted hover:border-muted-foreground/20'
                    }`}
                    onClick={() => {
                      if (lead.id) {
                        setSelectedLead(lead.id);
                        // Fetch email history for this lead if not already fetched
                        if (!fetchedLeads.has(lead.id)) {
                          fetchEmailHistoryForLead(lead.id);
                        }
                      }
                    }}
                  >
                    <div className="flex items-center justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <User className="h-4 w-4" />
                        <span className="font-medium text-sm">
                          {getLeadDisplayName(lead)}
                        </span>
                      </div>
                      <Badge variant="custom" className={`text-xs ${getStatusColor(lead.status || 'unknown')}`} style={getStatusStyle(lead.status || 'unknown')}>
                        {lead.status || 'Unknown'}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span>ID: {lead.id?.slice(-8) || 'Unknown'}</span>
                      <span>•</span>
                      <div className="flex items-center gap-1">
                        <MessageCircle className="h-3 w-3" />
                        <span>{lead.messageCount || 0} messages</span>
                      </div>
                    </div>
                  </div>
                  ))
                ) : null}
              </div>
            </div>

            {/* Lead Detail View */}
            <div className={`flex-1 h-full transition-all duration-300 ${filterLoading ? 'opacity-30 scale-[0.98]' : 'opacity-100 scale-100'}`}>
              {selectedLeadData ? (
                <div className="h-full flex flex-col">
                  {/* Lead Header */}
                  <div className="flex-shrink-0 mb-4">
                    <div className="flex items-start justify-between">
                      <div>
                        <h2 className="text-lg font-semibold mb-2">
                          {getLeadDisplayName(selectedLeadData)}
                        </h2>
                        <div className="flex items-center gap-4">
                          <Badge variant="custom" className={getStatusColor(selectedLeadData.status || 'unknown')} style={getStatusStyle(selectedLeadData.status || 'unknown')}>
                            {selectedLeadData.status || 'Unknown'}
                          </Badge>
                          <span className="text-sm text-muted-foreground">
                            ID: {selectedLeadData.id?.slice(-8) || 'Unknown'}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Content with Toggle Buttons */}
                  <div className="flex-1 flex flex-col min-h-0">
                    <div className="flex gap-2 mb-4 flex-shrink-0">
                      <Button
                        variant={activeView === 'conversation' ? 'default' : 'outline'}
                        size="sm"
                        onClick={() => setActiveView('conversation')}
                      >
                        Email Conversation
                      </Button>
                      <Button
                        variant={activeView === 'details' ? 'default' : 'outline'}
                        size="sm"
                        onClick={() => setActiveView('details')}
                      >
                        Lead Details
                      </Button>
                    </div>
                    
                    {/* Conversation View */}
                    {activeView === 'conversation' && (
                      <div className="flex-1 overflow-y-auto">
                        {loadingHistory ? (
                          <div className="space-y-4 pb-4">
                            {/* Skeleton Loading */}
                            {[1, 2, 3].map((index) => (
                              <Card key={index}>
                                <CardHeader className="pb-4">
                                  <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                      <Skeleton className="h-4 w-4" />
                                      <Skeleton className="h-4 w-20" />
                                    </div>
                                    <Skeleton className="h-5 w-16 rounded-full" />
                                  </div>
                                </CardHeader>
                                <CardContent>
                                  <div className="border rounded-md bg-white">
                                    <div className="border-b bg-gray-50 px-4 py-3">
                                      <div className="flex items-center gap-2 mb-1">
                                        <Skeleton className="h-3 w-12" />
                                      </div>
                                      <Skeleton className="h-4 w-3/4" />
                                    </div>
                                    <div className="p-4 space-y-2">
                                      <Skeleton className="h-3 w-full" />
                                      <Skeleton className="h-3 w-5/6" />
                                      <Skeleton className="h-3 w-4/5" />
                                      <Skeleton className="h-3 w-3/4" />
                                    </div>
                                  </div>
                                </CardContent>
                              </Card>
                            ))}
                          </div>
                        ) : selectedLeadData && selectedLeadData.emailHistory && selectedLeadData.emailHistory.length > 0 ? (
                          <div className="space-y-4 pb-4">
                            {selectedLeadData.emailHistory
                              .sort((a, b) => a.sequenceNumber - b.sequenceNumber)
                              .map((email, index) => {
                                // Use index + 1 as sequence number since backend returns 0
                                const sequenceNumber = index + 1;
                                return (
                                  <Card key={index} className={email.type === 'REPLY' ? 'border-green-200 bg-green-50/30' : ''}>
                                    <CardHeader className="pb-4">
                                      <div className="flex items-center justify-between">
                                        <div className="flex flex-col gap-1">
                                          <CardTitle className="text-sm font-medium flex items-center gap-2">
                                            {email.type === 'REPLY' ? 
                                              <Reply className="h-4 w-4 text-green-600" /> : 
                                              <Mail className="h-4 w-4" />
                                            }
                                            {email.type === 'REPLY' ? 'Reply' : `Email ${sequenceNumber}`}
                                          </CardTitle>
                                          {email.time && (
                                            <div className="flex items-center gap-1 text-xs text-muted-foreground">
                                              <Clock className="h-3 w-3" />
                                              <span>{email.type === 'REPLY' ? 'Received' : 'Sent'}: {new Date(email.time).toLocaleDateString()} {new Date(email.time).toLocaleTimeString()}</span>
                                            </div>
                                          )}
                                        </div>
                                        <div className="flex items-center gap-2">
                                          {email.type === 'REPLY' ? (
                                            <Badge className="text-xs bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 border-green-200 dark:border-green-800">
                                              Reply
                                            </Badge>
                                          ) : (
                                            <Badge variant="outline" className="text-xs">
                                              Sequence {sequenceNumber}
                                            </Badge>
                                          )}
                                          {/* Only show classification for replies */}
                                          {email.type === 'REPLY' && (
                                            <>
                                              {email.isClassified && email.classificationResult ? (
                                                <Badge
                                                  className={`text-xs border ${getClassificationColor(email.classificationResult)}`}
                                                  variant="outline"
                                                >
                                                  {email.classificationResult}
                                                </Badge>
                                              ) : (
                                                <Badge
                                                  className="text-xs border bg-gray-50 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border-gray-300 dark:border-gray-700"
                                                  variant="outline"
                                                >
                                                  Pending Analysis
                                                </Badge>
                                              )}
                                            </>
                                          )}
                                        </div>
                                      </div>
                                    </CardHeader>
                                <CardContent>
                                  {/* Email Preview */}
                                  <div className="border rounded-md bg-muted/30 dark:bg-muted/10">
                                    <div className="border-b px-4 py-3 bg-muted/50 dark:bg-muted/20">
                                      <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
                                        <span className="font-medium">Subject:</span>
                                      </div>
                                      <div className="font-medium break-words overflow-wrap-anywhere" style={{ wordBreak: 'break-word', overflowWrap: 'anywhere' }}>
                                        {email.subject || 'No Subject'}
                                      </div>
                                    </div>

                                    {/* Email Body */}
                                    <div className="p-4 overflow-x-auto max-w-full">
                                      <div
                                        className="text-sm prose prose-sm dark:prose-invert max-w-none break-words overflow-wrap-anywhere whitespace-pre-wrap"
                                        style={{ wordBreak: 'break-word', overflowWrap: 'anywhere' }}
                                        dangerouslySetInnerHTML={{
                                          __html: email.body || 'No content available'
                                        }}
                                      />

                                      {/* Classification Status - Only show for replies */}
                                      {email.type === 'REPLY' && (
                                        <div className="mt-4 pt-3 border-t">
                                          <div className="flex items-center justify-between text-xs text-muted-foreground">
                                            {email.isClassified && email.classifiedAt ? (
                                              <>
                                                <span>
                                                  Analyzed by RevReply: {new Date(email.classifiedAt).toLocaleDateString()} {new Date(email.classifiedAt).toLocaleTimeString()}
                                                </span>
                                                {email.classificationResult && (
                                                  <span className="font-medium">
                                                    Classification: {email.classificationResult}
                                                  </span>
                                                )}
                                              </>
                                            ) : (
                                              <>
                                                <span className="text-gray-400">
                                                  RevReply analysis: Pending
                                                </span>
                                                <span className="text-gray-400 text-xs">
                                                  Will be analyzed in next sync cycle
                                                </span>
                                              </>
                                            )}
                                          </div>
                                        </div>
                                      )}
                                    </div>
                                  </div>
                                </CardContent>
                                  </Card>
                                )
                              })}
                          </div>
                        ) : (
                          <div className="text-center py-8 text-muted-foreground">
                            <Mail className="h-12 w-12 mx-auto mb-2 opacity-50" />
                            <p>No email history available</p>
                          </div>
                        )}
                      </div>
                    )}
                    
                    {/* Details View */}
                    {activeView === 'details' && (
                      <div className="flex-1 overflow-y-auto">
                        <div className="space-y-4 pb-4">
                          <div>
                            <h4 className="font-medium text-sm mb-3">Lead Information</h4>
                            <div className="grid grid-cols-1 gap-2 text-sm bg-muted p-3 rounded-md">
                              <div>
                                <span className="font-medium">Name:</span>
                                <span className="ml-2 text-foreground font-medium">{getLeadDisplayName(selectedLeadData)}</span>
                              </div>
                              <div>
                                <span className="font-medium">Lead ID:</span>
                                <span className="ml-2 text-muted-foreground font-mono">{selectedLeadData.id || 'Unknown'}</span>
                              </div>
                              <div>
                                <span className="font-medium">Status:</span>
                                <span className="ml-2">
                                  <Badge className={getStatusColor(selectedLeadData.status || 'unknown')}>
                                    {selectedLeadData.status || 'Unknown'}
                                  </Badge>
                                </span>
                              </div>
                              <div>
                                <span className="font-medium">Total Emails:</span>
                                <span className="ml-2 text-muted-foreground">{selectedLeadData.emailHistory?.length || 0}</span>
                              </div>
                            </div>
                          </div>

                          {/* Custom Fields */}
                          {selectedLeadData.customFields && selectedLeadData.customFields.length > 0 && (
                            <Card>
                              <CardHeader className="pb-3">
                                <CardTitle className="text-sm font-medium flex items-center gap-2">
                                  <User className="h-4 w-4" />
                                  Custom Fields
                                </CardTitle>
                              </CardHeader>
                              <CardContent>
                                <div className="grid grid-cols-1 gap-3">
                                  {selectedLeadData.customFields?.map((field, index) => {
                                    // Skip the id field
                                    if (field.key === 'id') {
                                      return null;
                                    }
                                    
                                    // Try to parse JSON values
                                    let displayValue = field.value;
                                    let parsedFields: Array<{key: string, value: string}> = [];
                                    
                                    try {
                                      if (field.value.startsWith('{') && field.value.endsWith('}')) {
                                        const parsed = JSON.parse(field.value);
                                        parsedFields = Object.entries(parsed).map(([k, v]) => ({
                                          key: k,
                                          value: String(v)
                                        }));
                                      }
                                    } catch (e) {
                                      // Not valid JSON, use original value
                                    }
                                    
                                    if (parsedFields.length > 0) {
                                      // Display parsed JSON as separate fields
                                      return parsedFields.map((subField, subIndex) => (
                                        <div key={`${index}-${subIndex}`} className="flex items-center justify-between py-2 px-3 bg-gray-50 border rounded-lg">
                                          <span className="font-medium text-sm text-gray-700 capitalize">
                                            {subField.key.replace(/[._]/g, ' ')}
                                          </span>
                                          <span className="text-sm text-gray-900 font-medium bg-white px-2 py-1 rounded border">
                                            {subField.value === '' || subField.value === '--' ? '—' : subField.value}
                                          </span>
                                        </div>
                                      ));
                                    } else {
                                      // Display regular field
                                      return (
                                        <div key={index} className="flex items-center justify-between py-2 px-3 bg-gray-50 border rounded-lg">
                                          <span className="font-medium text-sm text-gray-700 capitalize">
                                            {field.key.replace(/_/g, ' ')}
                                          </span>
                                          <span className="text-sm text-gray-900 font-medium bg-white px-2 py-1 rounded border">
                                            {field.value === '' || field.value === '--' ? '—' : field.value}
                                          </span>
                                        </div>
                                      );
                                    }
                                  })}
                                </div>
                              </CardContent>
                            </Card>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-center h-full text-muted-foreground">
                  <div className="text-center">
                    <User className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>Select a lead to view details</p>
                  </div>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="text-center py-12 text-muted-foreground">
            <MessageCircle className="h-12 w-12 mx-auto mb-2 opacity-50" />
            <p>No lead history available</p>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}