'use client';

import { useState, useCallback, useRef, useEffect } from 'react';

interface ResizableColumnOptions {
  storageKey: string;
  defaultWidths: Record<string, number>;
  minWidth?: number;
  maxWidth?: number;
}

export function useResizableColumns({
  storageKey,
  defaultWidths,
  minWidth = 50,
  maxWidth = 500,
}: ResizableColumnOptions) {
  const [columnWidths, setColumnWidths] = useState<Record<string, number>>(() => {
    if (typeof window === 'undefined') return defaultWidths;
    
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved) {
        const parsed = JSON.parse(saved);
        return { ...defaultWidths, ...parsed };
      }
    } catch (error) {
      console.warn('Failed to load column widths from localStorage:', error);
    }
    return defaultWidths;
  });

  const isResizingRef = useRef(false);
  const startXRef = useRef(0);
  const startWidthRef = useRef(0);
  const resizingColumnRef = useRef<string>('');

  const updateColumnWidth = useCallback((columnKey: string, width: number) => {
    const constrainedWidth = Math.max(minWidth, Math.min(maxWidth, width));
    
    setColumnWidths(prev => {
      const newWidths = { ...prev, [columnKey]: constrainedWidth };
      
      // Save to localStorage
      try {
        localStorage.setItem(storageKey, JSON.stringify(newWidths));
      } catch (error) {
        console.warn('Failed to save column widths to localStorage:', error);
      }
      
      return newWidths;
    });
  }, [storageKey, minWidth, maxWidth]);

  const resetColumnWidths = useCallback(() => {
    setColumnWidths(defaultWidths);
    try {
      localStorage.removeItem(storageKey);
    } catch (error) {
      console.warn('Failed to remove column widths from localStorage:', error);
    }
  }, [defaultWidths, storageKey]);

  const handleMouseDown = useCallback((e: React.MouseEvent | React.TouchEvent, columnKey: string) => {
    e.preventDefault();
    isResizingRef.current = true;
    
    // Handle both mouse and touch events
    const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
    startXRef.current = clientX;
    startWidthRef.current = columnWidths[columnKey];
    resizingColumnRef.current = columnKey;
    
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, [columnWidths]);

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isResizingRef.current || !resizingColumnRef.current) return;
    
    const diff = e.clientX - startXRef.current;
    const newWidth = startWidthRef.current + diff;
    updateColumnWidth(resizingColumnRef.current, newWidth);
  }, [updateColumnWidth]);

  const handleTouchMove = useCallback((e: TouchEvent) => {
    if (!isResizingRef.current || !resizingColumnRef.current) return;
    
    const touch = e.touches[0];
    const diff = touch.clientX - startXRef.current;
    const newWidth = startWidthRef.current + diff;
    updateColumnWidth(resizingColumnRef.current, newWidth);
  }, [updateColumnWidth]);

  const handleMouseUp = useCallback(() => {
    isResizingRef.current = false;
    resizingColumnRef.current = '';
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  const handleTouchEnd = useCallback(() => {
    isResizingRef.current = false;
    resizingColumnRef.current = '';
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  useEffect(() => {
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.addEventListener('touchmove', handleTouchMove, { passive: false });
    document.addEventListener('touchend', handleTouchEnd);
    
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.removeEventListener('touchmove', handleTouchMove);
      document.removeEventListener('touchend', handleTouchEnd);
    };
  }, [handleMouseMove, handleMouseUp, handleTouchMove, handleTouchEnd]);

  const getColumnStyle = useCallback((columnKey: string) => {
    const width = columnWidths[columnKey] || defaultWidths[columnKey] || 100;
    return {
      width: `${width}px`,
      minWidth: `${width}px`,
      maxWidth: `${width}px`,
    };
  }, [columnWidths, defaultWidths]);

  return {
    columnWidths,
    updateColumnWidth,
    resetColumnWidths,
    handleMouseDown,
    getColumnStyle,
    isResizing: isResizingRef.current,
  };
}