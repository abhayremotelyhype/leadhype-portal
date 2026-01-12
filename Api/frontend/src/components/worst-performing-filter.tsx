'use client';

import { useState } from 'react';
import { Check, ChevronsUpDown, TrendingDown, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

export interface PerformanceFilter {
  id: string;
  label: string;
  description: string;
  minSent: number;
  maxReplyRate: number; // Maximum reply rate percentage
}

const PERFORMANCE_FILTERS: PerformanceFilter[] = [
  { id: 'all', label: 'All accounts', description: 'Show all email accounts', minSent: 0, maxReplyRate: 100 },
  { id: 'poor-100', label: 'Poor performers (100+)', description: '100+ sent, <2% reply rate', minSent: 100, maxReplyRate: 2 },
  { id: 'poor-250', label: 'Poor performers (250+)', description: '250+ sent, <2% reply rate', minSent: 250, maxReplyRate: 2 },
  { id: 'poor-500', label: 'Poor performers (500+)', description: '500+ sent, <2% reply rate', minSent: 500, maxReplyRate: 2 },
  { id: 'poor-1k', label: 'Poor performers (1k+)', description: '1000+ sent, <2% reply rate', minSent: 1000, maxReplyRate: 2 },
  { id: 'worst-100', label: 'Worst performers (100+)', description: '100+ sent, <1% reply rate', minSent: 100, maxReplyRate: 1 },
  { id: 'worst-250', label: 'Worst performers (250+)', description: '250+ sent, <1% reply rate', minSent: 250, maxReplyRate: 1 },
  { id: 'worst-500', label: 'Worst performers (500+)', description: '500+ sent, <1% reply rate', minSent: 500, maxReplyRate: 1 },
  { id: 'worst-1k', label: 'Worst performers (1k+)', description: '1000+ sent, <1% reply rate', minSent: 1000, maxReplyRate: 1 },
];

interface WorstPerformingFilterProps {
  selectedFilter: string | null;
  onSelectionChange: (filterId: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  variant?: 'default' | 'compact';
}

export function WorstPerformingFilter({
  selectedFilter,
  onSelectionChange,
  placeholder = "All accounts",
  disabled = false,
  className,
  variant = 'default',
}: WorstPerformingFilterProps) {
  const [open, setOpen] = useState(false);

  const isCompact = variant === 'compact';

  const handleFilterSelect = (filterId: string) => {
    if (filterId === 'all') {
      // If "All accounts" is selected, clear selection
      onSelectionChange(null);
      setOpen(false);
      return;
    }

    // Single selection - either select this one or clear if already selected
    const newSelection = selectedFilter === filterId ? null : filterId;
    onSelectionChange(newSelection);
    setOpen(false);
  };

  const handleClearAll = () => {
    onSelectionChange(null);
    setOpen(false);
  };

  const getDisplayText = () => {
    if (!selectedFilter) {
      return placeholder;
    }
    
    const filter = PERFORMANCE_FILTERS.find(f => f.id === selectedFilter);
    return filter?.label || placeholder;
  };

  const selectedFilterObj = selectedFilter 
    ? PERFORMANCE_FILTERS.find(filter => filter.id === selectedFilter)
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
              selectedFilter ? "bg-red-50 border-red-200 text-red-700" : ""
            )}
            disabled={disabled}
          >
            <div className="flex items-center space-x-2 min-w-0">
              <TrendingDown className="h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <span className={cn(
                "truncate",
                selectedFilter ? "text-red-700 font-medium" : "text-muted-foreground"
              )}>
                {getDisplayText()}
              </span>
            </div>
            <div className="flex items-center space-x-1 ml-2 flex-shrink-0">
              <ChevronsUpDown className="h-4 w-4 text-muted-foreground" />
            </div>
            {selectedFilter && (
              <div className="absolute -top-1 -right-1 w-2 h-2 bg-red-500 rounded-full" />
            )}
          </Button>
        </PopoverTrigger>
        
        <PopoverContent className="w-80 p-0" align="start">
          <div className="flex items-center justify-between p-3 border-b">
            <div className="flex items-center space-x-2">
              <TrendingDown className="h-4 w-4 text-red-500" />
              <span className="font-medium text-sm">Worst Performing</span>
            </div>
          </div>

          <div className="p-2">
            <div className="text-xs text-muted-foreground mb-3 px-1">
              Find email accounts with high sent volumes but low reply rates - potential reputation issues or deliverability problems.
            </div>
          </div>

          <div className="max-h-72 overflow-y-auto">
            {PERFORMANCE_FILTERS.map((filter) => {
              const isSelected = selectedFilter === filter.id;
              const isAllAccounts = filter.id === 'all';
              const isAllSelected = !selectedFilter;
              
              return (
                <div
                  key={filter.id}
                  className={cn(
                    "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors",
                    (isAllAccounts && isAllSelected) || (!isAllAccounts && isSelected) ? "bg-accent" : ""
                  )}
                  onClick={() => handleFilterSelect(filter.id)}
                >
                  <Checkbox
                    checked={(isAllAccounts && isAllSelected) || (!isAllAccounts && isSelected)}
                    onChange={() => {}} // Handled by the div click
                    className="flex-shrink-0"
                  />
                  <div className="flex-1 min-w-0">
                    <div className={cn(
                      "text-sm font-medium",
                      !isAllAccounts && (filter.id.startsWith('worst-') ? "text-red-600" : "text-orange-600")
                    )}>
                      {filter.label}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {filter.description}
                    </div>
                  </div>
                  {((isAllAccounts && isAllSelected) || (!isAllAccounts && isSelected)) && (
                    <Check className="h-4 w-4 text-primary flex-shrink-0" />
                  )}
                </div>
              );
            })}
          </div>

        </PopoverContent>
      </Popover>
    </div>
  );
}