'use client';

import { Settings, RotateCcw, Columns3 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { ColumnDefinition } from '@/types';

interface CustomizeColumnsProps {
  columns: Record<string, ColumnDefinition>;
  visibleColumns: Set<string>;
  onColumnToggle: (columnKey: string) => void;
  onReset: () => void;
  onResetWidths?: () => void;
}

export function CustomizeColumns({
  columns,
  visibleColumns,
  onColumnToggle,
  onReset,
  onResetWidths,
}: CustomizeColumnsProps) {
  const optionalColumns = Object.entries(columns).filter(([, def]) => !def.required);

  return (
    <TooltipProvider>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button 
            variant="outline" 
            size="sm"
            className="h-9 w-9 sm:w-auto p-0 sm:px-3 sm:gap-2"
          >
            <Settings className="h-4 w-4" />
            <span className="hidden sm:inline">Columns</span>
          </Button>
        </DropdownMenuTrigger>
        
        <DropdownMenuContent className="w-64" align="end">
          <div className="flex items-center justify-end p-2">
            <div className="flex gap-1">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={onReset}
                    className="h-6 px-2 text-xs"
                  >
                    <RotateCcw className="h-3 w-3 mr-1" />
                    Reset
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <p>Reset column visibility</p>
                </TooltipContent>
              </Tooltip>
              {onResetWidths && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={onResetWidths}
                      className="h-6 px-2 text-xs"
                    >
                      <Columns3 className="h-3 w-3 mr-1" />
                      Widths
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>Reset column widths to default</p>
                  </TooltipContent>
                </Tooltip>
              )}
            </div>
          </div>
        
        <DropdownMenuSeparator />
        
        {optionalColumns.map(([columnKey, columnDef]) => {
          const isVisible = visibleColumns.has(columnKey);
          
          return (
            <DropdownMenuItem
              key={columnKey}
              className="flex items-center justify-between cursor-pointer"
              onSelect={(e) => {
                e.preventDefault();
                onColumnToggle(columnKey);
              }}
            >
              <span className="text-sm">{columnDef.label}</span>
              <Switch
                checked={isVisible}
              />
            </DropdownMenuItem>
          );
        })}
        </DropdownMenuContent>
      </DropdownMenu>
    </TooltipProvider>
  );
}