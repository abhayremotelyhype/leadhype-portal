'use client';

import React from 'react';
import { SortConfig } from '@/types';

interface StatsCellProps {
  count: number;
  baseValue: number;
  column: string;
  sortConfig: SortConfig;
  colorScheme?: 'opened' | 'replied' | 'bounced' | 'clicked' | 'unsubscribed' | 'positive';
}

export function StatsCell({ count, baseValue, column, sortConfig, colorScheme = 'opened' }: StatsCellProps) {
  const percentage = baseValue > 0 ? ((count / baseValue) * 100).toFixed(1) : '0';
  
  const isActive = sortConfig.column === column;
  const showPercentageFirst = isActive && sortConfig.mode === 'percentage';

  // Color schemes for different stat types
  const colorSchemes = {
    opened: {
      primary: 'text-blue-700 dark:text-blue-400',
      secondary: 'text-blue-600 dark:text-blue-500',
      bg: 'bg-blue-50 dark:bg-blue-950'
    },
    replied: {
      primary: 'text-green-700 dark:text-green-400',
      secondary: 'text-green-600 dark:text-green-500',
      bg: 'bg-green-50 dark:bg-green-950'
    },
    bounced: {
      primary: 'text-red-700 dark:text-red-400',
      secondary: 'text-red-600 dark:text-red-500',
      bg: 'bg-red-50 dark:bg-red-950'
    },
    clicked: {
      primary: 'text-purple-700 dark:text-purple-400',
      secondary: 'text-purple-600 dark:text-purple-500',
      bg: 'bg-purple-50 dark:bg-purple-950'
    },
    unsubscribed: {
      primary: 'text-orange-700 dark:text-orange-400',
      secondary: 'text-orange-600 dark:text-orange-500',
      bg: 'bg-orange-50 dark:bg-orange-950'
    },
    positive: {
      primary: 'text-emerald-700 dark:text-emerald-400',
      secondary: 'text-emerald-600 dark:text-emerald-500',
      bg: 'bg-emerald-50 dark:bg-emerald-950'
    }
  };

  const colors = colorSchemes[colorScheme];

  if (count === 0) {
    return <span className="text-xs text-muted-foreground">-</span>;
  }

  return (
    <div className={`inline-flex flex-col items-start rounded px-2 py-1 ${isActive ? colors.bg : ''}`}>
      {showPercentageFirst ? (
        <>
          <span className={`text-xs font-semibold ${isActive ? colors.primary : 'text-foreground'}`}>
            {percentage}%
          </span>
          <span className={`text-[10px] ${isActive ? colors.secondary : 'text-muted-foreground'}`}>
            {count.toLocaleString()}
          </span>
        </>
      ) : (
        <>
          <span className={`text-xs font-semibold ${isActive ? colors.primary : 'text-foreground'}`}>
            {count.toLocaleString()}
          </span>
          <span className={`text-[10px] ${isActive ? colors.secondary : 'text-muted-foreground'}`}>
            {percentage}%
          </span>
        </>
      )}
    </div>
  );
}