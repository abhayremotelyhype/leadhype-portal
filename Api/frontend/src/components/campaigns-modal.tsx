'use client';

import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Copy, Megaphone, AlertCircle, ExternalLink, Calendar, Users, Mail, MessageSquare } from 'lucide-react';
import { apiClient, ENDPOINTS, formatDate } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';
import { useRouter } from 'next/navigation';

interface Campaign {
  id: string;
  campaignId: number;
  name: string;
  status: string;
  clientName?: string;
  clientColor?: string;
  totalLeads: number;
  totalSent: number;
  totalReplied: number;
  createdAt: string;
  updatedAt: string;
}

interface CampaignsModalProps {
  isOpen: boolean;
  onClose: () => void;
  emailAccountId: string;
  emailAccountName?: string;
}

export function CampaignsModal({ isOpen, onClose, emailAccountId, emailAccountName }: CampaignsModalProps) {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(false);
  const { toast } = useToast();
  const router = useRouter();

  useEffect(() => {
    if (isOpen && emailAccountId) {
      loadCampaigns();
    }
  }, [isOpen, emailAccountId]);

  const loadCampaigns = async () => {
    setLoading(true);
    try {
      const campaigns = await apiClient.get<Campaign[]>(`${ENDPOINTS.emailAccounts}/${emailAccountId}/campaigns`);
      setCampaigns(campaigns || []);
    } catch (error) {
      console.error('Failed to load campaigns:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load campaigns',
      });
    } finally {
      setLoading(false);
    }
  };

  const copyAllCampaignNames = () => {
    const campaignNames = campaigns.map(campaign => campaign.name).join(', ');
    navigator.clipboard.writeText(campaignNames).then(() => {
      toast({
        title: 'Success',
        description: `Copied ${campaigns.length} campaign names to clipboard`,
      });
    }).catch(() => {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to copy campaign names to clipboard',
      });
    });
  };

  const copyCampaignName = (name: string) => {
    navigator.clipboard.writeText(name).then(() => {
      toast({
        title: 'Success',
        description: 'Campaign name copied to clipboard',
      });
    }).catch(() => {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to copy campaign name',
      });
    });
  };

  const getStatusBadgeColor = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400 border-green-200 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/30';
      case 'paused':
        return 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400 border-amber-200 dark:border-amber-800 hover:bg-amber-100 dark:hover:bg-amber-900/30';
      case 'completed':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400 border-blue-200 dark:border-blue-800 hover:bg-blue-100 dark:hover:bg-blue-900/30';
      case 'draft':
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400 border-gray-200 dark:border-gray-800 hover:bg-gray-100 dark:hover:bg-gray-900/30';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400 border-gray-200 dark:border-gray-800 hover:bg-gray-100 dark:hover:bg-gray-900/30';
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[800px] max-h-[80vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Megaphone className="h-5 w-5" />
            Campaigns
            {emailAccountName && <span className="text-muted-foreground">- {emailAccountName}</span>}
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-hidden">
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          ) : campaigns.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <AlertCircle className="h-12 w-12 text-muted-foreground mb-4" />
              <p className="text-muted-foreground">No campaigns using this email account</p>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  {campaigns.length} campaign{campaigns.length !== 1 ? 's' : ''} using this email account
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    onClick={copyAllCampaignNames}
                    size="sm"
                    variant="outline"
                    className="flex items-center gap-2"
                  >
                    <Copy className="h-4 w-4" />
                    Copy All Names
                  </Button>
                  <Button
                    onClick={() => {
                      router.push(`/campaigns?emailAccountId=${emailAccountId}`);
                      onClose();
                    }}
                    size="sm"
                    variant="outline"
                    className="flex items-center gap-2"
                  >
                    <ExternalLink className="h-4 w-4" />
                    View in Campaigns
                  </Button>
                </div>
              </div>

              <div className="max-h-96 overflow-y-auto space-y-2 pr-2">
                {campaigns.map((campaign) => (
                  <div
                    key={campaign.id}
                    className="flex items-center justify-between p-3 border rounded-lg hover:bg-muted/50 transition-colors cursor-pointer"
                    onClick={() => {
                      router.push(`/campaigns/${campaign.campaignId}`);
                      onClose();
                    }}
                  >
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <p className="font-medium truncate">{campaign.name}</p>
                        <Button
                          onClick={(e) => {
                            e.stopPropagation();
                            copyCampaignName(campaign.name);
                          }}
                          size="sm"
                          variant="ghost"
                          className="h-6 w-6 p-0 shrink-0"
                        >
                          <Copy className="h-3 w-3" />
                        </Button>
                      </div>
                      <div className="flex items-center gap-3 text-sm text-muted-foreground mb-2">
                        <div className="flex items-center gap-1">
                          <Users className="h-3 w-3" />
                          <span>{campaign.totalLeads.toLocaleString()} leads</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Mail className="h-3 w-3" />
                          <span>{campaign.totalSent.toLocaleString()} sent</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <MessageSquare className="h-3 w-3" />
                          <span>{campaign.totalReplied.toLocaleString()} replied</span>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {campaign.clientName && (
                          <div className="flex items-center gap-1">
                            <div
                              className="w-2 h-2 rounded-full"
                              style={{ backgroundColor: campaign.clientColor || '#6B7280' }}
                            />
                            <span className="text-xs text-muted-foreground">{campaign.clientName}</span>
                          </div>
                        )}
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <Calendar className="h-3 w-3" />
                          <span>{formatDate(campaign.updatedAt)}</span>
                        </div>
                      </div>
                    </div>
                    <div className="shrink-0 ml-2">
                      <Badge className={`text-xs px-2 py-0.5 ${getStatusBadgeColor(campaign.status)}`}>
                        {campaign.status}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}