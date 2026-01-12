'use client';

import { useState } from 'react';
import { Download, Calendar } from 'lucide-react';
import { format } from 'date-fns';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Calendar as CalendarComponent } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { ClientSelectionDropdown } from '@/components/client-selection-dropdown';
import { useToast } from '@/hooks/use-toast';
import { apiClient, ENDPOINTS } from '@/lib/api';

interface DownloadModalProps {
  isOpen: boolean;
  onClose: () => void;
  entityType: 'campaigns' | 'email-accounts';
  entityLabel: string; // e.g., "campaigns", "email accounts"
}

export function DownloadModal({
  isOpen,
  onClose,
  entityType,
  entityLabel,
}: DownloadModalProps) {
  const { toast } = useToast();
  const [downloadStartDate, setDownloadStartDate] = useState<Date | null>(null);
  const [downloadEndDate, setDownloadEndDate] = useState<Date | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);
  const [selectedClientIds, setSelectedClientIds] = useState<string[]>([]);
  const [downloadAllTime, setDownloadAllTime] = useState(false);

  const handleClose = () => {
    if (!isDownloading) {
      onClose();
      // Reset form state
      setDownloadStartDate(null);
      setDownloadEndDate(null);
      setSelectedClientIds([]);
      setDownloadAllTime(false);
    }
  };

  const handleDownloadConfirm = async () => {
    if (!downloadAllTime && (!downloadStartDate || !downloadEndDate)) {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Please select both start and end dates or enable "All time data"',
      });
      return;
    }

    if (!downloadAllTime && downloadStartDate && downloadEndDate && downloadStartDate > downloadEndDate) {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Start date must be before end date',
      });
      return;
    }

    setIsDownloading(true);
    try {
      const params: Record<string, string> = {};
      
      if (!downloadAllTime && downloadStartDate && downloadEndDate) {
        params.startDate = format(downloadStartDate, 'yyyy-MM-dd');
        params.endDate = format(downloadEndDate, 'yyyy-MM-dd');
      }
      
      if (selectedClientIds.length > 0) {
        params.clientIds = selectedClientIds.join(',');
      }
      
      const endpoint = entityType === 'campaigns' ? ENDPOINTS.campaigns : ENDPOINTS.emailAccounts;
      const response = await apiClient.get<Blob>(`${endpoint}/download`, params, {
        responseType: 'blob'
      });

      // Create download link
      const url = window.URL.createObjectURL(response);
      const link = document.createElement('a');
      link.href = url;
      const fileName = downloadAllTime 
        ? `${entityType}-all-time.csv`
        : `${entityType}-${format(downloadStartDate!, 'yyyy-MM-dd')}-to-${format(downloadEndDate!, 'yyyy-MM-dd')}.csv`;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);

      toast({
        title: 'Success',
        description: `${entityLabel} data downloaded successfully`,
      });
      
      handleClose();
    } catch (error) {
      console.error('Failed to download data:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to download data',
      });
    } finally {
      setIsDownloading(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="w-[95vw] max-w-sm p-4 data-[state=open]:slide-in-from-bottom-0 data-[state=closed]:slide-out-to-bottom-0 sm:w-auto sm:max-w-md sm:slide-in-from-bottom-0">
        <DialogHeader className="pb-2">
          <DialogTitle className="text-base">Download Data</DialogTitle>
          <DialogDescription className="text-sm text-muted-foreground">
            Export {entityLabel} as CSV
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-2 sm:space-y-3">
          <div className="flex items-center space-x-2">
            <Checkbox
              id="all-time"
              checked={downloadAllTime}
              onCheckedChange={(checked) => {
                setDownloadAllTime(checked as boolean);
                if (checked) {
                  setDownloadStartDate(null);
                  setDownloadEndDate(null);
                }
              }}
            />
            <label
              htmlFor="all-time"
              className="text-xs sm:text-sm font-medium leading-none"
            >
              All time data
            </label>
          </div>
          
          {!downloadAllTime && (
            <div className="space-y-2 sm:space-y-3">
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Start Date</label>
                <Popover>
                  <PopoverTrigger asChild>
                    <Button variant="outline" size="sm" className="w-full justify-start text-left font-normal h-8 sm:h-9 px-2 sm:px-3">
                      <Calendar className="mr-1 sm:mr-2 h-3 w-3 sm:h-4 sm:w-4" />
                      <span className="text-xs sm:text-sm truncate">
                        {downloadStartDate ? format(downloadStartDate, 'MMM dd, yyyy') : 'Select start date'}
                      </span>
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <CalendarComponent
                      mode="single"
                      selected={downloadStartDate || undefined}
                      onSelect={(date) => setDownloadStartDate(date || null)}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </div>
              
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">End Date</label>
                <Popover>
                  <PopoverTrigger asChild>
                    <Button variant="outline" size="sm" className="w-full justify-start text-left font-normal h-8 sm:h-9 px-2 sm:px-3">
                      <Calendar className="mr-1 sm:mr-2 h-3 w-3 sm:h-4 sm:w-4" />
                      <span className="text-xs sm:text-sm truncate">
                        {downloadEndDate ? format(downloadEndDate, 'MMM dd, yyyy') : 'Select end date'}
                      </span>
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <CalendarComponent
                      mode="single"
                      selected={downloadEndDate || undefined}
                      onSelect={(date) => setDownloadEndDate(date || null)}
                      initialFocus
                      disabled={(date) => downloadStartDate ? date < downloadStartDate : false}
                    />
                  </PopoverContent>
                </Popover>
              </div>
            </div>
          )}
          
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground">Client Filter</label>
            <ClientSelectionDropdown
              selectedClientIds={selectedClientIds}
              onSelectionChange={setSelectedClientIds}
              placeholder="All clients"
              className="w-full h-8"
            />
          </div>
        </div>

        <DialogFooter className="pt-2 flex-col sm:flex-row gap-2 sm:gap-0">
          <Button 
            variant="outline" 
            size="sm" 
            className="text-xs h-8 px-3 sm:px-4 w-full sm:w-auto" 
            onClick={handleClose}
            disabled={isDownloading}
          >
            Cancel
          </Button>
          <Button 
            size="sm" 
            className="text-xs h-8 px-3 sm:px-4 w-full sm:w-auto" 
            onClick={handleDownloadConfirm} 
            disabled={isDownloading}
          >
            {isDownloading ? (
              <>
                <div className="mr-1.5 sm:mr-2 h-3 w-3 animate-spin rounded-full border-2 border-current border-t-transparent" />
                <span className="truncate">Downloading...</span>
              </>
            ) : (
              <>
                <Download className="mr-1.5 sm:mr-2 h-3 w-3" />
                <span className="truncate">Download CSV</span>
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}