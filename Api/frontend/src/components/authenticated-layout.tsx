'use client';

import { useAuth } from '@/contexts/auth-context';
import { usePathname } from 'next/navigation';
import { AppSidebar } from '@/components/app-sidebar';
import { SidebarProvider, SidebarInset } from '@/components/ui/sidebar';
import { PageTransition } from '@/components/page-transition';
import { LoadingBar } from '@/components/loading-bar';

interface AuthenticatedLayoutProps {
  children: React.ReactNode;
}

// Routes that don't require authentication
const publicRoutes = ['/login'];

export function AuthenticatedLayout({ children }: AuthenticatedLayoutProps) {
  const { isAuthenticated, isLoading } = useAuth();
  const pathname = usePathname();

  const isPublicRoute = publicRoutes.includes(pathname);

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  // If it's a public route or user is not authenticated, show without sidebar
  if (isPublicRoute || !isAuthenticated) {
    return (
      <>
        <LoadingBar />
        <PageTransition>
          {children}
        </PageTransition>
      </>
    );
  }

  // If authenticated, show with sidebar
  return (
    <SidebarProvider defaultOpen={false}>
      <LoadingBar />
      <AppSidebar />
      <SidebarInset>
        <PageTransition>
          {children}
        </PageTransition>
      </SidebarInset>
    </SidebarProvider>
  );
}