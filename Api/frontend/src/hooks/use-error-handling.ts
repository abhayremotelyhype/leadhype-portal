import { useState, useCallback } from 'react';
import { useToast } from '@/hooks/use-toast';

interface UseErrorHandlingOptions {
  resetOnSuccess?: boolean;
  showToast?: boolean; // Whether to show toast notifications (default: true)
}

export function useErrorHandling(options: UseErrorHandlingOptions = {}) {
  const { toast } = useToast();
  const [error, setError] = useState<string | null>(null);
  const [hasShownError, setHasShownError] = useState(false);

  const showError = useCallback((title: string, description: string) => {
    setError(description);
    
    // Only show toast if enabled and not already shown
    if (options.showToast !== false && !hasShownError) {
      toast({
        variant: 'destructive',
        title,
        description,
      });
      setHasShownError(true);
    }
  }, [toast, hasShownError, options.showToast]);

  // Enhanced error handler for API errors
  const handleApiError = useCallback((err: any, context: string = 'operation') => {
    // Ignore abort errors - they're expected when requests are canceled
    if (err.name === 'AbortError') {
      console.log(`${context} request was aborted (expected behavior)`);
      return;
    }
    
    console.error(`Failed ${context}:`, err);
    
    // Handle different types of errors appropriately
    if (err.isAuthError) {
      showError('Authentication Error', 'Your session has expired. Redirecting to login...');
    } else if (err.isForbidden) {
      showError('Access Denied', `You do not have permission to perform this ${context}.`);
    } else if (err.isServerError) {
      showError('Server Error', `Server error occurred: ${err.detail || 'Please try again later.'}`);
    } else if (err.isNetworkError) {
      showError('Connection Error', `Failed to ${context}. Please check your connection and try again.`);
    } else {
      // Generic error with more detail if available
      const errorMessage = err.detail || err.message || 'An unexpected error occurred.';
      showError('Error', `Failed to ${context}: ${errorMessage}`);
    }
  }, [showError]);

  const clearError = useCallback(() => {
    setError(null);
    setHasShownError(false);
  }, []);

  const handleSuccess = useCallback(() => {
    if (options.resetOnSuccess && error) {
      clearError();
    }
  }, [error, clearError, options.resetOnSuccess]);

  return {
    error,
    hasShownError,
    showError,
    handleApiError,
    clearError,
    handleSuccess,
  };
}