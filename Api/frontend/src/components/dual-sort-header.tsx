'use client';

import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';
import { cn } from '@/lib/utils';
import { MultiSortConfig } from '@/types';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface DualSortHeaderProps {
  column: string;
  label: string;
  multiSort: MultiSortConfig;
  isDualSort?: boolean;
  onSort: (column: string, event: React.MouseEvent) => void;
  className?: string;
}

export function DualSortHeader({
  column,
  label,
  multiSort,
  isDualSort = false,
  onSort,
  className,
}: DualSortHeaderProps) {
  // Find this column in the multi-sort configuration
  const sortIndex = multiSort?.sorts?.findIndex(s => s.column === column) ?? -1;
  const isActive = sortIndex >= 0;
  const sortItem = isActive ? multiSort?.sorts?.[sortIndex] : null;
  const direction = sortItem?.direction || 'asc';
  const mode = sortItem?.mode || 'count';

  const getSortIcon = () => {
    if (!isActive) {
      return (
        <ArrowUpDown className="w-3 h-3 text-muted-foreground opacity-50 group-hover:opacity-100" />
      );
    }

    const DirectionIcon = direction === 'asc' ? ArrowUp : ArrowDown;

    // Show sort priority number if this is a multi-sort scenario
    const showPriority = (multiSort?.sorts?.length ?? 0) > 1 && isActive;

    if (!isDualSort) {
      return (
        <div className="flex items-center gap-1">
          <DirectionIcon className="w-3.5 h-3.5 text-primary" />
          {showPriority && (
            <span className="text-[10px] font-bold text-primary bg-primary/10 rounded-full w-4 h-4 flex items-center justify-center">
              {sortIndex + 1}
            </span>
          )}
        </div>
      );
    }

    const modeColor = mode === 'percentage' ? 'from-purple-500 to-pink-500' : 'from-blue-500 to-cyan-500';
    const modeIcon = mode === 'percentage' ? '%' : '#';
    const nextMode = mode === 'percentage' ? 'count' : 'percentage';

    return (
      <div 
        className="flex items-center gap-1.5"
        title={`Currently sorting by ${mode === 'percentage' ? 'percentage' : 'count'} (${direction}). ${showPriority ? `Sort priority: ${sortIndex + 1}` : ''} Hold Shift and click to add as secondary sort.`}
      >
        <DirectionIcon className="w-3.5 h-3.5 text-primary" />
        {showPriority && (
          <span className="text-[10px] font-bold text-primary bg-primary/10 rounded-full w-4 h-4 flex items-center justify-center">
            {sortIndex + 1}
          </span>
        )}
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <div 
                className={cn(
                  "w-5 h-5 rounded-full bg-gradient-to-r flex items-center justify-center shadow-sm cursor-pointer hover:scale-110 transition-transform",
                  modeColor
                )}
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  const nextMode = mode === 'percentage' ? 'count' : 'percentage';
                  const event = { ...e, shiftKey: false, sortMode: nextMode } as any;
                  onSort(column, event);
                }}
              >
                <span className="text-[10px] text-white font-bold">{modeIcon}</span>
              </div>
            </TooltipTrigger>
            <TooltipContent side="bottom" sideOffset={5}>
              <p>Click to switch to {mode === 'percentage' ? 'Count' : 'Percentage'}</p>
              {showPriority && <p className="text-xs opacity-75">Priority: {sortIndex + 1}</p>}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
    );
  };

  return (
    <button
      onClick={(e) => onSort(column, e)}
      className={cn(
        "flex items-center gap-1 w-full px-1 py-2 text-left ui-table-header hover:bg-muted/30 group",
        className
      )}
      title="Click to sort. Hold Shift + Click to add as secondary sort."
    >
      <span className="group-hover:text-foreground truncate flex-1 min-w-0">
        {label}
      </span>
      <span className="flex-shrink-0">
        {getSortIcon()}
      </span>
    </button>
  );
}