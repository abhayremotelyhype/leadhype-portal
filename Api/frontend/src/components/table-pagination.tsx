'use client';

import { ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';

interface TablePaginationProps {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  pageSizeOptions: number[];
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}

export function TablePagination({
  currentPage,
  totalPages,
  totalCount,
  pageSize,
  pageSizeOptions,
  onPageChange,
  onPageSizeChange,
}: TablePaginationProps) {
  return (
    <div className="flex items-center justify-between border-t px-2 py-1.5 animate-in fade-in duration-150">
      {/* Left side - Page size selector only */}
      <div className="flex items-center gap-1.5">
        <span className="text-xs text-muted-foreground hidden sm:inline">Rows:</span>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
              {pageSize}
              <ChevronDown className="ml-0.5 h-3 w-3" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            {pageSizeOptions.map((size) => (
              <DropdownMenuItem
                key={size}
                onClick={() => onPageSizeChange(size)}
                className={pageSize === size ? 'bg-accent' : ''}
              >
                {size}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Right side - Pagination */}
      {totalPages > 1 && (
        <Pagination className="mx-0 w-auto">
          <PaginationContent className="gap-1">
            <PaginationItem>
              <PaginationPrevious 
                onClick={() => onPageChange(currentPage - 1)}
                className={`cursor-pointer h-7 px-2 transition-all duration-150 active:scale-95 ${currentPage === 1 ? 'pointer-events-none opacity-50' : 'hover:scale-105'}`}
              />
            </PaginationItem>
            
            {/* Page numbers - Mobile: show 3 pages, Desktop: show 5 */}
            {(() => {
              const pages = [];
              const maxVisible = 3; // Show 3 pages on mobile, 5 on desktop
              const maxVisibleDesktop = 5;
              const isMobile = true; // We'll show 3 for mobile layout
              
              // Mobile: show current and 1 adjacent page when possible
              const mobileStart = Math.max(1, currentPage - 1);
              const mobileEnd = Math.min(totalPages, mobileStart + 2);
              
              // Desktop: show more pages
              const desktopStart = Math.max(1, currentPage - 2);
              const desktopEnd = Math.min(totalPages, desktopStart + maxVisibleDesktop - 1);
              
              // Mobile pages (show 3 pages max)
              for (let i = mobileStart; i <= mobileEnd; i++) {
                pages.push(
                  <PaginationItem key={`mobile-${i}`} className="block sm:hidden">
                    <PaginationLink 
                      onClick={() => onPageChange(i)}
                      isActive={currentPage === i}
                      className="cursor-pointer h-7 px-2 text-xs transition-all duration-150 hover:scale-105 active:scale-95"
                    >
                      {i}
                    </PaginationLink>
                  </PaginationItem>
                );
              }
              
              // Desktop pages (show 5 pages max)
              for (let i = desktopStart; i <= desktopEnd; i++) {
                pages.push(
                  <PaginationItem key={`desktop-${i}`} className="hidden sm:block">
                    <PaginationLink 
                      onClick={() => onPageChange(i)}
                      isActive={currentPage === i}
                      className="cursor-pointer h-7 px-2 transition-all duration-150 hover:scale-105 active:scale-95"
                    >
                      {i}
                    </PaginationLink>
                  </PaginationItem>
                );
              }
              
              return pages;
            })()}
            
            <PaginationItem>
              <PaginationNext 
                onClick={() => onPageChange(currentPage + 1)}
                className={`cursor-pointer h-7 px-2 transition-all duration-150 active:scale-95 ${currentPage === totalPages ? 'pointer-events-none opacity-50' : 'hover:scale-105'}`}
              />
            </PaginationItem>
          </PaginationContent>
        </Pagination>
      )}
    </div>
  );
}