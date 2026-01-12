import { ProtectedRoute } from '@/components/protected-route';
import { PageHeader } from '@/components/page-header';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertTriangle, RefreshCw, BarChart3 } from 'lucide-react';

interface DashboardErrorStateProps {
  onRetry: () => void;
}

export function DashboardErrorState({ onRetry }: DashboardErrorStateProps) {
  return (
    <ProtectedRoute>
      <div className="flex h-full flex-col">
        <PageHeader 
          title="Dashboard"
          description="Campaign performance and analytics overview"
          icon={BarChart3}
        />
        <div className="flex-1 p-6">
          <Alert className="p-6">
            <AlertTriangle />
            <AlertTitle>No Data Available</AlertTitle>
            <AlertDescription className="mt-4">
              <div className="space-y-4">
                <div>
                  <p>We couldn&apos;t load your dashboard data at the moment.</p>
                  <p className="text-sm mt-1">This could be due to a temporary network issue or server maintenance.</p>
                </div>
                <div className="flex flex-col sm:flex-row gap-3 justify-start items-start">
                  <Button onClick={onRetry} className="min-w-32">
                    <RefreshCw className="w-4 h-4 mr-2" />
                    Try Again
                  </Button>
                  <Button variant="outline" onClick={() => window.location.reload()} className="min-w-32">
                    <RefreshCw className="w-4 h-4 mr-2" />
                    Refresh Page
                  </Button>
                </div>
              </div>
            </AlertDescription>
          </Alert>
        </div>
      </div>
    </ProtectedRoute>
  );
}