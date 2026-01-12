'use client';

import { useState, useCallback, useEffect } from 'react';
import { SortConfig, TableState, MultiSortConfig, MultiSortItem } from '@/types';

interface UseDataTableProps {
  storageKey: string;
  defaultPageSize: number;
  defaultSort: SortConfig;
}

export function useDataTable({ storageKey, defaultPageSize, defaultSort }: UseDataTableProps) {
  const [tableState, setTableState] = useState<TableState>({
    currentPage: 1,
    pageSize: defaultPageSize,
    totalPages: 1,
    totalCount: 0,
    searchQuery: '',
    sort: defaultSort,
    multiSort: { sorts: [defaultSort] }, // Initialize multiSort with default sort for primary sort consistency
    selectedItems: new Set(),
  });

  // Initialize page size from localStorage after hydration
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const savedPageSize = localStorage.getItem(`${storageKey}-page-size`);
      if (savedPageSize) {
        const pageSize = parseInt(savedPageSize);
        if (pageSize && pageSize !== tableState.pageSize) {
          setTableState(prev => ({ ...prev, pageSize }));
        }
      }
    }
  }, [storageKey, tableState.pageSize]);

  // Handlers
  const handleSort = useCallback((column: string, event?: React.MouseEvent & { sortMode?: string }) => {
    const isShiftKey = event?.shiftKey || false;
    const forcedMode = (event as any)?.sortMode; // Extract the forced sort mode
    
    setTableState(prev => {
      const existingSortIndex = prev.multiSort.sorts.findIndex(s => s.column === column);
      const hasExistingSort = existingSortIndex >= 0;
      
      // Check if this is the first user-initiated sort interaction and we only have the default sort
      const hasOnlyDefaultSort = prev.multiSort.sorts.length === 1 && 
        prev.multiSort.sorts[0].column === defaultSort.column &&
        prev.multiSort.sorts[0].direction === defaultSort.direction &&
        prev.multiSort.sorts[0].mode === defaultSort.mode;
      
      // Debug logging removed - uncomment if needed for troubleshooting
      // console.log('Sort Debug:', { 
      //   column, 
      //   hasExistingSort, 
      //   existingSort: hasExistingSort ? prev.multiSort.sorts[existingSortIndex] : null,
      //   isShiftKey,
      //   hasOnlyDefaultSort,
      //   currentSorts: prev.multiSort.sorts
      // });
      
      // If Shift is not held, replace all sorts with this one
      if (!isShiftKey) {
        if (hasExistingSort) {
          const existingSort = prev.multiSort.sorts[existingSortIndex];
          
          // If a specific mode is forced (from dual sort header buttons)
          if (forcedMode) {
            console.log('Forced mode detected:', forcedMode, 'Current mode:', existingSort.mode);
            if (existingSort.mode === forcedMode) {
              // Same mode - just toggle direction
              const newDirection = existingSort.direction === 'desc' ? 'asc' : 'desc';
              console.log('Same mode - toggling direction to:', newDirection);
              return {
                ...prev,
                sort: { ...existingSort, direction: newDirection },
                multiSort: { sorts: [{ ...existingSort, direction: newDirection }] },
                currentPage: 1,
              };
            } else {
              // Different mode - switch mode and start with desc
              console.log('Different mode - switching from', existingSort.mode, 'to', forcedMode);
              return {
                ...prev,
                sort: { ...existingSort, mode: forcedMode, direction: 'desc' },
                multiSort: { sorts: [{ ...existingSort, mode: forcedMode, direction: 'desc' }] },
                currentPage: 1,
              };
            }
          }
          
          // Normal cycling logic (when no mode is forced)
          // Cycle within current mode: desc → asc → (remove/reset)
          // Mode switching only happens via the mode toggle button
          if (existingSort.direction === 'desc') {
            // Switch to ascending (same mode)
            return {
              ...prev,
              sort: { ...existingSort, direction: 'asc' },
              multiSort: { sorts: [{ ...existingSort, direction: 'asc' }] },
            };
          } else {
            // Reset to same mode descending (3rd click removes sort)
            return {
              ...prev,
              sort: { column, direction: 'desc', mode: existingSort.mode },
              multiSort: { sorts: [{ column, direction: 'desc', mode: existingSort.mode }] },
            };
          }
        } else {
          // New sort, replace all
          const mode: 'count' | 'percentage' = (forcedMode as 'count' | 'percentage') || 'count';
          const newSort = { column, direction: 'desc' as const, mode };
          return {
            ...prev,
            sort: newSort,
            multiSort: { sorts: [newSort] },
          };
        }
      }
      
      // Shift is held - add or modify as secondary sort
      let newSorts = [...prev.multiSort.sorts];
      
      if (hasExistingSort) {
        // Cycle through the existing sort
        const existingSort = newSorts[existingSortIndex];
        
        // Cycle within current mode: desc → asc → remove
        // Mode switching only happens via the mode toggle button
        if (existingSort.direction === 'desc') {
          newSorts[existingSortIndex] = { ...existingSort, direction: 'asc' };
        } else {
          // Remove this sort after completing the cycle
          newSorts.splice(existingSortIndex, 1);
        }
      } else {
        // Special case: If we only have the default sort and user shift+clicks a different column,
        // clear the default sort and make this the primary sort (position 1)
        if (hasOnlyDefaultSort && column !== defaultSort.column) {
          newSorts = [{ column, direction: 'desc', mode: 'count' }];
        } else {
          // Add new sort (limit to 3 sorts)
          if (newSorts.length < 3) {
            newSorts.push({ column, direction: 'desc', mode: 'count' });
          }
        }
      }
      
      // Update primary sort to match first in multi-sort
      const primarySort = newSorts[0] || defaultSort;
      
      return {
        ...prev,
        sort: primarySort,
        multiSort: { sorts: newSorts },
        currentPage: 1, // Reset to first page when sorting changes
      };
    });
  }, [defaultSort]);

  const handleSearch = useCallback((query: string) => {
    setTableState(prev => ({
      ...prev,
      searchQuery: query,
      currentPage: 1,
    }));
  }, []);

  const handlePageChange = useCallback((page: number) => {
    setTableState(prev => ({ ...prev, currentPage: page }));
  }, []);

  const handlePageSizeChange = useCallback((newPageSize: number) => {
    if (typeof window !== 'undefined') {
      localStorage.setItem(`${storageKey}-page-size`, newPageSize.toString());
    }
    setTableState(prev => ({
      ...prev,
      pageSize: newPageSize,
      currentPage: 1,
    }));
  }, [storageKey]);

  const handleSelectAll = useCallback((checked: boolean) => {
    setTableState(prev => ({
      ...prev,
      selectedItems: checked ? new Set() : new Set(), // Will be populated by parent
    }));
  }, []);

  const handleSelectOne = useCallback((id: string, checked: boolean) => {
    setTableState(prev => {
      const newSelected = new Set(prev.selectedItems);
      if (checked) {
        newSelected.add(id);
      } else {
        newSelected.delete(id);
      }
      return { ...prev, selectedItems: newSelected };
    });
  }, []);

  const updateTableData = useCallback((data: { totalPages: number; totalCount: number }) => {
    setTableState(prev => ({
      ...prev,
      totalPages: data.totalPages,
      totalCount: data.totalCount,
    }));
  }, []);

  const clearSelection = useCallback(() => {
    setTableState(prev => ({ ...prev, selectedItems: new Set() }));
  }, []);

  const resetSort = useCallback(() => {
    setTableState(prev => ({ 
      ...prev, 
      sort: defaultSort,
      multiSort: { sorts: [defaultSort] }
    }));
  }, [defaultSort]);

  const clearAllSorts = useCallback(() => {
    setTableState(prev => ({ 
      ...prev, 
      sort: defaultSort,
      multiSort: { sorts: [] },
      currentPage: 1
    }));
  }, [defaultSort]);

  return {
    tableState,
    setTableState,
    handleSort,
    handleSearch,
    handlePageChange,
    handlePageSizeChange,
    handleSelectAll,
    handleSelectOne,
    updateTableData,
    clearSelection,
    resetSort,
    clearAllSorts,
  };
}