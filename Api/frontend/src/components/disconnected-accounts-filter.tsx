'use client';

import { useState } from 'react';
import { Check, ChevronsUpDown, UserX, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/lib/utils';

interface DisconnectedAccountsFilterProps {
  showDisconnected: boolean;
  onToggle: (showDisconnected: boolean) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  variant?: 'default' | 'compact';
}

export function DisconnectedAccountsFilter({
  showDisconnected,
  onToggle,
  placeholder = "All accounts",
  disabled = false,
  className,
  variant = 'default',
}: DisconnectedAccountsFilterProps) {
  const [open, setOpen] = useState(false);

  const isCompact = variant === 'compact';

  const handleToggleDisconnected = () => {
    onToggle(!showDisconnected);
  };

  const getDisplayText = () => {
    if (showDisconnected) {
      return "Disconnected only";
    }
    return placeholder;
  };

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
              showDisconnected ? "bg-primary/10" : ""
            )}
            disabled={disabled}
          >
            <div className="flex items-center space-x-2 min-w-0">
              <UserX className="h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <span className={cn(
                "truncate",
                showDisconnected ? "text-primary font-medium" : "text-muted-foreground"
              )}>
                {getDisplayText()}
              </span>
            </div>
            <div className="flex items-center space-x-1 ml-2 flex-shrink-0">
              <ChevronsUpDown className="h-4 w-4 text-muted-foreground" />
            </div>
            {showDisconnected && (
              <div className="absolute -top-1 -right-1 w-2 h-2 bg-primary rounded-full" />
            )}
          </Button>
        </PopoverTrigger>
        
        <PopoverContent className="w-64 p-0" align="start">
          <div className="flex items-center justify-between p-3 border-b">
            <div className="flex items-center space-x-2">
              <UserX className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium text-sm">Filter by Connection</span>
            </div>
            {showDisconnected && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onToggle(false)}
                className="h-6 px-2 text-xs"
              >
                <X className="h-3 w-3 mr-1" />
                Clear
              </Button>
            )}
          </div>

          <div className="p-3">
            <div
              className={cn(
                "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors rounded-md",
                showDisconnected ? "bg-accent" : ""
              )}
              onClick={handleToggleDisconnected}
            >
              <Checkbox
                checked={showDisconnected}
                onChange={() => {}} // Handled by the div click
                className="flex-shrink-0"
              />
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium">
                  Disconnected Accounts
                </div>
                <div className="text-xs text-muted-foreground">
                  Show only accounts with inactive warmup status
                </div>
              </div>
              {showDisconnected && (
                <Check className="h-4 w-4 text-primary flex-shrink-0" />
              )}
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  );
}