'use client';

import { useState, useCallback, useEffect } from 'react';
import { GripVertical, Eye, EyeOff, RotateCcw, Settings } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Separator } from '@/components/ui/separator';
import { ColumnDefinition } from '@/types';
import { cn } from '@/lib/utils';

// Simple drag and drop implementation without external dependencies
interface DragItem {
  key: string;
  index: number;
}

interface ColumnItem {
  key: string;
  definition: ColumnDefinition;
  visible: boolean;
  required: boolean;
}

interface ColumnCustomizationModalProps {
  columns: Record<string, ColumnDefinition>;
  visibleColumns: Set<string>;
  columnOrder?: string[]; // Optional: current column order
  onColumnToggle: (columnKey: string) => void;
  onColumnReorder?: (newOrder: string[]) => void; // Optional: for reordering
  onResetColumns: () => void;
  onResetOrder?: () => void; // Optional: reset column order
  trigger?: React.ReactNode;
  title?: string;
  description?: string;
}

export function ColumnCustomizationModal({
  columns,
  visibleColumns,
  columnOrder,
  onColumnToggle,
  onColumnReorder,
  onResetColumns,
  onResetOrder,
  trigger,
  title = "Customize Columns",
  description = "Show, hide, and reorder columns to customize your view.",
}: ColumnCustomizationModalProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);
  const [showRequired, setShowRequired] = useState(false);

  // Create ordered column items (optionally including required columns)
  const createOrderedItems = useCallback((): ColumnItem[] => {
    const columnEntries = Object.entries(columns).filter(([_, definition]) => 
      showRequired || !definition.required
    );
    
    if (columnOrder && onColumnReorder) {
      // Use provided order
      const orderedEntries: [string, ColumnDefinition][] = [];
      const remainingEntries = [...columnEntries];
      
      // Add columns in the specified order
      for (const key of columnOrder) {
        const entryIndex = remainingEntries.findIndex(([k]) => k === key);
        if (entryIndex >= 0) {
          orderedEntries.push(remainingEntries[entryIndex]);
          remainingEntries.splice(entryIndex, 1);
        }
      }
      
      // Add any remaining columns that weren't in the order
      orderedEntries.push(...remainingEntries);
      
      return orderedEntries.map(([key, definition]) => ({
        key,
        definition,
        visible: visibleColumns.has(key),
        required: definition.required || false,
      }));
    } else {
      // Use default order (object key order)
      return columnEntries.map(([key, definition]) => ({
        key,
        definition,
        visible: visibleColumns.has(key),
        required: definition.required || false,
      }));
    }
  }, [columns, visibleColumns, columnOrder, onColumnReorder, showRequired]);

  const [items, setItems] = useState<ColumnItem[]>(createOrderedItems);

  // Update items when props change
  useEffect(() => {
    setItems(createOrderedItems());
  }, [createOrderedItems]);

  const handleDragStart = (e: React.DragEvent, index: number) => {
    if (!onColumnReorder) return;
    
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/html', '');
    
    // Add visual feedback
    const target = e.target as HTMLElement;
    target.style.opacity = '0.5';
  };

  const handleDragEnd = (e: React.DragEvent) => {
    if (!onColumnReorder) return;
    
    const target = e.target as HTMLElement;
    target.style.opacity = '';
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    if (!onColumnReorder || draggedIndex === null) return;
    
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    
    if (index !== draggedIndex) {
      setDragOverIndex(index);
    }
  };

  const handleDragLeave = () => {
    setDragOverIndex(null);
  };

  const handleDrop = (e: React.DragEvent, dropIndex: number) => {
    if (!onColumnReorder || draggedIndex === null) return;
    
    e.preventDefault();
    
    if (draggedIndex !== dropIndex) {
      const newItems = [...items];
      const draggedItem = newItems[draggedIndex];
      
      // Remove dragged item
      newItems.splice(draggedIndex, 1);
      
      // Insert at new position
      newItems.splice(dropIndex, 0, draggedItem);
      
      setItems(newItems);
      
      // Call the reorder callback with new order
      const newOrder = newItems.map(item => item.key);
      onColumnReorder(newOrder);
    }
    
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  const handleToggle = (columnKey: string) => {
    onColumnToggle(columnKey);
    
    // Update local state
    setItems(prev => prev.map(item => 
      item.key === columnKey 
        ? { ...item, visible: !item.visible }
        : item
    ));
  };

  const handleResetColumns = () => {
    onResetColumns();
    setItems(createOrderedItems());
  };

  const handleResetOrder = () => {
    if (onResetOrder) {
      onResetOrder();
      setItems(createOrderedItems());
    }
  };

  const visibleCount = items.filter(item => item.visible).length;
  const totalCount = items.length;

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        {trigger || (
          <Button variant="outline" size="sm">
            <Settings className="h-4 w-4 mr-2" />
            Columns
          </Button>
        )}
      </DialogTrigger>
      
      <DialogContent className="w-[420px] max-w-[90vw] max-h-[80vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <div className="flex items-center justify-between py-2 border-b">
          <span className="text-sm text-muted-foreground">
            {items.length} columns
          </span>
          <div className="flex items-center gap-2">
            <label className="text-xs text-muted-foreground cursor-pointer" htmlFor="show-required">
              Show required
            </label>
            <Switch
              id="show-required"
              checked={showRequired}
              onCheckedChange={setShowRequired}
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
          <div className="space-y-1 py-2">
            {items.map((item, index) => (
              <div
                key={item.key}
                className={cn(
                  "flex items-center gap-3 p-2 rounded border transition-colors",
                  "hover:bg-accent",
                  draggedIndex === index && "opacity-50",
                  dragOverIndex === index && "border-primary",
                )}
                draggable={!!onColumnReorder}
                onDragStart={(e) => handleDragStart(e, index)}
                onDragEnd={handleDragEnd}
                onDragOver={(e) => handleDragOver(e, index)}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, index)}
              >
                {onColumnReorder && (
                  <GripVertical className="h-4 w-4 text-muted-foreground cursor-grab active:cursor-grabbing" />
                )}
                
                <div className="flex-1 min-w-0">
                  <span className="text-sm">{item.definition.label}</span>
                  {item.required && (
                    <Badge variant="outline" className="ml-2 text-xs">Required</Badge>
                  )}
                </div>

                <Switch
                  checked={item.visible}
                  onCheckedChange={() => handleToggle(item.key)}
                  disabled={item.required}
                />
              </div>
            ))}
          </div>
        </div>

        <DialogFooter className="gap-2">
          <Button variant="ghost" size="sm" onClick={handleResetColumns}>
            Reset
          </Button>
          <Button onClick={() => setIsOpen(false)}>
            Done
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}