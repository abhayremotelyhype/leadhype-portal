// API Configuration and utilities
// Using relative URLs for proxy server setup
export const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

export const ENDPOINTS = {
  status: '/status',
  accounts: '/api/accounts',
  campaigns: '/api/campaigns',
  campaignList: '/api/campaigns/list',
  campaignSearch: '/api/campaigns/search',
  clients: '/api/clients',
  clientList: '/api/clients/list',
  clientSearch: '/api/clients/search',
  users: '/api/users',
  userList: '/api/users/list',
  emailAccounts: '/api/email-accounts',
  settings: '/api/settings',
  // Webhook endpoints
  webhooks: '/api/webhooks',
  webhookEvents: '/api/webhook-events',
  // V1 API endpoints
  v1: {
    campaigns: '/api/v1/campaigns',
    emailAccounts: '/api/v1/email-accounts',
  },
  // Dashboard endpoints
  dashboard: {
    overview: '/api/dashboard/overview',
    performanceTrend: '/api/dashboard/performance-trend',
    topCampaigns: '/api/dashboard/top-campaigns',
    topClients: '/api/dashboard/top-clients',
    emailAccountsSummary: '/api/dashboard/email-accounts-summary',
    recentActivities: '/api/dashboard/recent-activities',
    stats: '/api/dashboard/stats',
    realtime: '/api/dashboard/realtime',
  },
  // Analytics endpoints
  analytics: {
    dashboard: '/api/analytics/dashboard',
    overview: '/api/analytics/overview',
    performanceTrends: '/api/analytics/performance-trends',
    emailAccountPerformance: '/api/analytics/email-account-performance',
    clientComparison: '/api/analytics/client-comparison',
  },
} as const;

export interface ApiResponse<T = any> {
  data?: T;
  message?: string;
  error?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit & { responseType?: 'json' | 'blob' } = {}
  ): Promise<T> {
    // Use relative URLs when baseUrl is empty (proxy server setup)
    const url = this.baseUrl ? `${this.baseUrl}${endpoint}` : endpoint;
    const { responseType = 'json', ...fetchOptions } = options;
    
    // Get token from localStorage for authentication
    const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
    const refreshToken = typeof window !== 'undefined' ? localStorage.getItem('refreshToken') : null;
    
    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
        ...fetchOptions.headers,
      },
      ...fetchOptions,
    };

    try {
      const response = await fetch(url, config);
      
      if (!response.ok) {
        let errorMessage = `HTTP error! status: ${response.status}`;
        let errorDetail = '';
        
        // Try to get error details from response
        try {
          const errorData = await response.json();
          if (errorData.message) {
            errorDetail = errorData.message;
          }
        } catch {
          // If JSON parsing fails, use status text
          errorDetail = response.statusText;
        }
        
        // Create enhanced error object
        const apiError = new Error(errorMessage) as any;
        apiError.status = response.status;
        apiError.statusText = response.statusText;
        apiError.detail = errorDetail;
        apiError.isAuthError = response.status === 401;
        apiError.isForbidden = response.status === 403;
        apiError.isNotFound = response.status === 404;
        apiError.isServerError = response.status >= 500;
        
        // For 401 errors, try token refresh first before logging out
        if (response.status === 401 && typeof window !== 'undefined') {
          const refreshToken = localStorage.getItem('refreshToken');
          
          // Only try refresh if we have a refresh token and this isn't already a refresh request
          if (refreshToken && !endpoint.endsWith('/refresh')) {
            try {
              const refreshResponse = await fetch('/api/auth/refresh', {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                },
                body: JSON.stringify({ refreshToken }),
              });

              if (refreshResponse.ok) {
                const refreshData = await refreshResponse.json();
                localStorage.setItem('accessToken', refreshData.accessToken);
                localStorage.setItem('refreshToken', refreshData.refreshToken);
                
                // Retry the original request with new token
                config.headers = {
                  ...config.headers,
                  Authorization: `Bearer ${refreshData.accessToken}`,
                };
                
                const retryResponse = await fetch(url, config);
                if (retryResponse.ok) {
                  if (responseType === 'blob') {
                    return await retryResponse.blob() as T;
                  }
                  
                  // Handle empty responses (like 204 NoContent) in retry
                  if (retryResponse.status === 204 || retryResponse.headers.get('content-length') === '0') {
                    return null as T;
                  }
                  
                  // Check if response has content before trying to parse JSON
                  const retryContentType = retryResponse.headers.get('content-type');
                  if (!retryContentType || !retryContentType.includes('application/json')) {
                    const retryText = await retryResponse.text();
                    return (retryText || null) as T;
                  }
                  
                  return await retryResponse.json();
                }
              }
            } catch (refreshError) {
              console.error('Token refresh failed:', refreshError);
            }
          }
          
          // If refresh failed or no refresh token, clear tokens and redirect
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          
          // Redirect to login after a short delay to allow error to be handled
          setTimeout(() => {
            window.location.href = '/login';
          }, 1000);
        }
        
        throw apiError;
      }

      if (responseType === 'blob') {
        const blob = await response.blob();
        return blob as T;
      }
      
      // Handle empty responses (like 204 NoContent)
      if (response.status === 204 || response.headers.get('content-length') === '0') {
        return null as T;
      }
      
      // Check if response has content before trying to parse JSON
      const contentType = response.headers.get('content-type');
      if (!contentType || !contentType.includes('application/json')) {
        const text = await response.text();
        return (text || null) as T;
      }
      
      const data = await response.json();
      return data;
    } catch (error: any) {
      console.error('API request failed:', error);
      
      // If it's already our enhanced error, just throw it
      if (error.status) {
        throw error;
      }
      
      // For network errors, enhance with connection info
      error.isNetworkError = true;
      throw error;
    }
  }

  async get<T>(endpoint: string, params?: Record<string, string>, options?: RequestInit & { responseType?: 'json' | 'blob' }): Promise<T> {
    const url = params 
      ? `${endpoint}?${new URLSearchParams(params).toString()}`
      : endpoint;
    
    return this.request<T>(url, { method: 'GET', ...options });
  }

  async post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}

export const apiClient = new ApiClient();

// Utility functions
export function formatDate(dateString: string): string {
  if (!dateString) return 'N/A';
  
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  } catch {
    return 'Invalid Date';
  }
}

export function formatDateTime(dateString: string): string {
  if (!dateString) return 'N/A';
  
  try {
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return 'Invalid Date';
  }
}

export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
}

// Utility function for handling API errors in components with toast
export function handleApiErrorWithToast(error: any, context: string, toastFn: any) {
  console.error(`Failed ${context}:`, error);
  
  if (error.isAuthError) {
    toastFn({
      variant: 'destructive',
      title: 'Authentication Error',
      description: 'Your session has expired. Redirecting to login...',
    });
  } else if (error.isForbidden) {
    toastFn({
      variant: 'destructive',
      title: 'Access Denied',
      description: `You do not have permission to ${context}.`,
    });
  } else if (error.isServerError) {
    toastFn({
      variant: 'destructive',
      title: 'Server Error',
      description: `Server error occurred: ${error.detail || 'Please try again later.'}`,
    });
  } else if (error.isNetworkError) {
    toastFn({
      variant: 'destructive',
      title: 'Connection Error',
      description: `Failed to ${context}. Please check your connection and try again.`,
    });
  } else {
    const errorMessage = error.detail || error.message || 'An unexpected error occurred.';
    toastFn({
      variant: 'destructive',
      title: 'Error',
      description: `Failed to ${context}: ${errorMessage}`,
    });
  }
}

