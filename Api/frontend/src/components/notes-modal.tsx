'use client';

import { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { useToast } from '@/hooks/use-toast';
import { apiClient, handleApiErrorWithToast } from '@/lib/api';
import { FileText, Loader2 } from 'lucide-react';

interface NotesModalProps {
  isOpen: boolean;
  onClose: () => void;
  itemType: 'campaign' | 'emailAccount';
  itemId: string;
  itemName: string;
  initialNotes: string | null;
  onNotesUpdated: (notes: string | null) => void;
}

export function NotesModal({
  isOpen,
  onClose,
  itemType,
  itemId,
  itemName,
  initialNotes,
  onNotesUpdated
}: NotesModalProps) {
  const [notes, setNotes] = useState(initialNotes || '');
  const [saving, setSaving] = useState(false);
  const { toast } = useToast();

  // Reset notes when modal opens/closes or initialNotes change
  useEffect(() => {
    if (isOpen) {
      setNotes(initialNotes || '');
    }
  }, [isOpen, initialNotes]);

  const handleSave = async () => {
    try {
      setSaving(true);
      
      const endpoint = itemType === 'campaign' 
        ? `/api/campaigns/${itemId}/notes`
        : `/api/email-accounts/${itemId}/notes`;
      
      await apiClient.put(endpoint, {
        notes: notes.trim() || null
      });

      onNotesUpdated(notes.trim() || null);
      toast({
        title: "Success",
        description: `Notes updated for ${itemType === 'campaign' ? 'campaign' : 'email account'}`,
      });
      onClose();
    } catch (error: any) {
      handleApiErrorWithToast(error, 'update notes', toast);
    } finally {
      setSaving(false);
    }
  };

  const handleOpenChange = (open: boolean) => {
    if (!open && !saving) {
      onClose();
    }
  };

  const hasChanges = notes.trim() !== (initialNotes || '').trim();

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Notes - {itemType === 'campaign' ? 'Campaign' : 'Email Account'}
          </DialogTitle>
          <DialogDescription>
            Add or edit notes for <span className="font-medium">{itemName}</span>
          </DialogDescription>
        </DialogHeader>
        
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="notes" className="text-sm font-medium">
              Notes
            </Label>
            <Textarea
              id="notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={`Add notes for this ${itemType === 'campaign' ? 'campaign' : 'email account'}...`}
              className="min-h-[120px] resize-none"
              disabled={saving}
            />
            <p className="text-xs text-muted-foreground">
              {notes.length}/1000 characters
            </p>
          </div>
        </div>

        <DialogFooter className="gap-2">
          <Button 
            variant="outline" 
            onClick={() => onClose()} 
            disabled={saving}
          >
            Cancel
          </Button>
          <Button 
            onClick={handleSave} 
            disabled={saving || (!hasChanges && notes.trim() === '')}
          >
            {saving ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Saving...
              </>
            ) : (
              'Save Notes'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}