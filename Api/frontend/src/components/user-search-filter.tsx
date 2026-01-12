'use client';

import { useState, useEffect } from 'react';
import { Check, ChevronsUpDown, User, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { UserListItem } from '@/types';
import { cn } from '@/lib/utils';

interface UserSearchFilterProps {
  selectedUserId: string | null;
  onSelectionChange: (userId: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  variant?: 'default' | 'compact';
}

export function UserSearchFilter({
  selectedUserId,
  onSelectionChange,
  placeholder = "All users",
  disabled = false,
  className,
  variant = 'default',
}: UserSearchFilterProps) {
  const [open, setOpen] = useState(false);
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isCompact = variant === 'compact';

  const loadUsers = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiClient.get<{ data: UserListItem[] }>(ENDPOINTS.userList);
      // Handle both paginated response format and direct array format
      const usersData = response.data || response;
      setUsers(Array.isArray(usersData) ? usersData : []);
    } catch (error) {
      console.error('Failed to load users:', error);
      setError('Failed to load users');
      setUsers([]); // Reset to empty array on error
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (open && users.length === 0) {
      loadUsers();
    }
  }, [open, users.length]);

  // Load users when there is a selected user ID but no users loaded yet
  useEffect(() => {
    if (selectedUserId && users.length === 0 && !loading) {
      loadUsers();
    }
  }, [selectedUserId, users.length, loading]);

  const handleUserSelect = (userId: string) => {
    if (userId === 'all') {
      // If "All users" is selected, clear selection
      onSelectionChange(null);
      setOpen(false);
      return;
    }

    // Single selection - either select this one or clear if already selected
    const newSelection = selectedUserId === userId ? null : userId;
    onSelectionChange(newSelection);
    setOpen(false);
  };

  const getDisplayText = () => {
    if (!selectedUserId) {
      return placeholder;
    }
    
    const user = users.find(u => u.id === selectedUserId);
    return user?.name || placeholder;
  };

  const selectedUser = selectedUserId 
    ? users.find(user => user.id === selectedUserId)
    : null;

  return (
    <div className={cn("flex items-center space-x-2", className)}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn(
              "justify-between font-normal relative",
              isCompact ? "h-9 px-3" : "h-10 px-4",
              selectedUserId ? "bg-blue-50 border-blue-200 text-blue-700" : ""
            )}
            disabled={disabled}
          >
            <div className="flex items-center space-x-2 min-w-0">
              <User className="h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <span className={cn(
                "truncate",
                selectedUserId ? "text-blue-700 font-medium" : "text-muted-foreground"
              )}>
                {getDisplayText()}
              </span>
            </div>
            <div className="flex items-center space-x-1 ml-2 flex-shrink-0">
              <ChevronsUpDown className="h-4 w-4 text-muted-foreground" />
            </div>
            {selectedUserId && (
              <div className="absolute -top-1 -right-1 w-2 h-2 bg-blue-500 rounded-full" />
            )}
          </Button>
        </PopoverTrigger>
        
        <PopoverContent className="w-80 p-0" align="start">
          <div className="flex items-center justify-between p-3 border-b">
            <div className="flex items-center space-x-2">
              <User className="h-4 w-4 text-blue-500" />
              <span className="font-medium text-sm">Filter by User</span>
            </div>
          </div>

          <div className="p-2">
            <div className="text-xs text-muted-foreground mb-3 px-1">
              Filter to view items created by specific users. Admin accounts are excluded from this filter.
            </div>
          </div>

          <div className="max-h-72 overflow-y-auto">
            {loading ? (
              <div className="flex items-center justify-center py-6">
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                <span className="ml-2 text-sm text-muted-foreground">Loading users...</span>
              </div>
            ) : error ? (
              <Alert variant="destructive" className="m-2">
                <AlertCircle className="h-4 w-4" />
                <AlertTitle>Error Loading Users</AlertTitle>
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            ) : (
              <>
                {/* All Users Option */}
                <div
                  className={cn(
                    "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors",
                    !selectedUserId ? "bg-accent" : ""
                  )}
                  onClick={() => handleUserSelect('all')}
                >
                  <Checkbox
                    checked={!selectedUserId}
                    onChange={() => {}} // Handled by the div click
                    className="flex-shrink-0"
                  />
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium">
                      All users
                    </div>
                    <div className="text-xs text-muted-foreground">
                      Show items from all users
                    </div>
                  </div>
                  {!selectedUserId && (
                    <Check className="h-4 w-4 text-primary flex-shrink-0" />
                  )}
                </div>
                
                {(users || []).map((user) => {
                  const isSelected = selectedUserId === user.id;
                  
                  return (
                    <div
                      key={user.id}
                      className={cn(
                        "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors",
                        isSelected ? "bg-accent" : ""
                      )}
                      onClick={() => handleUserSelect(user.id)}
                    >
                      <Checkbox
                        checked={isSelected}
                        onChange={() => {}} // Handled by the div click
                        className="flex-shrink-0"
                      />
                      <div className="flex-1 min-w-0">
                        <div className="text-sm font-medium text-blue-600">
                          {user.name}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {user.role}
                        </div>
                      </div>
                      {isSelected && (
                        <Check className="h-4 w-4 text-primary flex-shrink-0" />
                      )}
                    </div>
                  );
                })}
              </>
            )}
          </div>

        </PopoverContent>
      </Popover>
    </div>
  );
}