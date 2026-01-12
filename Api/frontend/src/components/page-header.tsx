'use client';

import React from 'react';
import { LucideIcon } from 'lucide-react';
import { SidebarTrigger } from '@/components/ui/sidebar';
import { useSafeSidebar } from '@/hooks/use-safe-sidebar';
import { cn } from '@/lib/utils';

interface PageHeaderProps {
  title: string;
  description: string;
  mobileDescription?: string;
  icon?: LucideIcon;
  actions?: React.ReactNode;
  className?: string;
  showSidebarTrigger?: boolean;
  itemCount?: number;
  itemLabel?: string;
  originalTotalCount?: number;
  searchQuery?: string;
}

export function PageHeader({ 
  title, 
  description, 
  mobileDescription,
  icon: Icon,
  actions,
  className,
  showSidebarTrigger = true,
  itemCount,
  itemLabel,
  originalTotalCount,
  searchQuery
}: PageHeaderProps) {
  const sidebar = useSafeSidebar();
  return (
    <div className={cn(
      "border-b bg-gradient-to-r from-background via-muted/30 to-background",
      className
    )}>
      <div className="px-3 py-3">
        <div className="flex items-center justify-between">
          {/* Title Section */}
          <div className="flex items-center gap-3 flex-1 min-w-0">
            {showSidebarTrigger && sidebar.isAvailable && <SidebarTrigger />}
            <div className="flex items-center gap-2 flex-1 min-w-0">
              {Icon && (
                <div className="flex h-6 w-6 sm:h-8 sm:w-8 items-center justify-center rounded-lg bg-foreground shadow-sm flex-shrink-0">
                  <Icon className="h-3 w-3 sm:h-4 sm:w-4 text-background" />
                </div>
              )}
              <div className="flex flex-col flex-1 min-w-0">
                <h1 className="text-lg font-semibold tracking-tight bg-gradient-to-r from-foreground to-foreground/80 bg-clip-text truncate">
                  {title}
                </h1>
                <p className="text-xs text-muted-foreground hidden sm:block truncate">
                  {description}
                </p>
                <p className="text-xs text-muted-foreground sm:hidden truncate">
                  {mobileDescription || (
                    title.toLowerCase().includes('campaigns') ? 'Campaign management' : 
                    title.toLowerCase().includes('clients') ? 'Client management' :
                    title.toLowerCase().includes('email') ? 'Email management' :
                    title.toLowerCase().includes('smartlead') ? 'Account management' :
                    title.toLowerCase().includes('dashboard') ? 'Performance metrics' :
                    'Management'
                  )}
                </p>
              </div>
            </div>
          </div>

          {/* Count and Actions Section */}
          <div className="flex items-center gap-3 flex-shrink-0 ml-2">
            {itemCount !== undefined && itemLabel && (
              <span className="text-sm font-medium text-foreground bg-muted/30 px-2 py-1 rounded-md border flex-shrink-0">
                {searchQuery && originalTotalCount ? (
                  <>
                    {itemCount.toLocaleString()}
                    <span className="text-muted-foreground mx-1">/</span>
                    {originalTotalCount.toLocaleString()}
                  </>
                ) : (
                  itemCount.toLocaleString()
                )} {itemLabel}
              </span>
            )}
            {actions && actions}
          </div>
        </div>
      </div>
    </div>
  );
}