'use client';

import { useEffect } from 'react';

export function DensityProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    const savedDensity = localStorage.getItem('density') || 'compact';
    document.documentElement.setAttribute('data-density', savedDensity);
  }, []);

  return <>{children}</>;
}