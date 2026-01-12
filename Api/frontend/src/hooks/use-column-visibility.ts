'use client';

import { useState, useEffect, useCallback } from 'react';
import { ColumnDefinition } from '@/types';

interface UseColumnVisibilityProps {
  columns: Record<string, ColumnDefinition>;
  storageKey: string;
  orderStorageKey?: string; // Optional: for storing column order
}

export function useColumnVisibility({ columns, storageKey, orderStorageKey }: UseColumnVisibilityProps) {
  // Initialize visible columns with all columns that are either required or don't specify required
  const getDefaultVisibleColumns = useCallback(() => {
    return new Set(
      Object.entries(columns)
        .filter(([_, column]) => column.required !== false)
        .map(([key]) => key)
    );
  }, [columns]);

  const [visibleColumns, setVisibleColumns] = useState<Set<string>>(getDefaultVisibleColumns);
  const [isInitialized, setIsInitialized] = useState(false);
  
  // Column order state
  const getDefaultColumnOrder = useCallback(() => {
    return Object.keys(columns);
  }, [columns]);
  
  const [columnOrder, setColumnOrder] = useState<string[]>(getDefaultColumnOrder);

  // Load from localStorage on mount
  useEffect(() => {
    try {
      // Load column visibility
      const saved = localStorage.getItem(storageKey);
      if (saved) {
        const savedColumns = JSON.parse(saved) as string[];
        // Ensure required columns are always included
        const requiredColumns = Object.entries(columns)
          .filter(([_, column]) => column.required === true)
          .map(([key]) => key);
        
        const columnsSet = new Set([...savedColumns, ...requiredColumns]);
        setVisibleColumns(columnsSet);
      }
      
      // Load column order if orderStorageKey is provided
      if (orderStorageKey) {
        const savedOrder = localStorage.getItem(orderStorageKey);
        if (savedOrder) {
          const orderArray = JSON.parse(savedOrder) as string[];
          // Validate that all columns exist
          const validOrder = orderArray.filter(key => columns[key]);
          // Add any missing columns to the end
          const missingColumns = Object.keys(columns).filter(key => !validOrder.includes(key));
          setColumnOrder([...validOrder, ...missingColumns]);
        }
      }
    } catch (error) {
      console.warn('Failed to load column settings from localStorage:', error);
    } finally {
      setIsInitialized(true);
    }
  }, [columns, storageKey, orderStorageKey]);

  // Save visibility to localStorage when columns change
  useEffect(() => {
    if (!isInitialized) return;
    
    try {
      localStorage.setItem(storageKey, JSON.stringify(Array.from(visibleColumns)));
    } catch (error) {
      console.warn('Failed to save column visibility to localStorage:', error);
    }
  }, [visibleColumns, storageKey, isInitialized]);
  
  // Save order to localStorage when order changes
  useEffect(() => {
    if (!isInitialized || !orderStorageKey) return;
    
    try {
      localStorage.setItem(orderStorageKey, JSON.stringify(columnOrder));
    } catch (error) {
      console.warn('Failed to save column order to localStorage:', error);
    }
  }, [columnOrder, orderStorageKey, isInitialized]);

  const toggleColumn = useCallback((columnKey: string) => {
    const column = columns[columnKey];
    if (column?.required === true) {
      return; // Don't allow toggling required columns
    }

    setVisibleColumns(prev => {
      const newSet = new Set(prev);
      if (newSet.has(columnKey)) {
        newSet.delete(columnKey);
      } else {
        newSet.add(columnKey);
      }
      return newSet;
    });
  }, [columns]);

  const resetColumns = useCallback(() => {
    setVisibleColumns(getDefaultVisibleColumns());
  }, [getDefaultVisibleColumns]);
  
  const resetColumnOrder = useCallback(() => {
    setColumnOrder(getDefaultColumnOrder());
  }, [getDefaultColumnOrder]);
  
  const reorderColumns = useCallback((newOrder: string[]) => {
    setColumnOrder(newOrder);
  }, []);

  const isColumnVisible = useCallback((columnKey: string) => {
    return visibleColumns.has(columnKey);
  }, [visibleColumns]);
  
  // Get ordered visible columns for rendering
  const getOrderedVisibleColumns = useCallback(() => {
    if (!orderStorageKey) {
      // If no ordering is enabled, return original order
      return Object.entries(columns).filter(([key]) => visibleColumns.has(key));
    }
    
    // Return columns in the specified order, filtering only visible ones
    return columnOrder
      .filter(key => columns[key] && visibleColumns.has(key))
      .map(key => [key, columns[key]] as [string, ColumnDefinition]);
  }, [columns, visibleColumns, columnOrder, orderStorageKey]);

  return {
    visibleColumns,
    columnOrder: orderStorageKey ? columnOrder : undefined,
    toggleColumn,
    resetColumns,
    resetColumnOrder: orderStorageKey ? resetColumnOrder : undefined,
    reorderColumns: orderStorageKey ? reorderColumns : undefined,
    isColumnVisible,
    getOrderedVisibleColumns,
    isInitialized,
  };
}