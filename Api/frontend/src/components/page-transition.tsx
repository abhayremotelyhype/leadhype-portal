'use client';

import { useEffect, useState, useRef } from 'react';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';

interface PageTransitionProps {
  children: React.ReactNode;
  className?: string;
}

export function PageTransition({ children, className }: PageTransitionProps) {
  const pathname = usePathname();
  const [isAnimating, setIsAnimating] = useState(false);
  const previousPathname = useRef(pathname);
  
  useEffect(() => {
    if (pathname !== previousPathname.current) {
      setIsAnimating(true);
      previousPathname.current = pathname;
      
      // Complete enter animation
      const enterTimer = setTimeout(() => {
        setIsAnimating(false);
      }, 300); // Full transition duration
      
      return () => {
        clearTimeout(enterTimer);
      };
    }
  }, [pathname]);
  
  return (
    <div
      className={cn(
        'page-transition-container',
        isAnimating && 'page-transitioning',
        className
      )}
    >
      <div className="page-content">
        {children}
      </div>
    </div>
  );
}

// Hook for programmatic navigation with transitions
export function usePageTransition() {
  const [isTransitioning, setIsTransitioning] = useState(false);
  
  const startTransition = () => {
    setIsTransitioning(true);
    setTimeout(() => setIsTransitioning(false), 300);
  };
  
  return {
    isTransitioning,
    startTransition
  };
}