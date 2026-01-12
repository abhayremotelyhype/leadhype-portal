'use client';

import { useState, useEffect, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { useToast } from '@/hooks/use-toast';
import { Tags, X, Plus } from 'lucide-react';
import { apiClient, handleApiErrorWithToast, ENDPOINTS } from '@/lib/api';

interface TagSelectorProps {
  entityType: 'campaign' | 'email-account';
  entityId: string | number;
  selectedTags: string[];
  onTagsChange: (tags: string[]) => void;
  trigger?: React.ReactNode;
}

export function TagSelector({ entityType, entityId, selectedTags, onTagsChange, trigger }: TagSelectorProps) {
  const [tempSelectedTags, setTempSelectedTags] = useState<string[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [inputValue, setInputValue] = useState('');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const { toast } = useToast();

  // Common tag colors for visual consistency
  const TAG_COLORS = [
    '#3B82F6', '#EF4444', '#10B981', '#F59E0B', '#8B5CF6',
    '#EC4899', '#06B6D4', '#84CC16', '#F97316', '#6366F1',
    '#14B8A6', '#F59E0B', '#EF4444', '#8B5CF6', '#06B6D4'
  ];

  const getTagColor = (tag: string) => {
    // Simple hash function to get consistent colors for tags
    let hash = 0;
    for (let i = 0; i < tag.length; i++) {
      hash = tag.charCodeAt(i) + ((hash << 5) - hash);
    }
    return TAG_COLORS[Math.abs(hash) % TAG_COLORS.length];
  };

  useEffect(() => {
    if (dialogOpen) {
      setTempSelectedTags([...selectedTags]);
      setInputValue(''); // Clear input when dialog opens
      setEditingIndex(null); // Clear editing state
    }
  }, [dialogOpen, selectedTags]);

  const handleTagToggle = (tagName: string, checked: boolean) => {
    if (checked) {
      setTempSelectedTags([...tempSelectedTags, tagName]);
    } else {
      setTempSelectedTags(tempSelectedTags.filter(name => name !== tagName));
    }
  };

  const saveTags = async () => {
    // Check if there's unsaved text in the input
    const trimmedInput = inputValue.trim();
    if (trimmedInput) {
      toast({
        variant: 'destructive',
        title: 'Unsaved Tag',
        description: `You have typed "${trimmedInput}" but haven't added it as a tag. Press Enter to add it, or clear the input to continue.`,
      });
      // Focus the input to highlight the unsaved text
      inputRef.current?.focus();
      return;
    }

    try {
      setLoading(true);
      const endpoint = entityType === 'campaign' 
        ? `${ENDPOINTS.campaigns}/${entityId}/tags`
        : `${ENDPOINTS.emailAccounts}/${entityId}/tags`;

      const response = await apiClient.post(endpoint, tempSelectedTags);
      
      onTagsChange(tempSelectedTags);
      setDialogOpen(false);
      toast({
        title: 'Success',
        description: 'Tags updated successfully',
      });
    } catch (error: any) {
      handleApiErrorWithToast(error, 'update tags', toast);
    } finally {
      setLoading(false);
    }
  };

  const removeTag = async (tagName: string) => {
    try {
      const endpoint = entityType === 'campaign' 
        ? `${ENDPOINTS.campaigns}/${entityId}/tags/${encodeURIComponent(tagName)}`
        : `${ENDPOINTS.emailAccounts}/${entityId}/tags/${encodeURIComponent(tagName)}`;

      await apiClient.delete(endpoint);
      
      const updatedTags = selectedTags.filter(tag => tag !== tagName);
      onTagsChange(updatedTags);
      toast({
        title: 'Success',
        description: 'Tag removed successfully',
      });
    } catch (error: any) {
      handleApiErrorWithToast(error, 'remove tag', toast);
    }
  };

  const handleInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    const trimmedValue = inputValue.trim();

    if (e.key === 'Enter' && trimmedValue) {
      e.preventDefault();
      addTag(trimmedValue);
    } else if (e.key === 'Backspace' && inputValue === '' && tempSelectedTags.length > 0) {
      // Only enter edit mode when input is completely empty and we have tags
      e.preventDefault(); // Prevent the default backspace behavior
      const lastIndex = tempSelectedTags.length - 1;
      const lastTag = tempSelectedTags[lastIndex];
      setInputValue(lastTag);
      setEditingIndex(lastIndex);
      // Remove the tag from the array temporarily while editing
      setTempSelectedTags(tempSelectedTags.slice(0, -1));
    } else if (e.key === 'Escape' && editingIndex !== null) {
      // Cancel editing
      setInputValue('');
      setEditingIndex(null);
    }
  };

  const addTag = (tagName: string) => {
    if (!tagName) return;

    // Check if tag already exists
    if (tempSelectedTags.includes(tagName)) {
      toast({
        variant: 'destructive',
        title: 'Tag Already Exists',
        description: 'This tag is already selected',
      });
      setInputValue('');
      setEditingIndex(null);
      return;
    }

    if (editingIndex !== null) {
      // We're editing an existing tag
      const newTags = [...tempSelectedTags];
      newTags.splice(editingIndex, 0, tagName); // Insert at the editing position
      setTempSelectedTags(newTags);
      setEditingIndex(null);
    } else {
      // Adding a new tag
      setTempSelectedTags([...tempSelectedTags, tagName]);
    }

    setInputValue('');
    inputRef.current?.focus();
  };

  const removeTagAtIndex = (index: number) => {
    const newTags = tempSelectedTags.filter((_, i) => i !== index);
    setTempSelectedTags(newTags);
    inputRef.current?.focus();
  };

  return (
    <div className="space-y-3">
      {/* Display current tags */}
      {selectedTags.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {selectedTags.map((tag) => (
            <Badge
              key={tag}
              style={{ backgroundColor: getTagColor(tag), color: 'white' }}
              className="flex items-center gap-1.5 py-1 px-2.5 text-xs font-medium transition-all hover:scale-105"
            >
              {tag}
              <X
                className="w-3 h-3 cursor-pointer hover:opacity-70 transition-opacity"
                onClick={() => removeTag(tag)}
                aria-label={`Remove ${tag} tag`}
              />
            </Badge>
          ))}
        </div>
      )}

      {/* Tag selector dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogTrigger asChild>
          {trigger || (
            <Button variant="ghost" size="sm" className="h-8 px-2 text-xs">
              <Tags className="w-3 h-3 mr-1" />
              Tags
            </Button>
          )}
        </DialogTrigger>
        <DialogContent className="max-w-lg">
          <DialogHeader className="pb-3">
            <DialogTitle className="text-base font-semibold">Manage Tags</DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground">
              Type to add tags, press Enter to create, Backspace to edit last tag.
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-3">
            {/* Inline Tag Input */}
            <div className="space-y-2">
              <Label className="text-sm font-medium text-foreground">
                Tags {tempSelectedTags.length > 0 && (
                  <span className="text-xs text-muted-foreground ml-1">
                    ({tempSelectedTags.length})
                  </span>
                )}
              </Label>
              <div className="min-h-[80px] p-3 border-2 rounded-lg bg-background focus-within:border-primary transition-colors">
                <div className="flex flex-wrap gap-2 items-center">
                  {tempSelectedTags.map((tag, index) => (
                    <Badge
                      key={`${tag}-${index}`}
                      style={{ backgroundColor: getTagColor(tag), color: 'white' }}
                      className="flex items-center gap-1.5 py-1.5 px-3 text-sm font-medium transition-all hover:scale-105 cursor-pointer"
                      onClick={() => {
                        // Click to edit tag
                        setInputValue(tag);
                        setEditingIndex(index);
                        removeTagAtIndex(index);
                        inputRef.current?.focus();
                      }}
                    >
                      {tag}
                      <X
                        className="w-3 h-3 cursor-pointer hover:opacity-70 transition-opacity ml-1"
                        onClick={(e) => {
                          e.stopPropagation();
                          removeTagAtIndex(index);
                        }}
                        aria-label={`Remove ${tag} tag`}
                      />
                    </Badge>
                  ))}
                  <Input
                    ref={inputRef}
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                    onKeyDown={handleInputKeyDown}
                    placeholder={tempSelectedTags.length === 0 ? "Type a tag name and press Enter..." : ""}
                    className="border-0 shadow-none p-0 h-7 min-w-[150px] flex-1 focus-visible:ring-0 focus-visible:ring-offset-0"
                    autoFocus
                  />
                </div>
              </div>
              {editingIndex !== null && (
                <p className="text-xs text-muted-foreground">
                  Editing tag - Press Enter to save, Escape to cancel
                </p>
              )}
              <p className="text-xs text-muted-foreground">
                Enter to add • Backspace to edit • Click to edit
              </p>
            </div>
          </div>
          <DialogFooter className="pt-4 border-t">
            <Button 
              variant="outline" 
              onClick={() => setDialogOpen(false)}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button 
              onClick={saveTags} 
              disabled={loading}
              className={`flex-1 ${inputValue.trim() ? 'bg-orange-500 hover:bg-orange-600' : ''}`}
            >
              {loading ? (
                <>
                  <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin mr-2" />
                  Saving...
                </>
              ) : inputValue.trim() ? (
                <>
                  Save Changes
                  <span className="ml-1 text-xs opacity-75">(unsaved text!)</span>
                </>
              ) : (
                'Save Changes'
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}