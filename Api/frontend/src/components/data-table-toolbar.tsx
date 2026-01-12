'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { Search, UserPlus, X, RotateCcw, XCircle, ChevronDown, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { CustomizeColumns } from '@/components/customize-columns';
import { ColumnDefinition, SortConfig, MultiSortConfig } from '@/types';
import { Separator } from '@/components/ui/separator';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface DataTableToolbarProps {
  searchPlaceholder: string;
  searchQuery: string;
  onSearch: (query: string) => void;
  columns: Record<string, ColumnDefinition>;
  visibleColumns: Set<string>;
  onColumnToggle: (key: string) => void;
  onResetColumns: () => void;
  onResetWidths?: () => void;
  selectedCount: number;
  totalCount: number;
  originalTotalCount?: number;
  itemLabel: string; // "accounts" | "campaigns"
  onBulkAssign?: () => void;
  onBulkDelete?: () => void;
  onClearSelection?: () => void;
  showBulkAssign?: boolean;
  showBulkDelete?: boolean;
  sortConfig?: SortConfig;
  multiSort?: MultiSortConfig;
  defaultSort?: SortConfig;
  onResetSort?: () => void;
  onClearAllSorts?: () => void;
  customActions?: React.ReactNode;
  customColumnAction?: React.ReactNode; // Custom column customization action (replaces default dropdown)
  onDownload?: () => void;
}

export function DataTableToolbar({
  searchPlaceholder,
  searchQuery,
  onSearch,
  columns,
  visibleColumns,
  onColumnToggle,
  onResetColumns,
  onResetWidths,
  selectedCount,
  totalCount,
  originalTotalCount,
  itemLabel,
  onBulkAssign,
  onBulkDelete,
  onClearSelection,
  showBulkAssign = true,
  showBulkDelete = false,
  sortConfig,
  multiSort,
  defaultSort,
  onResetSort,
  onClearAllSorts,
  customActions,
  customColumnAction,
  onDownload,
}: DataTableToolbarProps) {
  const [localSearchQuery, setLocalSearchQuery] = useState(searchQuery);

  // Sync local state with prop changes (e.g., when parent resets search)
  useEffect(() => {
    setLocalSearchQuery(searchQuery);
  }, [searchQuery]);

  // Debounced search effect
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (localSearchQuery !== searchQuery) {
        onSearch(localSearchQuery);
      }
    }, 500); // 500ms debounce

    return () => clearTimeout(timeoutId);
  }, [localSearchQuery, searchQuery, onSearch]);

  // Check if sort is different from default
  const isSortChanged = sortConfig && defaultSort && (
    sortConfig.column !== defaultSort.column ||
    sortConfig.direction !== defaultSort.direction ||
    sortConfig.mode !== defaultSort.mode
  );

  // Check if there are multiple sorts
  const hasMultipleSorts = multiSort && multiSort.sorts.length > 1;

  return (
    <TooltipProvider>
      <div className="border-b border-border/50 bg-card/50 backdrop-blur-sm">
      <div className="p-2 sm:p-3">
        <div className="flex flex-col gap-3">
          {/* Mobile: Search bar - always on top */}
          <div className="sm:hidden">
            <div className="relative w-full">
              <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={searchPlaceholder}
                value={localSearchQuery}
                onChange={(e) => setLocalSearchQuery(e.target.value)}
                className="w-full pl-8 h-10 text-base"
              />
            </div>
          </div>

          {/* All Controls Row */}
          <div className="flex flex-col sm:flex-row sm:items-center gap-3 sm:justify-between">
            {selectedCount > 0 ? (
              <div className="flex items-center justify-between sm:justify-start gap-2 order-2 sm:order-1">
                <div className="flex items-center gap-2 px-3 py-2 bg-primary/10 text-primary rounded-md text-sm font-medium">
                  <div className="h-1.5 w-1.5 rounded-full bg-primary" />
                  {selectedCount} selected
                </div>
                
                <div className="flex items-center gap-2">
                  {showBulkAssign && onBulkAssign && (
                    <Button size="sm" onClick={onBulkAssign} className="h-10 sm:h-9">
                      <UserPlus className="h-4 w-4 mr-2" />
                      Assign
                    </Button>
                  )}
                  {showBulkDelete && onBulkDelete && (
                    <Button size="sm" variant="destructive" onClick={onBulkDelete} className="h-10 sm:h-9">
                      Delete
                    </Button>
                  )}
                  {onClearSelection && (
                    <Button size="sm" variant="ghost" onClick={onClearSelection} className="h-10 sm:h-9 w-10 sm:w-9 p-0">
                      <X className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>
            ) : (
              <>
                {/* Desktop search - order 3 (rightmost) */}
                <div className="relative hidden sm:block flex-shrink-0 order-3">
                  <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder={searchPlaceholder}
                    value={localSearchQuery}
                    onChange={(e) => setLocalSearchQuery(e.target.value)}
                    className="w-[300px] lg:w-[384px] pl-8 h-9"
                  />
                </div>
                
                {/* Controls group - order 1 (left side) */}
                <div className="flex items-center gap-0.5 flex-wrap order-1">
                  {/* Custom Actions */}
                  {customActions && customActions}
                  
                  {/* Column Controls */}
                  {customColumnAction ? (
                    customColumnAction
                  ) : (
                    <CustomizeColumns
                      columns={columns}
                      visibleColumns={visibleColumns}
                      onColumnToggle={onColumnToggle}
                      onReset={onResetColumns}
                      onResetWidths={onResetWidths}
                    />
                  )}
                  
                  {/* Sort Controls */}
                  {isSortChanged && onResetSort && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={onResetSort}
                          className="h-10 sm:h-9 w-10 sm:w-auto p-0 sm:px-3 sm:gap-2 bg-background flex-shrink-0"
                        >
                          <RotateCcw className="w-4 h-4" />
                          <span className="hidden sm:inline">Reset Sort</span>
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Reset Sort</TooltipContent>
                    </Tooltip>
                  )}
                  
                  {hasMultipleSorts && onClearAllSorts && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={onClearAllSorts}
                          className="h-10 sm:h-9 w-10 sm:w-auto p-0 sm:px-3 sm:gap-2 bg-background flex-shrink-0"
                        >
                          <XCircle className="w-4 h-4" />
                          <span className="hidden sm:inline">Clear All ({multiSort.sorts.length})</span>
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Clear All ({multiSort.sorts.length})</TooltipContent>
                    </Tooltip>
                  )}

                  {/* Download Button */}
                  {onDownload && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={onDownload}
                          className="h-10 sm:h-9 w-10 sm:w-auto p-0 sm:px-3 sm:gap-2 bg-background flex-shrink-0"
                        >
                          <Download className="w-4 h-4" />
                          <span className="hidden sm:inline">Download</span>
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Download</TooltipContent>
                    </Tooltip>
                  )}
                </div>
              </>
            )}
          </div>
        </div>
      </div>
      </div>
    </TooltipProvider>
  );
}