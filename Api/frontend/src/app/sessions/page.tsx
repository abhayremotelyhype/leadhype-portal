'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/auth-context';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { format, formatDistanceToNow } from 'date-fns';
import { 
  Laptop, 
  Smartphone, 
  Monitor, 
  LogOut, 
  Shield, 
  MapPin, 
  Clock, 
  Calendar,
  Trash2,
  AlertTriangle,
  CheckCircle2,
  Globe,
  Wifi,
  Activity,
  Lock,
  Zap,
  Star,
  TabletSmartphone,
  Chrome,
  Apple
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { PageHeader } from '@/components/page-header';
import { toast } from 'sonner';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';

interface Session {
  id: string;
  deviceName: string | null;
  ipAddress: string | null;
  createdAt: string;
  lastAccessedAt: string;
  isCurrent: boolean;
}

export default function SessionsPage() {
  const { user, logout } = useAuth();
  const router = useRouter();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [loading, setLoading] = useState(true);
  const [revoking, setRevoking] = useState<string | null>(null);
  const [showLogoutAllDialog, setShowLogoutAllDialog] = useState(false);

  // Set page title
  useEffect(() => {
    document.title = 'Sessions - LeadHype';
  }, []);

  useEffect(() => {
    if (!user) {
      router.push('/login');
      return;
    }
    fetchSessions();
  }, [user, router]);

  const fetchSessions = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch('/api/auth/sessions', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'X-Refresh-Token': localStorage.getItem('refreshToken') || '',
        },
      });
      
      if (response.ok) {
        const data = await response.json();
        setSessions(data);
      } else {
        toast.error('Failed to load sessions');
      }
    } catch (error) {
      console.error('Failed to fetch sessions:', error);
      toast.error('Failed to load sessions');
    } finally {
      setLoading(false);
    }
  };

  const revokeSession = async (sessionId: string) => {
    setRevoking(sessionId);
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/auth/sessions/${sessionId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });
      
      if (response.ok) {
        toast.success('Session revoked successfully');
        fetchSessions();
      } else {
        toast.error('Failed to revoke session');
      }
    } catch (error) {
      console.error('Failed to revoke session:', error);
      toast.error('Failed to revoke session');
    } finally {
      setRevoking(null);
    }
  };

  const logoutAll = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch('/api/auth/logout-all', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });
      
      if (response.ok) {
        toast.success('All sessions logged out successfully');
        logout();
      } else {
        toast.error('Failed to logout all sessions');
      }
    } catch (error) {
      console.error('Failed to logout all sessions:', error);
      toast.error('Failed to logout all sessions');
    } finally {
      setShowLogoutAllDialog(false);
    }
  };

  const getDeviceInfo = (deviceName: string | null) => {
    if (!deviceName) {
      return {
        icon: Monitor,
        type: 'Unknown',
        platform: 'Unknown',
        color: 'from-slate-500 to-slate-600',
        bgColor: 'bg-gradient-to-br from-slate-50 to-slate-100',
        textColor: 'text-slate-700',
        borderColor: 'border-slate-200'
      };
    }
    
    const name = deviceName.toLowerCase();
    
    // iOS Devices
    if (name.includes('iphone')) {
      return {
        icon: Smartphone,
        type: 'iPhone',
        platform: 'iOS',
        color: 'from-blue-500 to-cyan-500',
        bgColor: 'bg-gradient-to-br from-blue-50 to-cyan-50',
        textColor: 'text-blue-700',
        borderColor: 'border-blue-200'
      };
    } else if (name.includes('ipad')) {
      return {
        icon: TabletSmartphone,
        type: 'iPad',
        platform: 'iPadOS',
        color: 'from-purple-500 to-pink-500',
        bgColor: 'bg-gradient-to-br from-purple-50 to-pink-50',
        textColor: 'text-purple-700',
        borderColor: 'border-purple-200'
      };
    } else if (name.includes('mac') || name.includes('darwin')) {
      return {
        icon: Apple,
        type: 'Mac',
        platform: 'macOS',
        color: 'from-gray-600 to-gray-800',
        bgColor: 'bg-gradient-to-br from-gray-50 to-gray-100',
        textColor: 'text-gray-700',
        borderColor: 'border-gray-200'
      };
    }
    
    // Android Devices
    else if (name.includes('android')) {
      if (name.includes('tablet')) {
        return {
          icon: TabletSmartphone,
          type: 'Android Tablet',
          platform: 'Android',
          color: 'from-green-500 to-lime-500',
          bgColor: 'bg-gradient-to-br from-green-50 to-lime-50',
          textColor: 'text-green-700',
          borderColor: 'border-green-200'
        };
      } else {
        return {
          icon: Smartphone,
          type: 'Android',
          platform: 'Android',
          color: 'from-green-500 to-lime-500',
          bgColor: 'bg-gradient-to-br from-green-50 to-lime-50',
          textColor: 'text-green-700',
          borderColor: 'border-green-200'
        };
      }
    }
    
    // Windows Devices
    else if (name.includes('windows') || name.includes('win32') || name.includes('win64')) {
      return {
        icon: Monitor,
        type: 'Windows',
        platform: 'Windows',
        color: 'from-blue-600 to-blue-800',
        bgColor: 'bg-gradient-to-br from-blue-50 to-blue-100',
        textColor: 'text-blue-700',
        borderColor: 'border-blue-200'
      };
    }
    
    // Linux Devices
    else if (name.includes('linux') || name.includes('ubuntu') || name.includes('debian') || name.includes('fedora')) {
      return {
        icon: Monitor,
        type: 'Linux',
        platform: 'Linux',
        color: 'from-orange-500 to-red-500',
        bgColor: 'bg-gradient-to-br from-orange-50 to-red-50',
        textColor: 'text-orange-700',
        borderColor: 'border-orange-200'
      };
    }
    
    // Chrome OS
    else if (name.includes('chrome') || name.includes('chromebook')) {
      return {
        icon: Chrome,
        type: 'Chromebook',
        platform: 'Chrome OS',
        color: 'from-yellow-500 to-orange-500',
        bgColor: 'bg-gradient-to-br from-yellow-50 to-orange-50',
        textColor: 'text-yellow-700',
        borderColor: 'border-yellow-200'
      };
    }
    
    // Generic fallbacks
    else if (name.includes('phone') || name.includes('mobile')) {
      return {
        icon: Smartphone,
        type: 'Mobile',
        platform: 'Mobile',
        color: 'from-blue-500 to-cyan-500',
        bgColor: 'bg-gradient-to-br from-blue-50 to-cyan-50',
        textColor: 'text-blue-700',
        borderColor: 'border-blue-200'
      };
    } else if (name.includes('tablet')) {
      return {
        icon: TabletSmartphone,
        type: 'Tablet',
        platform: 'Tablet',
        color: 'from-purple-500 to-pink-500',
        bgColor: 'bg-gradient-to-br from-purple-50 to-pink-50',
        textColor: 'text-purple-700',
        borderColor: 'border-purple-200'
      };
    } else {
      return {
        icon: Laptop,
        type: 'Desktop',
        platform: 'Desktop',
        color: 'from-emerald-500 to-teal-500',
        bgColor: 'bg-gradient-to-br from-emerald-50 to-teal-50',
        textColor: 'text-emerald-700',
        borderColor: 'border-emerald-200'
      };
    }
  };

  const getSecurityLevel = (session: Session) => {
    const now = new Date();
    const lastAccess = new Date(session.lastAccessedAt);
    const hoursSinceAccess = (now.getTime() - lastAccess.getTime()) / (1000 * 60 * 60);
    
    if (session.isCurrent) {
      return { level: 'Active', className: 'bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20 hover:bg-green-500/20 hover:text-green-800 dark:hover:text-green-300', icon: <Activity className="w-3 h-3" /> };
    } else if (hoursSinceAccess < 24) {
      return { level: 'Recent', className: 'bg-blue-500/10 text-blue-700 dark:text-blue-400 border-blue-500/20 hover:bg-blue-500/20 hover:text-blue-800 dark:hover:text-blue-300', icon: <Clock className="w-3 h-3" /> };
    } else if (hoursSinceAccess < 168) { // 7 days
      return { level: 'Idle', className: 'bg-amber-500/10 text-amber-700 dark:text-amber-400 border-amber-500/20 hover:bg-amber-500/20 hover:text-amber-800 dark:hover:text-amber-300', icon: <Clock className="w-3 h-3" /> };
    } else {
      return { level: 'Stale', className: 'bg-red-500/10 text-red-700 dark:text-red-400 border-red-500/20 hover:bg-red-500/20 hover:text-red-800 dark:hover:text-red-300', icon: <AlertTriangle className="w-3 h-3" /> };
    }
  };

  const getLocationFromIP = (ipAddress: string | null) => {
    if (!ipAddress) return 'Unknown location';
    // This would typically be replaced with actual geolocation data
    return 'Location based on IP';
  };

  if (loading) {
    return (
      <div className="flex h-full flex-col bg-gradient-to-br from-background via-background to-muted/20">
        <div className="space-y-2 p-6">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-4 w-72" />
        </div>
        
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-6xl mx-auto space-y-8">
            {/* Current Session Skeleton */}
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <Skeleton className="w-2 h-2 rounded-full" />
                <Skeleton className="h-6 w-32" />
                <Skeleton className="h-6 w-20 rounded-full" />
              </div>
              
              <Card>
                <CardHeader className="pb-4">
                  <div className="flex items-start justify-between">
                    <div className="flex items-start space-x-3 sm:space-x-6">
                      <div className="relative flex items-center justify-center w-10 h-10 sm:w-12 sm:h-12">
                        <Skeleton className="w-6 h-6 sm:w-8 sm:h-8 rounded" />
                        <Skeleton className="absolute -top-1 -right-1 w-4 h-4 rounded-full" />
                      </div>
                      
                      <div className="flex-1 min-w-0 space-y-3">
                        <div className="flex items-center gap-2 mb-2">
                          <Skeleton className="h-6 w-40" />
                          <Skeleton className="h-6 w-16 rounded-full" />
                        </div>
                        
                        <div className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2 lg:grid-cols-3">
                          <div className="flex items-center gap-2">
                            <Skeleton className="w-4 h-4 rounded" />
                            <div className="space-y-1">
                              <Skeleton className="h-3 w-16" />
                              <Skeleton className="h-3 w-24" />
                            </div>
                          </div>
                          
                          <div className="flex items-center gap-3">
                            <Skeleton className="w-4 h-4 rounded" />
                            <div className="space-y-1">
                              <Skeleton className="h-3 w-16" />
                              <Skeleton className="h-3 w-20" />
                            </div>
                          </div>
                          
                          <div className="flex items-center gap-3">
                            <Skeleton className="w-4 h-4 rounded" />
                            <div className="space-y-1">
                              <Skeleton className="h-3 w-20" />
                              <Skeleton className="h-3 w-16" />
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex gap-1 sm:gap-2 flex-wrap">
                      <Skeleton className="h-6 w-16 rounded-full" />
                      <Skeleton className="h-6 w-20 rounded-full" />
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </div>

            {/* Other Sessions Skeleton */}
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Skeleton className="w-2 h-2 rounded-full" />
                  <Skeleton className="h-6 w-32" />
                  <Skeleton className="h-6 w-20 rounded-full" />
                </div>
              </div>
              
              <div className="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
                {[...Array(2)].map((_, i) => (
                  <Card key={i}>
                    <CardHeader className="pb-3">
                      <div className="flex items-start justify-between">
                        <div className="flex items-start space-x-2 sm:space-x-3 flex-1 min-w-0">
                          <Skeleton className="w-10 h-10 rounded-lg" />
                          
                          <div className="flex-1 min-w-0 space-y-3">
                            <div className="flex items-center gap-1 sm:gap-2 mb-1">
                              <Skeleton className="h-4 w-32" />
                            </div>
                            
                            <div className="flex items-center gap-1 sm:gap-2 mb-2 flex-wrap">
                              <Skeleton className="h-5 w-16 rounded-full" />
                              <Skeleton className="h-5 w-20 rounded-full" />
                            </div>
                            
                            <div className="space-y-1">
                              <div className="flex items-center gap-1">
                                <Skeleton className="w-3 h-3 rounded" />
                                <Skeleton className="h-3 w-24" />
                              </div>
                              <div className="flex items-center gap-1">
                                <Skeleton className="w-3 h-3 rounded" />
                                <Skeleton className="h-3 w-20" />
                              </div>
                            </div>
                          </div>
                        </div>
                        
                        <Skeleton className="h-7 w-7 rounded ml-1 sm:ml-2" />
                      </div>
                    </CardHeader>
                  </Card>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const currentSession = sessions.find(s => s.isCurrent);
  const otherSessions = sessions.filter(s => !s.isCurrent);

  return (
    <div className="flex h-full flex-col bg-gradient-to-br from-background via-background to-muted/20">
      <PageHeader 
        title="Security Center"
        description="Monitor and manage your active sessions across all devices"
        mobileDescription="Session security"
        icon={Shield}
        itemCount={sessions.length}
        itemLabel="active sessions"
        actions={
          otherSessions.length > 0 && (
            <Button 
              variant="destructive" 
              size="sm"
              onClick={() => setShowLogoutAllDialog(true)}
              className="bg-gradient-to-r from-red-500 to-red-600 hover:from-red-600 hover:to-red-700 shadow-lg h-8 w-8 sm:w-auto p-0 sm:px-3"
            >
              <LogOut className="w-4 h-4 sm:mr-2" />
              <span className="hidden sm:inline">Log Out All Devices</span>
            </Button>
          )
        }
      />

      <div className="flex-1 overflow-auto p-6">
        {sessions.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <div className="relative mb-8">
              <div className="w-24 h-24 bg-gradient-to-br from-primary/10 to-primary/5 rounded-full flex items-center justify-center mb-4 shadow-lg">
                <Shield className="w-12 h-12 text-primary" />
              </div>
              <div className="absolute -top-2 -right-2 w-8 h-8 bg-green-500 rounded-full flex items-center justify-center">
                <Lock className="w-4 h-4 text-white" />
              </div>
            </div>
            <h3 className="text-2xl font-bold mb-3 bg-gradient-to-r from-foreground to-muted-foreground bg-clip-text text-transparent">
              All Secure
            </h3>
            <p className="text-muted-foreground max-w-md text-lg">
              No active sessions found. Your account security is fully protected.
            </p>
          </div>
        ) : (
          <div className="max-w-6xl mx-auto space-y-8">
            {/* Current Session */}
            {currentSession && (
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
                  <h2 className="text-xl font-semibold text-foreground">Current Session</h2>
                  <Badge className="bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400 border-green-200 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/30 hover:text-green-800 dark:hover:text-green-400 pointer-events-none">
                    <Zap className="w-3 h-3 mr-1" />
                    Active Now
                  </Badge>
                </div>
                
                {(() => {
                  const deviceInfo = getDeviceInfo(currentSession.deviceName);
                  const securityLevel = getSecurityLevel(currentSession);
                  
                  return (
                    <Card>
                      
                      <CardHeader className="pb-4">
                        <div className="flex items-start justify-between">
                          <div className="flex items-start space-x-3 sm:space-x-6">
                            <div className="relative flex items-center justify-center w-10 h-10 sm:w-12 sm:h-12">
                              <div className={`w-6 h-6 sm:w-8 sm:h-8 text-${deviceInfo.textColor.split('-')[1]}-600 flex items-center justify-center`}>
                                <deviceInfo.icon className="h-6 w-6 sm:h-8 sm:w-8" />
                              </div>
                              <div className="absolute -top-1 -right-1 w-4 h-4 bg-green-500 rounded-full border-2 border-white flex items-center justify-center">
                                <Activity className="w-2 h-2 text-white" />
                              </div>
                            </div>
                            
                            <div className="flex-1 min-w-0 space-y-3">
                              <div>
                                <div className="flex items-center gap-2 mb-2">
                                  <CardTitle className="text-lg sm:text-xl font-bold truncate">
                                    {currentSession.deviceName || 'Unknown Device'}
                                  </CardTitle>
                                  <Badge 
                                    variant="outline"
                                    className={`px-3 py-1 ${securityLevel.className} pointer-events-none`}
                                  >
                                    {securityLevel.icon}
                                    <span className="ml-1 font-semibold">{securityLevel.level}</span>
                                  </Badge>
                                </div>
                                
                                <div className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2 lg:grid-cols-3">
                                  <div className="flex items-center gap-2 text-muted-foreground">
                                    <Globe className="w-3 h-3 sm:w-4 sm:h-4 text-blue-600 flex-shrink-0" />
                                    <div>
                                      <div className="font-medium text-foreground text-xs sm:text-sm">Location</div>
                                      <div className="text-xs truncate">{currentSession.ipAddress || 'Unknown IP'}</div>
                                    </div>
                                  </div>
                                  
                                  <div className="flex items-center gap-3 text-muted-foreground">
                                    <Calendar className="w-3 h-3 sm:w-4 sm:h-4 text-purple-600 flex-shrink-0" />
                                    <div>
                                      <div className="font-medium text-foreground text-xs sm:text-sm">Signed In</div>
                                      <div className="text-xs">{format(new Date(currentSession.createdAt), 'MMM d, yyyy')}</div>
                                    </div>
                                  </div>
                                  
                                  <div className="flex items-center gap-3 text-muted-foreground">
                                    <Activity className="w-3 h-3 sm:w-4 sm:h-4 text-green-600 flex-shrink-0" />
                                    <div>
                                      <div className="font-medium text-foreground text-xs sm:text-sm">Last Active</div>
                                      <div className="text-xs hover:bg-transparent cursor-default" title={format(new Date(currentSession.lastAccessedAt), 'PPpp')}>
                                        {formatDistanceToNow(new Date(currentSession.lastAccessedAt), { addSuffix: true })}
                                      </div>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                          
                          <div className="flex gap-1 sm:gap-2 flex-wrap">
                            <Badge variant="outline" className="px-2 py-1 text-xs">
                              <Star className="w-3 h-3 mr-1 hidden sm:inline" />
                              {deviceInfo.type}
                            </Badge>
                            {deviceInfo.platform !== deviceInfo.type && (
                              <Badge variant="secondary" className="px-2 py-1 text-xs hidden sm:inline-flex">
                                {deviceInfo.platform}
                              </Badge>
                            )}
                          </div>
                        </div>
                      </CardHeader>
                    </Card>
                  );
                })()}
              </div>
            )}

            {/* Other Sessions */}
            {otherSessions.length > 0 && (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-2 h-2 bg-amber-500 rounded-full"></div>
                    <h2 className="text-xl font-semibold text-foreground">Other Sessions</h2>
                    <Badge variant="outline" className="text-muted-foreground">
                      {otherSessions.length} {otherSessions.length === 1 ? 'session' : 'sessions'}
                    </Badge>
                  </div>
                </div>
                
                <div className="grid gap-4 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
                  {otherSessions.map((session) => {
                    const deviceInfo = getDeviceInfo(session.deviceName);
                    const securityLevel = getSecurityLevel(session);
                    
                    return (
                      <Card key={session.id} className="group transition-all duration-300">
                        <CardHeader className="pb-3">
                          <div className="flex items-start justify-between">
                            <div className="flex items-start space-x-2 sm:space-x-3 flex-1 min-w-0">
                              <div className="relative flex items-center justify-center w-10 h-10">
                                <div className={`w-6 h-6 text-${deviceInfo.textColor.split('-')[1]}-600 flex items-center justify-center`}>
                                  <deviceInfo.icon className="h-6 w-6" />
                                </div>
                              </div>
                              
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-1 sm:gap-2 mb-1">
                                  <CardTitle className="text-sm sm:text-base font-semibold truncate">
                                    {session.deviceName || 'Unknown Device'}
                                  </CardTitle>
                                </div>
                                
                                <div className="flex items-center gap-1 sm:gap-2 mb-2 flex-wrap">
                                  <Badge 
                                    variant="outline"
                                    className={`text-xs px-2 py-0.5 ${securityLevel.className} pointer-events-none`}
                                  >
                                    {securityLevel.icon}
                                    <span className="ml-1">{securityLevel.level}</span>
                                  </Badge>
                                  <Badge variant="outline" className="text-xs px-1.5 py-0.5">
                                    {deviceInfo.type}
                                  </Badge>
                                  {deviceInfo.platform !== deviceInfo.type && (
                                    <Badge variant="secondary" className="text-xs px-1.5 py-0.5 hidden sm:inline-flex">
                                      {deviceInfo.platform}
                                    </Badge>
                                  )}
                                </div>
                                
                                <div className="space-y-1 text-xs text-muted-foreground">
                                  <div className="flex items-center gap-1">
                                    <MapPin className="w-3 h-3" />
                                    <span className="truncate">{session.ipAddress || 'Unknown IP'}</span>
                                  </div>
                                  <div className="flex items-center gap-1">
                                    <Clock className="w-3 h-3" />
                                    <span className="hover:bg-transparent cursor-default" title={format(new Date(session.lastAccessedAt), 'PPpp')}>
                                      {formatDistanceToNow(new Date(session.lastAccessedAt), { addSuffix: true })}
                                    </span>
                                  </div>
                                </div>
                              </div>
                            </div>
                            
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => revokeSession(session.id)}
                              disabled={revoking === session.id}
                              className="ml-1 sm:ml-2 opacity-70 sm:opacity-0 group-hover:opacity-100 transition-opacity duration-200 text-destructive hover:text-destructive hover:bg-destructive/10 hover:border-destructive/20 h-7 w-7 sm:h-8 sm:w-8 p-0 flex-shrink-0"
                            >
                              {revoking === session.id ? (
                                <div className="w-3 h-3 animate-spin rounded-full border-2 border-current border-t-transparent" />
                              ) : (
                                <Trash2 className="w-3 h-3" />
                              )}
                            </Button>
                          </div>
                        </CardHeader>
                      </Card>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      <AlertDialog open={showLogoutAllDialog} onOpenChange={setShowLogoutAllDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Log Out All Devices?</AlertDialogTitle>
            <AlertDialogDescription>
              This will log you out of LeadHype on all your devices (phone, computer, tablet, etc.), 
              including this current device. You&apos;ll need to log in again wherever you want to use LeadHype.
            </AlertDialogDescription>
            
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 flex items-start gap-3 mt-4">
              <AlertTriangle className="w-5 h-5 text-amber-600 mt-0.5 flex-shrink-0" />
              <div className="text-sm">
                <div className="font-medium text-amber-900 mb-1">When to use this:</div>
                <div className="text-amber-700">
                  • Someone else might know your password<br/>
                  • You lost your phone or laptop<br/>
                  • You used a public computer and forgot to log out
                </div>
              </div>
            </div>
          </AlertDialogHeader>
          
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={logoutAll} className="bg-destructive hover:bg-destructive/90">
              <LogOut className="w-4 h-4 mr-2" />
              Log Out All Devices
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}