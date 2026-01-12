'use client';

import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Copy, Mail, CheckCircle2, AlertCircle, ExternalLink } from 'lucide-react';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';
import { useRouter } from 'next/navigation';

interface EmailAccount {
  id: number;
  email: string;
  name: string;
  status: string;
  clientName?: string;
  clientColor?: string;
}

interface EmailAccountsModalProps {
  isOpen: boolean;
  onClose: () => void;
  campaignId: string;
  campaignName?: string;
}

export function EmailAccountsModal({ isOpen, onClose, campaignId, campaignName }: EmailAccountsModalProps) {
  const [emailAccounts, setEmailAccounts] = useState<EmailAccount[]>([]);
  const [loading, setLoading] = useState(false);
  const { toast } = useToast();
  const router = useRouter();

  useEffect(() => {
    if (isOpen && campaignId) {
      loadEmailAccounts();
    }
  }, [isOpen, campaignId]);

  const loadEmailAccounts = async () => {
    setLoading(true);
    try {
      const accounts = await apiClient.get<EmailAccount[]>(`${ENDPOINTS.campaigns}/${campaignId}/email-accounts`);
      setEmailAccounts(accounts || []);
    } catch (error) {
      console.error('Failed to load email accounts:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load email accounts',
      });
    } finally {
      setLoading(false);
    }
  };

  const copyAllEmails = () => {
    const emails = emailAccounts.map(account => account.email).join(', ');
    navigator.clipboard.writeText(emails).then(() => {
      toast({
        title: 'Success',
        description: `Copied ${emailAccounts.length} email addresses to clipboard`,
      });
    }).catch(() => {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to copy emails to clipboard',
      });
    });
  };

  const copyEmail = (email: string) => {
    navigator.clipboard.writeText(email).then(() => {
      toast({
        title: 'Success',
        description: 'Email copied to clipboard',
      });
    }).catch(() => {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to copy email',
      });
    });
  };

  const getStatusBadgeColor = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400 border-green-200 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/30';
      case 'inactive':
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400 border-gray-200 dark:border-gray-800 hover:bg-gray-100 dark:hover:bg-gray-900/30';
      case 'warmup only':
        return 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400 border-amber-200 dark:border-amber-800 hover:bg-amber-100 dark:hover:bg-amber-900/30';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400 border-gray-200 dark:border-gray-800 hover:bg-gray-100 dark:hover:bg-gray-900/30';
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[700px] max-h-[80vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            Email Accounts
            {campaignName && <span className="text-muted-foreground">- {campaignName}</span>}
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-hidden">
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          ) : emailAccounts.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <AlertCircle className="h-12 w-12 text-muted-foreground mb-4" />
              <p className="text-muted-foreground">No email accounts linked to this campaign</p>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  {emailAccounts.length} email account{emailAccounts.length !== 1 ? 's' : ''} linked to this campaign
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    onClick={copyAllEmails}
                    size="sm"
                    variant="outline"
                    className="flex items-center gap-2"
                  >
                    <Copy className="h-4 w-4" />
                    Copy All Emails
                  </Button>
                  <Button
                    onClick={() => {
                      router.push(`/email-accounts?campaignId=${campaignId}`);
                      onClose();
                    }}
                    size="sm"
                    variant="outline"
                    className="flex items-center gap-2"
                  >
                    <ExternalLink className="h-4 w-4" />
                    View in Email Accounts
                  </Button>
                </div>
              </div>

              <div className="max-h-96 overflow-y-auto space-y-2 pr-2">
                {emailAccounts.map((account) => (
                  <div
                    key={account.id}
                    className="flex items-center justify-between p-3 border rounded-lg hover:bg-muted/50 transition-colors"
                  >
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <p className="font-medium truncate">{account.email}</p>
                        <Button
                          onClick={() => copyEmail(account.email)}
                          size="sm"
                          variant="ghost"
                          className="h-6 w-6 p-0 shrink-0"
                        >
                          <Copy className="h-3 w-3" />
                        </Button>
                      </div>
                      {account.name && (
                        <p className="text-sm text-muted-foreground truncate">{account.name}</p>
                      )}
                      {account.clientName && (
                        <div className="flex items-center gap-1 mt-1">
                          <div
                            className="w-2 h-2 rounded-full"
                            style={{ backgroundColor: account.clientColor || '#6B7280' }}
                          />
                          <span className="text-xs text-muted-foreground">{account.clientName}</span>
                        </div>
                      )}
                    </div>
                    <div className="shrink-0 ml-2">
                      <Badge className={`text-xs px-2 py-0.5 ${getStatusBadgeColor(account.status)}`}>
                        {account.status}
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