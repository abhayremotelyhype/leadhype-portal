'use client';

import React, { createContext, useContext, useEffect, useState, useRef } from 'react';
import { useRouter } from 'next/navigation';

interface User {
  id: string;
  email: string;
  username: string;
  role: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  assignedClientIds?: string[];
  apiKey?: string;
  apiKeyCreatedAt?: string;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
  refreshUser: () => Promise<void>;
  isLoading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isClient, setIsClient] = useState(false); // Track if we're on client
  const router = useRouter();
  const hasInitializedRef = useRef(false);
  const abortControllerRef = useRef<AbortController | null>(null);

  const isAuthenticated = !!user;
  const isAdmin = user?.role === 'Admin';

  // Set client flag after hydration
  useEffect(() => {
    setIsClient(true);
  }, []);

  // Safe localStorage access
  const getStorageItem = (key: string): string | null => {
    if (!isClient) return null;
    try {
      return localStorage.getItem(key);
    } catch (error) {
      console.error(`Error accessing localStorage for key ${key}:`, error);
      return null;
    }
  };

  const setStorageItem = (key: string, value: string): void => {
    if (!isClient) return;
    try {
      localStorage.setItem(key, value);
    } catch (error) {
      console.error(`Error setting localStorage for key ${key}:`, error);
    }
  };

  const removeStorageItem = (key: string): void => {
    if (!isClient) return;
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.error(`Error removing localStorage for key ${key}:`, error);
    }
  };

  // Check if user is logged in on app start and set up token refresh
  useEffect(() => {
    // Only run on client after hydration
    if (!isClient) return;
    
    // Prevent duplicate initialization in StrictMode
    if (hasInitializedRef.current) return;
    hasInitializedRef.current = true;
    
    // Cancel any previous auth check
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    
    // Create new abort controller
    abortControllerRef.current = new AbortController();
    
    checkAuth(abortControllerRef.current.signal);
    
    // Set up automatic token refresh (check every 2 minutes for proactive refresh)
    const refreshInterval = setInterval(() => {
      checkAndRefreshToken();
    }, 2 * 60 * 1000); // 2 minutes

    // Add activity-based refresh on user interactions
    const activityEvents = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'];
    let lastActivityTime = Date.now();
    
    const handleUserActivity = () => {
      const now = Date.now();
      // Only check for refresh if it's been more than 10 minutes since last activity check
      if (now - lastActivityTime > 10 * 60 * 1000) {
        lastActivityTime = now;
        checkAndRefreshToken();
      }
    };

    activityEvents.forEach(event => {
      document.addEventListener(event, handleUserActivity, true);
    });
    
    return () => {
      clearInterval(refreshInterval);
      activityEvents.forEach(event => {
        document.removeEventListener(event, handleUserActivity, true);
      });
      // Reset the initialization flag on unmount
      hasInitializedRef.current = false;
    };
  }, [isClient]); // Depend on isClient

  const refreshTokenIfNeeded = async () => {
    const refreshToken = getStorageItem('refreshToken');
    if (!refreshToken) return;

    try {
      const response = await fetch('/api/auth/refresh', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
      });

      if (response.ok) {
        const data = await response.json();
        setStorageItem('accessToken', data.accessToken);
        setStorageItem('refreshToken', data.refreshToken);
        setUser(data.user);
      } else {
        // Refresh failed, log out user
        logout();
      }
    } catch (error) {
      console.error('Token refresh failed:', error);
      logout();
    }
  };

  // Check if token needs refresh based on expiration time
  const checkAndRefreshToken = async () => {
    const accessToken = getStorageItem('accessToken');
    const refreshToken = getStorageItem('refreshToken');
    
    if (!accessToken || !refreshToken) {
      return;
    }

    try {
      // Decode JWT token to check expiration (simple base64 decode)
      const payload = JSON.parse(atob(accessToken.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      const timeUntilExpiry = payload.exp - currentTime;
      
      // Refresh token if it expires in less than 5 minutes (300 seconds)
      if (timeUntilExpiry < 300) {
        console.log('Token expires soon, refreshing...');
        await refreshTokenIfNeeded();
      }
    } catch (error) {
      console.error('Error checking token expiration:', error);
      // If we can't decode the token, try to refresh anyway
      await refreshTokenIfNeeded();
    }
  };

  const checkAuth = async (signal?: AbortSignal) => {
    try {
      const token = getStorageItem('accessToken');
      if (!token) {
        setIsLoading(false);
        return;
      }

      const response = await fetch('/api/auth/me', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
        signal,
      });

      if (response.ok) {
        const userData = await response.json();
        setUser(userData);
      } else if (response.status === 401) {
        // Token might be expired, try to refresh
        await refreshTokenIfNeeded();
      } else {
        removeStorageItem('accessToken');
        removeStorageItem('refreshToken');
      }
    } catch (error) {
      // Don't log abort errors as they are intentional
      if (error instanceof Error && error.name !== 'AbortError') {
        console.error('Auth check failed:', error);
        removeStorageItem('accessToken');
        removeStorageItem('refreshToken');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (response.ok) {
        const data = await response.json();
        setStorageItem('accessToken', data.accessToken);
        setStorageItem('refreshToken', data.refreshToken);
        setUser(data.user);
        
        // Use setTimeout to ensure navigation happens after state update
        setTimeout(() => {
          router.push('/');
        }, 0);
        
        return { success: true };
      } else {
        // Try to get the error message from the response
        try {
          const errorData = await response.json();
          return { success: false, error: errorData.message || 'Login failed. Please try again.' };
        } catch (parseError) {
          // If we can't parse the response, use a default message
          if (response.status === 401) {
            return { success: false, error: 'Invalid email or password' };
          }
          return { success: false, error: 'Login failed. Please try again.' };
        }
      }
    } catch (error) {
      console.error('Login failed:', error);
      // Network error likely means backend is offline
      return { success: false, error: 'Unable to connect to server. Please check if the backend is running.' };
    }
  };

  const logout = async () => {
    // Clear auth state immediately to trigger layout change
    setUser(null);
    
    try {
      const token = getStorageItem('accessToken');
      const refreshToken = getStorageItem('refreshToken');
      removeStorageItem('accessToken');
      removeStorageItem('refreshToken');
      
      // Use setTimeout to ensure navigation happens after state update
      setTimeout(() => {
        router.replace('/login');
      }, 0);
      
      // Call logout API in background (non-blocking)
      if (token && refreshToken) {
        fetch('/api/auth/logout', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'X-Refresh-Token': refreshToken,
          },
        }).catch(error => {
          console.error('Logout API call failed:', error);
        });
      }
    } catch (error) {
      console.error('Logout failed:', error);
      // Even if logout fails, ensure user is redirected
      setTimeout(() => {
        router.replace('/login');
      }, 0);
    }
  };

  const refreshUser = async () => {
    const token = getStorageItem('accessToken');
    if (!token) return;

    try {
      const response = await fetch('/api/auth/me', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const userData = await response.json();
        setUser(userData);
      }
    } catch (error) {
      console.error('Failed to refresh user data:', error);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        logout,
        refreshUser,
        isLoading,
        isAuthenticated,
        isAdmin,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}