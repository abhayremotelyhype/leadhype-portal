'use client';

import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Client } from '@/types';

interface AssignClientModalProps<T = any> {
  isOpen: boolean;
  onClose: () => void;
  selectedItems: T[];
  selectedItemForAssignment?: T | null;
  clients: Client[];
  isLoading: boolean;
  isAssigning: boolean;
  onAssign: (clientId: string) => Promise<void>;
  entityType: 'account' | 'campaign';
  getEntityName?: (item: T) => string;
}

export function AssignClientModal<T extends { id?: string; name?: string; email?: string }>({
  isOpen,
  onClose,
  selectedItems,
  selectedItemForAssignment,
  clients,
  isLoading,
  isAssigning,
  onAssign,
  entityType,
  getEntityName,
}: AssignClientModalProps<T>) {
  const [selectedClientId, setSelectedClientId] = useState<string>('');

  // Reset selected client when modal opens/closes
  useEffect(() => {
    if (!isOpen) {
      setSelectedClientId('');
    }
  }, [isOpen]);

  const handleAssign = async () => {
    if (selectedClientId) {
      await onAssign(selectedClientId);
    }
  };

  const getDefaultEntityName = (item: T): string => {
    if (getEntityName) {
      return getEntityName(item);
    }
    return item.name || item.email || `${entityType} ${item.id}` || `Unknown ${entityType}`;
  };

  const getTitle = () => {
    if (selectedItemForAssignment) {
      const entityName = getDefaultEntityName(selectedItemForAssignment);
      return `Assign Client to ${entityName}`;
    }
    const count = selectedItems.length;
    const entityLabel = entityType === 'account' ? 'Email Accounts' : 'Campaigns';
    return `Assign Client to ${count} ${entityLabel}`;
  };

  const getDescription = () => {
    if (selectedItemForAssignment) {
      return `Select a client to assign to this ${entityType}.`;
    }
    const count = selectedItems.length;
    const entityLabel = entityType === 'account' ? 'email accounts' : 'campaigns';
    return `Select a client to assign to the ${count} selected ${entityLabel}.`;
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="w-[95vw] sm:max-w-[425px] max-h-[85vh] overflow-y-auto mx-2">
        <DialogHeader className="space-y-2">
          <DialogTitle className="text-base sm:text-lg font-medium pr-6 leading-tight">
            {selectedItemForAssignment ? (
              <>
                Assign Client to{' '}
                <span className="font-bold">
                  {getDefaultEntityName(selectedItemForAssignment)}
                </span>
              </>
            ) : (
              getTitle()
            )}
          </DialogTitle>
          <DialogDescription className="text-sm sm:text-base text-muted-foreground">
            {getDescription()}
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-4 py-3 overflow-hidden">
          <div className="space-y-3">
            <label htmlFor="client-select" className="text-sm sm:text-base font-medium leading-none">
              Select Client
            </label>
            {isLoading ? (
              <div className="flex items-center justify-center py-4">
                <div className="animate-spin w-5 h-5 border-2 border-primary border-t-transparent rounded-full"></div>
              </div>
            ) : clients && clients.length > 0 ? (
              <select
                id="client-select"
                value={selectedClientId}
                onChange={(e) => setSelectedClientId(e.target.value)}
                className="w-full rounded-md border border-input bg-background px-3 py-3 sm:py-2 text-base sm:text-sm min-h-[44px] sm:min-h-[auto]"
                disabled={isAssigning}
              >
                <option value="">Select a client...</option>
                {clients.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name} {client.company && `(${client.company})`}
                  </option>
                ))}
              </select>
            ) : (
              <div className="w-full rounded-md border border-input bg-muted px-3 py-2 text-sm text-muted-foreground">
                No clients available. Please create a client first.
              </div>
            )}
          </div>
          
          <div className="w-full pt-2">
            <div className="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 sm:gap-2">
              <Button
                variant="outline"
                onClick={onClose}
                disabled={isAssigning}
                className="h-11 sm:h-9"
              >
                Cancel
              </Button>
              <Button
                onClick={handleAssign}
                disabled={isAssigning || !selectedClientId}
                className="h-11 sm:h-9"
              >
              {isAssigning ? (
                <>
                  <div className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full mr-2"></div>
                  Assigning...
                </>
              ) : (
                'Assign Client'
              )}
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}