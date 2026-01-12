'use client';

import { useState } from 'react';
import { Check, ChevronsUpDown, TrendingUp, X } from 'lucide-react';
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

export interface VolumeRange {
  id: string;
  label: string;
  minSent: number;
  maxSent?: number; // undefined means no upper limit
}

const VOLUME_RANGES: VolumeRange[] = [
  { id: 'all', label: 'All volumes', minSent: 0 },
  { id: '10+', label: '10+ emails', minSent: 10 },
  { id: '25+', label: '25+ emails', minSent: 25 },
  { id: '50+', label: '50+ emails', minSent: 50 },
  { id: '100+', label: '100+ emails', minSent: 100 },
  { id: '250+', label: '250+ emails', minSent: 250 },
  { id: '500+', label: '500+ emails', minSent: 500 },
  { id: '1k+', label: '1k+ emails', minSent: 1000 },
  { id: '2.5k+', label: '2.5k+ emails', minSent: 2500 },
  { id: '5k+', label: '5k+ emails', minSent: 5000 },
  { id: '10k+', label: '10k+ emails', minSent: 10000 },
];

interface VolumeFilterProps {
  selectedVolumeRange: string | null;
  onSelectionChange: (volumeRangeId: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  variant?: 'default' | 'compact';
}

export function VolumeFilter({
  selectedVolumeRange,
  onSelectionChange,
  placeholder = "All volumes",
  disabled = false,
  className,
  variant = 'default',
}: VolumeFilterProps) {
  const [open, setOpen] = useState(false);

  const isCompact = variant === 'compact';

  const handleRangeSelect = (rangeId: string) => {
    if (rangeId === 'all') {
      // If "All volumes" is selected, clear selection
      onSelectionChange(null);
      setOpen(false);
      return;
    }

    // Single selection - either select this one or clear if already selected
    const newSelection = selectedVolumeRange === rangeId ? null : rangeId;
    onSelectionChange(newSelection);
    setOpen(false);
  };

  const handleClearAll = () => {
    onSelectionChange(null);
    setOpen(false);
  };

  const getDisplayText = () => {
    if (!selectedVolumeRange) {
      return placeholder;
    }
    
    const range = VOLUME_RANGES.find(r => r.id === selectedVolumeRange);
    return range?.label || placeholder;
  };

  const selectedRange = selectedVolumeRange 
    ? VOLUME_RANGES.find(range => range.id === selectedVolumeRange)
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
              selectedVolumeRange ? "bg-primary/10" : ""
            )}
            disabled={disabled}
          >
            <div className="flex items-center space-x-2 min-w-0">
              <TrendingUp className="h-4 w-4 flex-shrink-0 text-muted-foreground" />
              <span className={cn(
                "truncate",
                selectedVolumeRange ? "text-primary font-medium" : "text-muted-foreground"
              )}>
                {getDisplayText()}
              </span>
            </div>
            <div className="flex items-center space-x-1 ml-2 flex-shrink-0">
              <ChevronsUpDown className="h-4 w-4 text-muted-foreground" />
            </div>
            {selectedVolumeRange && (
              <div className="absolute -top-1 -right-1 w-2 h-2 bg-primary rounded-full" />
            )}
          </Button>
        </PopoverTrigger>
        
        <PopoverContent className="w-64 p-0" align="start">
          <div className="flex items-center justify-between p-3 border-b">
            <div className="flex items-center space-x-2">
              <TrendingUp className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium text-sm">Filter by Sent</span>
            </div>
          </div>

          <div className="max-h-72 overflow-y-auto">
            {VOLUME_RANGES.map((range) => {
              const isSelected = selectedVolumeRange === range.id;
              const isAllVolumes = range.id === 'all';
              const isAllSelected = !selectedVolumeRange;
              
              return (
                <div
                  key={range.id}
                  className={cn(
                    "flex items-center space-x-3 px-3 py-2 hover:bg-accent cursor-pointer transition-colors",
                    (isAllVolumes && isAllSelected) || (!isAllVolumes && isSelected) ? "bg-accent" : ""
                  )}
                  onClick={() => handleRangeSelect(range.id)}
                >
                  <Checkbox
                    checked={(isAllVolumes && isAllSelected) || (!isAllVolumes && isSelected)}
                    onChange={() => {}} // Handled by the div click
                    className="flex-shrink-0"
                  />
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium">
                      {range.label}
                    </div>
                    {range.id !== 'all' && (
                      <div className="text-xs text-muted-foreground">
                        {range.minSent.toLocaleString()}+ emails sent
                      </div>
                    )}
                  </div>
                  {((isAllVolumes && isAllSelected) || (!isAllVolumes && isSelected)) && (
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