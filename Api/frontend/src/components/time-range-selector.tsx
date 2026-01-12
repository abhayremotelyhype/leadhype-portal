'use client';

import { useState } from 'react';
import { Clock, ChevronDown } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

export interface TimeRangeOption {
  value: string;
  label: string;
  days: number | null;
}

interface TimeRangeSelectorProps {
  selectedTimeRange: TimeRangeOption;
  onTimeRangeChange: (option: TimeRangeOption) => void;
  className?: string;
  storageKey?: string;
  description?: string;
  examples?: string[];
}

const defaultTimeRangeOptions: TimeRangeOption[] = [
  { value: '24h', label: '24 Hours', days: 1 },
  { value: '7d', label: '7 Days', days: 7 },
  { value: '30d', label: '30 Days', days: 30 },
  { value: '3m', label: '3 Months', days: 90 },
  { value: '6m', label: '6 Months', days: 180 },
  { value: '1y', label: '1 Year', days: 365 },
  { value: 'all', label: 'All Time', days: 9999 },
  { value: 'custom', label: 'Custom Range', days: null },
];

export function TimeRangeSelector({ 
  selectedTimeRange, 
  onTimeRangeChange, 
  className = '',
  description = 'Choose the time range for displaying recent statistics in columns',
  examples = ['2 weeks', '6 months', '1 year']
}: TimeRangeSelectorProps) {
  const [showModal, setShowModal] = useState(false);
  const [customDays, setCustomDays] = useState('');
  const [customUnit, setCustomUnit] = useState<'days' | 'weeks' | 'months' | 'years'>('days');

  const convertToDays = (value: number, unit: string): number => {
    switch (unit) {
      case 'days': return value;
      case 'weeks': return value * 7;
      case 'months': return value * 30; // Average month
      case 'years': return value * 365; // Average year
      default: return value;
    }
  };

  const getMaxValueForUnit = (unit: string): number => {
    switch (unit) {
      case 'days': return 3650; // ~10 years
      case 'weeks': return 520; // ~10 years
      case 'months': return 120; // 10 years
      case 'years': return 10; // 10 years max
      default: return 365;
    }
  };

  const handleTimeRangeChange = (option: TimeRangeOption | null) => {
    if (!option) return;
    
    if (option.value === 'custom') {
      // Keep modal open for custom input
      return;
    }
    
    onTimeRangeChange(option);
    setShowModal(false);
  };

  const handleCustomDaysSubmit = () => {
    const inputValue = parseInt(customDays);
    const maxValue = getMaxValueForUnit(customUnit);
    
    if (inputValue > 0 && inputValue <= maxValue) {
      const totalDays = convertToDays(inputValue, customUnit);
      const unitLabel = inputValue === 1 
        ? customUnit.slice(0, -1) // Remove 's' for singular
        : customUnit;
      
      const customOption: TimeRangeOption = {
        value: 'custom',
        label: `${inputValue} ${unitLabel}`,
        days: totalDays
      };
      
      onTimeRangeChange(customOption);
      setShowModal(false);
      setCustomDays('');
      setCustomUnit('days');
    }
  };

  const getTimeLabel = (days: number | null): string => {
    if (days === null) return 'Custom';
    if (days === 1) return '24h';
    if (days === 9999) return 'All Time';
    return `${days}d`;
  };

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            className={cn(
              "h-9 w-9 sm:w-auto p-0 sm:px-3 sm:gap-2 relative",
              selectedTimeRange.value !== 'all' ? "bg-primary/10" : "bg-background",
              className
            )}
            title={selectedTimeRange.label}
          >
            <Clock className="w-4 h-4" />
            <span className={cn(
              "hidden sm:inline",
              selectedTimeRange.value !== 'all' ? "text-primary font-medium" : "text-muted-foreground"
            )}>
              {selectedTimeRange.label}
            </span>
            <ChevronDown className="w-3 h-3 hidden sm:inline" />
            {selectedTimeRange.value !== 'all' && (
              <div className="absolute -top-1 -right-1 w-2 h-2 bg-primary rounded-full" />
            )}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-44">
          {defaultTimeRangeOptions.filter(option => option.value !== 'custom').map((option) => (
            <DropdownMenuItem
              key={option.value}
              onClick={() => handleTimeRangeChange(option)}
              className={`text-sm ${selectedTimeRange.value === option.value ? "bg-accent font-medium" : ""}`}
            >
              {option.label}
            </DropdownMenuItem>
          ))}
          <DropdownMenuSeparator />
          <DropdownMenuItem
            onClick={() => setShowModal(true)}
            className="text-sm"
          >
            Custom Range...
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog open={showModal} onOpenChange={setShowModal}>
        <DialogContent className="w-[95vw] max-w-sm p-4">
          <DialogHeader className="pb-2">
            <DialogTitle className="text-base font-semibold">Custom Time Range</DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground">
              Enter a custom time period for displaying statistics
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-4">
            <div className="space-y-2">
              <Label className="text-xs font-medium">Custom Time Range</Label>
              <div className="flex gap-2">
                <Input
                  type="number"
                  placeholder="Enter value"
                  value={customDays}
                  onChange={(e) => setCustomDays(e.target.value)}
                  className="h-8 w-20"
                  min="1"
                  max={getMaxValueForUnit(customUnit)}
                />
                <Select 
                  value={customUnit} 
                  onValueChange={(value) => {
                    if (value && ['days', 'weeks', 'months', 'years'].includes(value)) {
                      setCustomUnit(value as 'days' | 'weeks' | 'months' | 'years');
                      setCustomDays('');
                    }
                  }}
                >
                  <SelectTrigger className="h-8 w-24">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="days">Days</SelectItem>
                    <SelectItem value="weeks">Weeks</SelectItem>
                    <SelectItem value="months">Months</SelectItem>
                    <SelectItem value="years">Years</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <p className="text-xs text-muted-foreground">
                Enter 1-{getMaxValueForUnit(customUnit)} {customUnit} (e.g., {examples.join(', ')})
              </p>
            </div>
          </div>

          <DialogFooter className="pt-2 gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowModal(false)}
              className="text-xs h-8 px-3"
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleCustomDaysSubmit}
              disabled={!customDays || parseInt(customDays) <= 0 || parseInt(customDays) > getMaxValueForUnit(customUnit)}
              className="h-8 px-3"
            >
              Apply
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

export { defaultTimeRangeOptions };