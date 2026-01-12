'use client';

import React, { useState, useEffect } from 'react';
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { DualSortHeader } from '@/components/dual-sort-header';
import { ResizableHeader } from '@/components/resizable-header';
import { ColumnDefinition, SortConfig, MultiSortConfig } from '@/types';
import { WifiOff, RefreshCw, AlertCircle } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';

interface DataTableProps<T = any> {
  data: T[];
  columns: Record<string, ColumnDefinition>;
  visibleColumns: Set<string>;
  orderedVisibleColumns?: [string, ColumnDefinition][]; // Optional: pre-ordered visible columns
  loading: boolean;
  error?: string;
  selectedItems: Set<string>;
  sortConfig?: SortConfig; // Keep for backward compatibility
  multiSort?: MultiSortConfig;
  onSelectAll: (checked: boolean) => void;
  onSelectOne: (id: string, checked: boolean) => void;
  onSort: (column: string, event?: React.MouseEvent) => void;
  onRetry?: () => void;
  renderCell: (item: T, columnKey: string) => React.ReactNode;
  emptyMessage: string;
  emptyDescription?: string;
  emptyAction?: React.ReactNode;
  getId: (item: T) => string;
  getColumnStyle?: (columnKey: string) => React.CSSProperties;
  onColumnResize?: (e: React.MouseEvent, columnKey: string) => void;
}

