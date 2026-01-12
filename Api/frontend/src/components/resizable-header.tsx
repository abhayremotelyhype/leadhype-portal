'use client';

import React from 'react';
import { DualSortHeader } from '@/components/dual-sort-header';
import { Checkbox } from '@/components/ui/checkbox';
import { ColumnDefinition, SortConfig, MultiSortConfig } from '@/types';

interface ResizableHeaderProps {
  columnKey: string;
  columnDef: ColumnDefinition;
  sortConfig?: SortConfig; // Keep for backward compatibility
  multiSort?: MultiSortConfig;
  onSort: (column: string, event?: React.MouseEvent) => void;
  onMouseDown: (e: React.MouseEvent | React.TouchEvent, columnKey: string) => void;
  style: React.CSSProperties;
  isAllSelected?: boolean;
  isIndeterminate?: boolean;
  onSelectAll?: (checked: boolean) => void;
}

export function ResizableHeader({
  columnKey,
  columnDef,
  sortConfig,
  multiSort,
  onSort,
  onMouseDown,
  style,
  isAllSelected,
  isIndeterminate,
  onSelectAll,
}: ResizableHeaderProps) {
  // Use multiSort if available, otherwise fall back to creating one from sortConfig
  const effectiveMultiSort = multiSort || (sortConfig ? { sorts: [sortConfig] } : { sorts: [] });
  
  return (
    <th 
      className="bg-background border-r last:border-r-0 relative group font-normal"
      style={style}
    >
      <div className={`${columnKey === 'select' ? 'pr-6 pl-3' : 'px-1'} py-3`}>
        {columnDef.sortable ? (
          <DualSortHeader
            column={columnKey}
            label={columnDef.label}
            multiSort={effectiveMultiSort}
            isDualSort={columnDef.dualSort}
            onSort={onSort}
          />
        ) : columnKey === 'select' ? (
          <Checkbox
            checked={isAllSelected}
            ref={(el) => {
              if (el && isIndeterminate !== undefined) {
                const inputEl = el.querySelector('input');
                if (inputEl) {
                  inputEl.indeterminate = isIndeterminate;
                }
              }
            }}
            onCheckedChange={onSelectAll}
            className="mr-4"
          />
        ) : (
          <span>{columnDef.label}</span>
        )}
      </div>
      
      {/* Resize Handle */}
      {columnKey !== 'select' && (
        <div
          className="absolute top-0 right-0 w-4 h-full cursor-col-resize bg-transparent hover:bg-primary/20 active:bg-primary/30 transition-colors z-10 touch-none"
          onMouseDown={(e) => onMouseDown(e, columnKey)}
          onTouchStart={(e) => onMouseDown(e, columnKey)}
          title="Drag to resize column"
        >
          <div className="absolute top-1/2 right-0 w-1 h-8 bg-border opacity-0 group-hover:opacity-100 active:opacity-100 transition-opacity transform -translate-y-1/2" />
        </div>
      )}
    </th>
  );
}