export function DataTable<T>({
  data,
  columns,
  visibleColumns,
  orderedVisibleColumns,
  loading,
  error,
  selectedItems,
  sortConfig,
  multiSort,
  onSelectAll,
  onSelectOne,
  onSort,
  onRetry,
  renderCell,
  emptyMessage,
  emptyDescription,
  emptyAction,
  getId,
  getColumnStyle,
  onColumnResize,
}: DataTableProps<T>) {
  const [isDataChanging, setIsDataChanging] = useState(false);
  const [prevDataLength, setPrevDataLength] = useState(0);
  
  // Use ordered columns if provided, otherwise fall back to default filtering
  const visibleColumnEntries = orderedVisibleColumns || 
    Object.entries(columns).filter(([columnKey]) => visibleColumns.has(columnKey));

  // Performance optimization: detect large datasets
  const isLargeDataset = data.length > 100;
  const maxAnimatedRows = 50; // Only animate first 50 rows for large datasets

  // Track data changes for animations
  useEffect(() => {
    if (data.length !== prevDataLength && !loading) {
      setIsDataChanging(true);
      setPrevDataLength(data.length);
      
      // Reset animation state faster for large datasets
      const resetDelay = isLargeDataset ? 20 : 50;
      const timer = setTimeout(() => {
        setIsDataChanging(false);
      }, resetDelay);
      
      return () => clearTimeout(timer);
    }
  }, [data.length, prevDataLength, loading, isLargeDataset]);

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      // Select all items on current page
      data.forEach(item => {
        const id = getId(item);
        if (!selectedItems.has(id)) {
          onSelectOne(id, true);
        }
      });
    } else {
      // Deselect all items on current page
      data.forEach(item => {
        const id = getId(item);
        if (selectedItems.has(id)) {
          onSelectOne(id, false);
        }
      });
    }
    onSelectAll(checked);
  };

  const isAllSelected = data.length > 0 && data.every(item => selectedItems.has(getId(item)));
  const isIndeterminate = data.some(item => selectedItems.has(getId(item))) && !isAllSelected;

  if (error) {
    return (
      <div className="flex-1 overflow-x-auto overflow-y-auto p-1">
        <div className="rounded-lg border border-border/40 bg-card/50 backdrop-blur-sm shadow-sm">
          <div className="flex flex-col items-center justify-center p-12 text-center space-y-4">
            <div className="relative">
              <div className="w-16 h-16 bg-destructive/10 rounded-full flex items-center justify-center">
                <WifiOff className="w-8 h-8 text-destructive/60" />
              </div>
              <div className="absolute -top-1 -right-1 w-6 h-6 bg-destructive/20 rounded-full flex items-center justify-center">
                <AlertCircle className="w-4 h-4 text-destructive" />
              </div>
            </div>
            
            <div className="space-y-2">
              <h3 className="text-lg font-semibold text-foreground">
                {error.includes('Authentication') ? 'Authentication Error' : 
                 error.includes('Access Denied') ? 'Access Denied' : 
                 error.includes('Server Error') ? 'Server Error' : 
                 'Connection Error'}
              </h3>
              <p className="text-sm text-muted-foreground max-w-md">
                {error}
              </p>
            </div>
            
            {onRetry && (
              <Button 
                onClick={onRetry}
                variant="outline" 
                size="sm"
                className="mt-4 gap-2"
              >
                <RefreshCw className="w-4 h-4" />
                Try Again
              </Button>
            )}
            
            <div className="text-xs text-muted-foreground/60 mt-2">
              {error}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 relative min-h-[400px]">
      <div className="absolute inset-0 p-2">
        <div 
          className="h-full overflow-auto rounded-lg border border-border/40 bg-card/30 backdrop-blur-sm custom-scrollbar min-h-[350px] shadow-sm relative"
          style={{
            scrollbarWidth: 'thin',
            scrollbarColor: 'hsl(var(--muted-foreground) / 0.3) hsl(var(--background))',
          }}
        >
          {/* Subtle loading overlay for pagination with enhanced animation */}
          {loading && data.length > 0 && (
            <div className="absolute inset-0 bg-background/20 backdrop-blur-[1px] z-10 flex items-center justify-center animate-in fade-in duration-200">
              <div className="bg-card/90 backdrop-blur border rounded-lg px-3 py-2 shadow-lg animate-in slide-in-from-bottom-2 duration-300">
                <div className="flex items-center gap-2">
                  <div className="w-4 h-4 border-2 border-primary border-t-transparent rounded-full animate-spin"></div>
                  <span className="text-sm text-muted-foreground">Loading...</span>
                </div>
              </div>
            </div>
          )}
          <Table 
            className="relative w-full" 
            style={{ 
              tableLayout: 'fixed',
              ...(getColumnStyle && visibleColumnEntries.reduce((acc, [columnKey]) => {
                const style = getColumnStyle(columnKey);
                acc[`--col-${columnKey}-width`] = style.width;
                return acc;
              }, {} as Record<string, any>))
            }}
          >
            {/* Force column widths with colgroup */}
            <colgroup>
              {visibleColumnEntries.map(([columnKey]) => {
                const style = getColumnStyle ? getColumnStyle(columnKey) : {};
                return (
                  <col 
                    key={columnKey} 
                    style={{ 
                      width: style.width,
                      minWidth: style.minWidth,
                      maxWidth: style.maxWidth 
                    }} 
                  />
                );
              })}
            </colgroup>
            <TableHeader className="sticky top-0 z-[1] bg-card/95 backdrop-blur-md border-b border-border/50">
              <TableRow className="bg-card/80 hover:bg-card/90 transition-colors">
                {visibleColumnEntries.map(([columnKey, columnDef], index) => {
                  const style = getColumnStyle ? getColumnStyle(columnKey) : {};
                  return (
                    <ResizableHeader
                      key={columnKey}
                      columnKey={columnKey}
                      columnDef={columnDef}
                      sortConfig={sortConfig}
                      multiSort={multiSort}
                      onSort={onSort}
                      onMouseDown={onColumnResize ? (e, columnKey) => onColumnResize(e as React.MouseEvent, columnKey) : () => {}}
                      style={style}
                      isAllSelected={isAllSelected}
                      isIndeterminate={isIndeterminate}
                      onSelectAll={handleSelectAll}
                    />
                  );
                })}
            </TableRow>
          </TableHeader>
          <TableBody className="relative">
            {loading && data.length === 0 ? (
              // Skeleton loading rows with optimized animation
              Array.from({ length: 10 }).map((_, index) => (
                <TableRow 
                  key={`skeleton-${index}`} 
                  className="hover:bg-transparent animate-in fade-in duration-200"
                  style={{
                    animationDelay: `${index * 40}ms`,
                    animationFillMode: 'both'
                  }}
                >
                  {visibleColumnEntries.map(([columnKey, columnDef], columnIndex) => {
                    const style = getColumnStyle ? getColumnStyle(columnKey) : {};
                    return (
                      <TableCell 
                        key={`skeleton-${index}-${columnKey}`}
                        className={`${columnKey === 'select' ? 'pr-4 pl-2' : ''}`}
                        style={style}
                      >
                        <div
                          className="animate-in fade-in slide-in-from-left-1 duration-150"
                          style={{
                            animationDelay: `${(index * 40) + (columnIndex * 20)}ms`,
                            animationFillMode: 'both'
                          }}
                        >
                          {columnKey === 'select' ? (
                            <Skeleton className="h-4 w-4 mr-3 rounded animate-pulse" />
                          ) : columnKey === 'actions' ? (
                            <div className="flex items-center gap-2">
                              <Skeleton className="h-8 w-8 rounded animate-pulse" />
                              <Skeleton className="h-8 w-8 rounded animate-pulse" />
                            </div>
                          ) : columnKey.includes('email') || columnKey.includes('name') ? (
                            <Skeleton className="h-4 w-[60%] animate-pulse" />
                          ) : columnKey.includes('status') ? (
                            <Skeleton className="h-6 w-20 rounded-full animate-pulse" />
                          ) : columnKey.includes('date') || columnKey.includes('At') ? (
                            <Skeleton className="h-4 w-24 animate-pulse" />
                          ) : columnKey.includes('count') || columnKey.includes('total') ? (
                            <Skeleton className="h-4 w-12 animate-pulse" />
                          ) : (
                            <Skeleton className="h-4 w-[70%] animate-pulse" />
                          )}
                        </div>
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))
            ) : (!data || data.length === 0) ? (
              <TableRow>
                <TableCell colSpan={visibleColumns.size} className="h-32">
                  <div className="flex flex-col items-center justify-center space-y-4 text-center">
                    <div className="space-y-2">
                      <h3 className="text-base sm:text-lg font-medium text-foreground">{emptyMessage}</h3>
                      {emptyDescription && (
                        <p className="text-xs sm:text-sm text-muted-foreground max-w-sm px-4">
                          {emptyDescription}
                        </p>
                      )}
                    </div>
                    {emptyAction}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              data.map((item, index) => {
                const itemId = getId(item);
                const isSelected = selectedItems.has(itemId);
                
                // Performance optimization: skip animations for large datasets or rows beyond threshold
                const shouldAnimate = !isLargeDataset && index < maxAnimatedRows;
                const animationDelay = shouldAnimate 
                  ? (isDataChanging ? `${index * 15}ms` : `${index * 25}ms`)
                  : '0ms';
                const animationDuration = shouldAnimate ? '200ms' : '0ms';
                
                return (
                  <TableRow
                    key={itemId}
                    className={`transition-all duration-200 hover:bg-muted/30 ${
                      isSelected ? 'bg-muted/50' : ''
                    } ${
                      shouldAnimate 
                        ? (isDataChanging 
                            ? 'animate-in fade-in slide-in-from-right-2' 
                            : 'animate-in fade-in slide-in-from-bottom-1')
                        : ''
                    }`}
                    style={{
                      animationDelay,
                      animationDuration,
                      animationFillMode: shouldAnimate ? 'both' : 'none'
                    }}
                  >
                    {visibleColumnEntries.map(([columnKey], columnIndex) => {
                      const style = getColumnStyle ? getColumnStyle(columnKey) : {};
                      const cellAnimationDelay = shouldAnimate 
                        ? `${(index * 15) + (columnIndex * 10)}ms`
                        : '0ms';
                      const cellAnimationDuration = shouldAnimate ? '150ms' : '0ms';
                      
                      return (
                        <TableCell
                          key={`${itemId}-${columnKey}`}
                          className={`${columnKey === 'select' ? 'pr-4 pl-2' : ''} transition-colors duration-150`}
                          style={style}
                        >
                          <div
                            className={shouldAnimate 
                              ? (isDataChanging 
                                  ? 'animate-in fade-in slide-in-from-left-1' 
                                  : 'animate-in fade-in')
                              : ''
                            }
                            style={{
                              animationDelay: cellAnimationDelay,
                              animationDuration: cellAnimationDuration,
                              animationFillMode: shouldAnimate ? 'both' : 'none'
                            }}
                          >
                            {columnKey === 'select' ? (
                              <Checkbox
                                checked={isSelected}
                                onCheckedChange={(checked) => onSelectOne(itemId, checked as boolean)}
                                className="mr-3 transition-all duration-200"
                              />
                            ) : (
                              renderCell(item, columnKey)
                            )}
                          </div>
                        </TableCell>
                      );
                    })}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
        </div>
      </div>
    </div>
  );
}