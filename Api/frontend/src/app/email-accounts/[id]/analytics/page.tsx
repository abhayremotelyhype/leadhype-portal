'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ArrowLeft, Mail, TrendingUp, BarChart3, Calendar, Download, Eye, MousePointer, Reply, UserMinus, XCircle, Clock, RefreshCw, CheckCircle, User, Megaphone, LineChart as LineChartIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useToast } from '@/hooks/use-toast';
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from '@/components/ui/chart';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, ResponsiveContainer, BarChart, Bar } from 'recharts';
import { CampaignsModal } from '@/components/campaigns-modal';

interface EmailAccountAnalytics {
  id: string;
  email: string;
  name?: string;
  status: 'Active' | 'Inactive' | 'Warming';
  totalSent: number;
  totalOpened: number;
  totalClicked: number;
  totalReplied: number;
  totalBounced: number;
  totalUnsubscribed: number;
  openRate: number;
  clickRate: number;
  replyRate: number;
  bounceRate: number;
  unsubscribeRate: number;
  createdAt: string;
  updatedAt: string;
  assignedClient?: string;
  // Warmup metrics
  warmupSent: number;
  warmupReplied: number;
  warmupSpamCount: number;
  savedFromSpam: number;
  warmupReplyRate: number;
  spamProtectionRate: number;
  dailyStats: Array<{
    date: string;
    sent: number;
    opened: number;
    clicked: number;
    replied: number;
    bounced: number;
    unsubscribed: number;
  }>;
  warmupStats: Array<{
    date: string;
    warmupSent: number;
    warmupReplied: number;
    savedFromSpam: number;
  }>;
}

export default function EmailAccountAnalyticsPage() {
  const params = useParams();
  const router = useRouter();
  const { toast } = useToast();
  const [analytics, setAnalytics] = useState<EmailAccountAnalytics | null>(null);
  const [originalAnalytics, setOriginalAnalytics] = useState<EmailAccountAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [performanceChartLoading, setPerformanceChartLoading] = useState(false);
  const [warmupChartLoading, setWarmupChartLoading] = useState(false);
  const [warmupChartVersion, setWarmupChartVersion] = useState(0);
  const [performanceTimeRange, setPerformanceTimeRange] = useState('all');
  const [warmupTimeRange, setWarmupTimeRange] = useState('all');
  const [performanceChartType, setPerformanceChartType] = useState<'bar' | 'line'>('line');
  const [warmupChartType, setWarmupChartType] = useState<'bar' | 'line'>('line');
  const [showCampaignsModal, setShowCampaignsModal] = useState(false);
  const hasFetchedRef = useRef(false);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Chart configurations - Official shadcn/ui format
  const performanceChartConfig = {
    sent: {
      label: "Sent",
      color: "hsl(var(--chart-1))",
    },
    opened: {
      label: "Opened", 
      color: "hsl(var(--chart-2))",
    },
    clicked: {
      label: "Clicked",
      color: "hsl(var(--chart-3))",
    },
    replied: {
      label: "Replied",
      color: "hsl(var(--chart-4))",
    },
    bounced: {
      label: "Bounced",
      color: "hsl(var(--chart-5))",
    },
    unsubscribed: {
      label: "Unsubscribed",
      color: "hsl(var(--destructive))",
    },
  } satisfies ChartConfig;

  const warmupChartConfig = {
    warmupSent: {
      label: "Warmup Sent",
      color: "hsl(var(--chart-1))",
    },
    warmupReplied: {
      label: "Warmup Replied",
      color: "hsl(var(--chart-2))",
    },
    savedFromSpam: {
      label: "Saved from Spam",
      color: "hsl(var(--chart-3))",
    },
  } satisfies ChartConfig;

  useEffect(() => {
    // Don't run if already fetched (prevents StrictMode double call)
    if (hasFetchedRef.current) return;
    
    // Cancel any ongoing requests from previous renders
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    
    // Create new abort controller for this request
    abortControllerRef.current = new AbortController();
    
    // Mark as fetched to prevent duplicate calls
    hasFetchedRef.current = true;
    
    loadAnalytics();
    
    return () => {
      // Cleanup: cancel ongoing requests when component unmounts
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      // Reset flag when component unmounts or ID changes
      hasFetchedRef.current = false;
    };
  }, [params.id]);

  useEffect(() => {
    // Update performance chart data when performance time range changes
    if (originalAnalytics) {
      updatePerformanceChartData();
    }
  }, [performanceTimeRange]);

  useEffect(() => {
    // Update warmup chart data when warmup time range changes
    if (originalAnalytics) {
      updateWarmupChartData();
    }
  }, [warmupTimeRange]);

  const getDateRangeForTimeRange = (timeRange: string) => {
    const endDate = new Date();
    const startDate = new Date();
    
    const days = timeRange === '7d' ? 7 : 
                 timeRange === '2w' ? 14 : 
                 timeRange === '1m' ? 30 : 
                 timeRange === '3m' ? 90 : 
                 timeRange === '6m' ? 180 : 
                 timeRange === '1y' ? 365 : 
                 timeRange === 'all' ? 365 : 30; // Default to 30 days or max 365 for 'all'
    
    if (timeRange !== 'all') {
      startDate.setDate(startDate.getDate() - days);
    } else {
      startDate.setDate(startDate.getDate() - 365); // For 'all', get max 1 year of data
    }
    
    return {
      startDate: startDate.toISOString().split('T')[0],
      endDate: endDate.toISOString().split('T')[0]
    };
  };

  const filterDataByTimeRange = (data: any[], timeRange: string) => {
    if (!data || data.length === 0) return data;
    
    const days = timeRange === '7d' ? 7 : 
                 timeRange === '2w' ? 14 : 
                 timeRange === '1m' ? 30 : 
                 timeRange === '3m' ? 90 : 
                 timeRange === '6m' ? 180 : 
                 timeRange === '1y' ? 365 : 
                 timeRange === 'all' ? data.length : 30;
    
    if (timeRange === 'all') return data;
    
    // Get the last N days of data
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - days);
    
    return data.filter(item => new Date(item.date) >= cutoffDate);
  };


  const fetchDailyStatsData = async (timeRange?: string, signal?: AbortSignal) => {
    // Get access token for authentication
    const token = localStorage.getItem('accessToken');
    
    // Get date range based on time range selection
    const dateRange = timeRange ? getDateRangeForTimeRange(timeRange) : getDateRangeForTimeRange('all');
    
    // Fetch daily stats from the endpoint with date parameters
    const dailyStatsHeaders = {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
    };
    
    const dailyStatsUrl = `/api/email-accounts/${params.id}/daily-stats?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}`;
    console.log('Daily stats request:', { url: dailyStatsUrl, timeRange });
    
    const dailyStatsResponse = await fetch(dailyStatsUrl, {
      method: 'GET',
      headers: dailyStatsHeaders,
      signal,
    });

    let dailyStatsData = null;
    if (dailyStatsResponse.ok) {
      const contentType = dailyStatsResponse.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        dailyStatsData = await dailyStatsResponse.json();
        console.log('Daily stats data received:', dailyStatsData);
      } else {
        console.error('Unexpected content type for daily stats:', contentType);
      }
    } else {
      console.error('Daily stats fetch failed:', dailyStatsResponse.status);
    }

    // Process daily stats
    let dailyStats = [];
    console.log('Processing daily stats data:', { dailyStatsData, hasData: !!dailyStatsData });
    
    if (dailyStatsData?.dailyStats && dailyStatsData.dailyStats.length > 0) {
      // Handle nested structure: { dailyStats: [...] }
      console.log('Using nested dailyStats structure');
      dailyStats = dailyStatsData.dailyStats.map((stat: any) => ({
        date: stat.date,
        sent: stat.sent || 0,
        opened: stat.opened || 0,
        clicked: stat.clicked || 0,
        replied: stat.replied || 0,
        bounced: stat.bounced || 0,
        unsubscribed: stat.unsubscribed || 0,
      }));
    } else if (dailyStatsData && Array.isArray(dailyStatsData)) {
      // Handle direct array structure: [...]
      console.log('Using direct array structure');
      dailyStats = dailyStatsData.map((stat: any) => ({
        date: stat.date,
        sent: stat.sent || 0,
        opened: stat.opened || 0,
        clicked: stat.clicked || 0,
        replied: stat.replied || 0,
        bounced: stat.bounced || 0,
        unsubscribed: stat.unsubscribed || 0,
      }));
    } else {
      console.warn('No daily stats data found or unexpected format:', dailyStatsData);
      // Generate empty data points for the chart to display
      const daysToShow = 7;
      for (let i = daysToShow - 1; i >= 0; i--) {
        const date = new Date();
        date.setDate(date.getDate() - i);
        dailyStats.push({
          date: date.toISOString().split('T')[0],
          sent: 0,
          opened: 0,
          clicked: 0,
          replied: 0,
          bounced: 0,
          unsubscribed: 0,
        });
      }
    }

    console.log('Final processed daily stats:', { count: dailyStats.length, sample: dailyStats[0] });
    return { dailyStats, dailyStatsData };
  };

  const fetchWarmupStatsData = async (timeRange?: string, signal?: AbortSignal) => {
    // Get access token for authentication
    const token = localStorage.getItem('accessToken');
    
    // Get date range based on time range selection
    const dateRange = timeRange ? getDateRangeForTimeRange(timeRange) : getDateRangeForTimeRange('all');
    
    // Fetch warmup daily stats from V1 endpoint with date parameters
    let warmupDailyStats = [];
    try {
      const warmupDailyStatsUrl = `/api/v1/email-accounts/${params.id}/warmup/daily-stats?startDate=${dateRange.startDate}&endDate=${dateRange.endDate}`;
      console.log('Warmup stats request:', { url: warmupDailyStatsUrl, timeRange, hasToken: !!token });
      
      const warmupDailyStatsResponse = await fetch(warmupDailyStatsUrl, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          ...(token && { Authorization: `Bearer ${token}` }),
        },
        signal,
      });
      
      if (warmupDailyStatsResponse.ok) {
        const warmupDailyStatsData = await warmupDailyStatsResponse.json();
        console.log('Warmup API response:', { 
          hasData: !!warmupDailyStatsData, 
          dataStructure: warmupDailyStatsData ? Object.keys(warmupDailyStatsData) : 'null',
          dataArray: warmupDailyStatsData?.data?.length || 'no data array',
          timeRange,
          sample: warmupDailyStatsData?.data?.[0]
        });
        
        if (warmupDailyStatsData?.data && Array.isArray(warmupDailyStatsData.data)) {
          warmupDailyStats = warmupDailyStatsData.data.map((stat: any) => ({
            date: stat.date,
            warmupSent: stat.sent || 0,
            warmupReplied: stat.replied || 0,
            savedFromSpam: stat.savedFromSpam || 0,
          }));
          console.log('Warmup daily stats processed:', {
            count: warmupDailyStats.length,
            timeRange,
            sample: warmupDailyStats[0],
            totalSent: warmupDailyStats.reduce((sum: number, s: any) => sum + s.warmupSent, 0)
          });
        } else {
          console.warn('Warmup data not in expected format:', warmupDailyStatsData);
        }
      } else {
        console.warn('Failed to fetch warmup daily stats:', {
          status: warmupDailyStatsResponse.status,
          statusText: warmupDailyStatsResponse.statusText,
          hasToken: !!token,
          url: warmupDailyStatsUrl
        });
      }
    } catch (error) {
      console.error('Error fetching warmup daily stats:', error);
    }

    return warmupDailyStats;
  };

  const fetchAnalyticsData = async (timeRange?: string, signal?: AbortSignal): Promise<EmailAccountAnalytics> => {
    // Get access token for authentication
    const token = localStorage.getItem('accessToken');
    console.log('Token for daily stats request:', token ? `${token.substring(0, 20)}...` : 'No token found');
    
    // Fetch email account data
    const accountResponse = await fetch(`/api/email-accounts/by-id/${params.id}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      signal,
    });

    if (!accountResponse.ok) {
      throw new Error(`Failed to fetch account data: ${accountResponse.statusText}`);
    }

    const accountData = await accountResponse.json();

    // Fetch warmup metrics
    const warmupResponse = await fetch(`/api/email-accounts/${params.id}/warmup-metrics`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      signal,
    });

    let warmupData = null;
    if (warmupResponse.ok) {
      warmupData = await warmupResponse.json();
    }

    // Fetch daily stats and warmup stats
    const { dailyStats, dailyStatsData } = await fetchDailyStatsData(timeRange, signal);
    const warmupDailyStats = await fetchWarmupStatsData(timeRange, signal);

    // Calculate rates
    const totalSent = accountData.sent || 0;
    const totalOpened = accountData.opened || 0;
    const totalClicked = accountData.clicked || 0;
    const totalReplied = accountData.replied || 0;
    const totalBounced = accountData.bounced || 0;
    const totalUnsubscribed = accountData.unsubscribed || 0;

    const openRate = totalSent > 0 ? (totalOpened / totalSent * 100) : 0;
    const clickRate = totalSent > 0 ? (totalClicked / totalSent * 100) : 0;
    const replyRate = totalSent > 0 ? (totalReplied / totalSent * 100) : 0;
    const bounceRate = totalSent > 0 ? (totalBounced / totalSent * 100) : 0;
    const unsubscribeRate = totalSent > 0 ? (totalUnsubscribed / totalSent * 100) : 0;

    // Process warmup stats - use V1 daily stats if available, otherwise fall back to warmup metrics
    const warmupStats = [];
    if (warmupDailyStats.length > 0) {
      warmupStats.push(...warmupDailyStats);
    } else if (warmupData?.dailyStats && warmupData.dailyStats.length > 0) {
      // Use the legacy warmup metrics format as fallback
      warmupStats.push(...warmupData.dailyStats.map((stat: any) => ({
        date: stat.date,
        warmupSent: stat.sent || 0,
        warmupReplied: stat.replied || 0,
        savedFromSpam: stat.savedFromSpam || 0,
      })));
    } else {
      // If no daily stats available, generate empty data for chart display
      if (dailyStats.length === 0) {
        const daysToShow = 7;
        for (let i = daysToShow - 1; i >= 0; i--) {
          const date = new Date();
          date.setDate(date.getDate() - i);
          dailyStats.push({
            date: date.toISOString().split('T')[0],
            sent: 0,
            opened: 0,
            clicked: 0,
            replied: 0,
            bounced: 0,
            unsubscribed: 0,
          });
        }
      }
      
      // If no warmup stats available, generate empty data for chart display
      const daysToShow = 7;
      for (let i = daysToShow - 1; i >= 0; i--) {
        const date = new Date();
        date.setDate(date.getDate() - i);
        warmupStats.push({
          date: date.toISOString().split('T')[0],
          warmupSent: 0,
          warmupReplied: 0,
          savedFromSpam: 0,
        });
      }
    }

    return {
      id: accountData.id?.toString() || params.id as string,
      email: accountData.email || 'Unknown',
      name: accountData.name,
      status: accountData.status || 'Unknown',
      totalSent,
      totalOpened,
      totalClicked,
      totalReplied,
      totalBounced,
      totalUnsubscribed,
      openRate: parseFloat(openRate.toFixed(1)),
      clickRate: parseFloat(clickRate.toFixed(1)),
      replyRate: parseFloat(replyRate.toFixed(1)),
      bounceRate: parseFloat(bounceRate.toFixed(1)),
      unsubscribeRate: parseFloat(unsubscribeRate.toFixed(1)),
      createdAt: accountData.createdAt || new Date().toISOString(),
      updatedAt: accountData.updatedAt || new Date().toISOString(),
      assignedClient: accountData.clientName,
      warmupSent: warmupData?.totalSent || accountData.warmupSent || 0,
      warmupReplied: warmupData?.totalReplied || accountData.warmupReplied || 0,
      warmupSpamCount: accountData.warmupSpamCount || 0,
      savedFromSpam: warmupData?.totalSavedFromSpam || accountData.warmupSavedFromSpam || 0,
      warmupReplyRate: warmupData?.totalSent > 0 ? parseFloat(((warmupData.totalReplied / warmupData.totalSent) * 100).toFixed(1)) : 0,
      spamProtectionRate: warmupData?.totalSent > 0 ? parseFloat(((warmupData.totalSavedFromSpam / warmupData.totalSent) * 100).toFixed(1)) : 0,
      dailyStats,
      warmupStats,
    };
  };

  const loadAnalytics = async (timeRange?: string) => {
    setLoading(true);
    try {
      const analyticsData = await fetchAnalyticsData(timeRange, abortControllerRef.current?.signal);
      setOriginalAnalytics(analyticsData);
      
      // Debug logging
      console.log('Analytics data loaded:', {
        dailyStats: analyticsData.dailyStats.length,
        warmupStats: analyticsData.warmupStats.length,
        timeRange: timeRange || 'all'
      });
      
      setAnalytics(analyticsData);
    } catch (error: any) {
      // Don't show error for aborted requests
      if (error?.name === 'AbortError') {
        console.log('Request was cancelled');
        return;
      }
      
      console.error('Failed to load analytics:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load analytics data. Please check if the backend is running.',
      });
    } finally {
      setLoading(false);
    }
  };

  const updatePerformanceChartData = async () => {
    setPerformanceChartLoading(true);
    
    try {
      // Fetch only daily stats data for performance chart
      const { dailyStats } = await fetchDailyStatsData(performanceTimeRange, abortControllerRef.current?.signal);
      
      // Update only the daily stats, keep everything else unchanged
      setAnalytics(prev => prev ? { 
        ...prev, 
        dailyStats
      } : prev);
      
      console.log('Performance chart updated for time range:', performanceTimeRange, 'with', dailyStats.length, 'data points');
    } catch (error: any) {
      if (error?.name !== 'AbortError') {
        console.error('Failed to update performance chart:', error);
      }
    } finally {
      setPerformanceChartLoading(false);
    }
  };

  const updateWarmupChartData = async () => {
    setWarmupChartLoading(true);
    
    try {
      console.log('Updating warmup chart for time range:', warmupTimeRange);
      
      // Fetch only warmup stats data for warmup chart
      const warmupDailyStats = await fetchWarmupStatsData(warmupTimeRange, abortControllerRef.current?.signal);
      
      console.log('Fetched warmup data:', { 
        count: warmupDailyStats.length, 
        timeRange: warmupTimeRange,
        hasData: warmupDailyStats.length > 0,
        sample: warmupDailyStats[0]
      });
      
      // Process warmup stats
      const warmupStats: any[] = [];
      if (warmupDailyStats.length > 0) {
        warmupStats.push(...warmupDailyStats);
        console.log('Using fetched warmup data:', {
          count: warmupStats.length,
          totalSent: warmupStats.reduce((sum: number, s: any) => sum + s.warmupSent, 0),
          dateRange: `${warmupStats[0]?.date} to ${warmupStats[warmupStats.length - 1]?.date}`
        });
      } else {
        console.log('No warmup data found, generating empty data points');
        // Generate empty data for chart display
        const daysToShow = 7;
        for (let i = daysToShow - 1; i >= 0; i--) {
          const date = new Date();
          date.setDate(date.getDate() - i);
          warmupStats.push({
            date: date.toISOString().split('T')[0],
            warmupSent: 0,
            warmupReplied: 0,
            savedFromSpam: 0,
          });
        }
      }
      
      // Update only the warmup stats, keep everything else unchanged
      setAnalytics(prev => {
        const updated = prev ? { 
          ...prev, 
          warmupStats
        } : prev;
        console.log('Analytics state updated:', {
          prevWarmupCount: prev?.warmupStats?.length,
          newWarmupCount: warmupStats.length,
          timeRange: warmupTimeRange
        });
        return updated;
      });
      
      // Force chart re-render
      setWarmupChartVersion(prev => prev + 1);
      
      console.log('Warmup chart updated for time range:', warmupTimeRange, 'with', warmupStats.length, 'data points');
    } catch (error: any) {
      if (error?.name !== 'AbortError') {
        console.error('Failed to update warmup chart:', error);
      }
    } finally {
      setWarmupChartLoading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const variants = {
      Active: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
      Inactive: 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200',
      Warming: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
    };

    return (
      <span className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors ${variants[status as keyof typeof variants]}`}>
        {status}
      </span>
    );
  };

  if (loading) {
    return (
      <div className="flex flex-col h-full bg-background">
        <div className="ui-page-padding border-b border-border">
          <Button variant="ghost" size="sm" onClick={() => router.push('/email-accounts')} className="mb-4">
            <ArrowLeft className="w-4 h-4" />
          </Button>
        </div>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <div className="animate-spin w-8 h-8 border-2 border-primary border-t-transparent rounded-full mx-auto mb-4"></div>
            <p className="ui-caption">Loading analytics...</p>
          </div>
        </div>
      </div>
    );
  }

  if (!analytics) {
    return (
      <div className="flex flex-col h-full bg-background">
        <div className="ui-page-padding border-b border-border">
          <Button variant="ghost" size="sm" onClick={() => router.push('/email-accounts')}>
            <ArrowLeft className="w-4 h-4" />
          </Button>
        </div>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <p className="ui-caption">Email account not found</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="border-b pt-3 pb-3">
        <div className="flex h-12 items-center justify-between px-3">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="sm" onClick={() => router.push('/email-accounts')}>
              <ArrowLeft className="w-4 h-4" />
            </Button>
            <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center">
              <Mail className="w-4 h-4 text-primary-foreground" />
            </div>
            <div>
              <div className="flex items-center gap-3">
                <h1 className="text-lg font-semibold tracking-tight">{analytics.email}</h1>
                {getStatusBadge(analytics.status.toUpperCase())}
              </div>
              {analytics.name && <p className="text-sm text-muted-foreground">{analytics.name}</p>}
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button 
              variant="outline" 
              size="sm" 
              onClick={() => setShowCampaignsModal(true)}
              className="flex items-center gap-2"
            >
              <Megaphone className="w-4 h-4" />
              View Campaigns
            </Button>
            <Button variant="outline" size="sm">
              <Download className="w-4 h-4 mr-2" />
              Export
            </Button>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto p-3">
        {/* Overview Cards */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-6 mb-6">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Sent</CardTitle>
              <Mail className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{analytics.totalSent.toLocaleString()}</div>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Opened</CardTitle>
              <Eye className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{analytics.totalOpened.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                {analytics.openRate}% open rate
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Clicked</CardTitle>
              <MousePointer className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{analytics.totalClicked.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                {analytics.clickRate}% click rate
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Replied</CardTitle>
              <Reply className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{analytics.totalReplied.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                {analytics.replyRate}% reply rate
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Unsubscribed</CardTitle>
              <UserMinus className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{analytics.totalUnsubscribed.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                {analytics.unsubscribeRate}% unsub rate
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Bounced</CardTitle>
              <XCircle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{analytics.totalBounced.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                {analytics.bounceRate}% bounce rate
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Account Information */}
        <Card className="mb-6">
          <CardHeader className="pb-4">
            <CardTitle className="text-lg">Account Information</CardTitle>
          </CardHeader>
          <CardContent className="pt-0">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="flex flex-col space-y-1">
                <div className="flex items-center">
                  <Clock className="w-3.5 h-3.5 text-muted-foreground mr-1.5" />
                  <span className="text-xs font-medium text-muted-foreground">Created At</span>
                </div>
                <span className="text-sm pl-5">{new Date(analytics.createdAt).toLocaleDateString()}</span>
              </div>
              
              <div className="flex flex-col space-y-1">
                <div className="flex items-center">
                  <RefreshCw className="w-3.5 h-3.5 text-muted-foreground mr-1.5" />
                  <span className="text-xs font-medium text-muted-foreground">Last Updated</span>
                </div>
                <span className="text-sm pl-5">{new Date(analytics.updatedAt).toLocaleDateString()}</span>
              </div>
              
              <div className="flex flex-col space-y-1">
                <div className="flex items-center">
                  <CheckCircle className="w-3.5 h-3.5 text-muted-foreground mr-1.5" />
                  <span className="text-xs font-medium text-muted-foreground">Status</span>
                </div>
                <span className="text-sm pl-5">{analytics.status.toUpperCase()}</span>
              </div>
              
              <div className="flex flex-col space-y-1">
                <div className="flex items-center">
                  <User className="w-3.5 h-3.5 text-muted-foreground mr-1.5" />
                  <span className="text-xs font-medium text-muted-foreground">Assigned Client</span>
                </div>
                <span className="text-sm pl-5">{analytics.assignedClient || 'Not assigned'}</span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Daily Performance Chart */}
        <Card className="mb-6">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <CardTitle className="text-lg">Daily Performance Trends</CardTitle>
                <CardDescription>
                  Email metrics over the selected time period
                </CardDescription>
              </div>
              <div className="flex items-center gap-2">
                <div className="flex gap-1">
                  {['7d', '2w', '1m', '3m', '6m', '1y', 'all'].map((range) => (
                    <Button
                      key={range}
                      variant={performanceTimeRange === range ? "default" : "outline"}
                      size="sm"
                      onClick={() => setPerformanceTimeRange(range)}
                      className="h-8 px-3 text-xs"
                    >
                      {range === '2w' ? '2W' : range === '1m' ? '1M' : range === '3m' ? '3M' : 
                       range === '6m' ? '6M' : range === '1y' ? '1Y' : range.toUpperCase()}
                    </Button>
                  ))}
                </div>
                <div className="h-4 w-px bg-border mx-1" />
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPerformanceChartType(performanceChartType === 'bar' ? 'line' : 'bar')}
                  className="h-8 px-3 text-xs"
                >
                  {performanceChartType === 'bar' ? <LineChartIcon className="w-4 h-4" /> : <BarChart3 className="w-4 h-4" />}
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {/* Performance Trend Stats - Modern shadcn/ui Design */}
            <div className="grid gap-3 md:grid-cols-6 mb-6">
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Sent</p>
                  <Mail className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="text-lg font-bold tracking-tight">
                  {analytics.dailyStats.reduce((sum, day) => sum + (day.sent || 0), 0).toLocaleString()}
                </div>
              </Card>
              
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Opened</p>
                  <Eye className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.dailyStats.reduce((sum, day) => sum + (day.opened || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.dailyStats.reduce((sum, day) => sum + (day.sent || 0), 0);
                      const opened = analytics.dailyStats.reduce((sum, day) => sum + (day.opened || 0), 0);
                      return sent > 0 ? `${((opened / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>
              
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Clicked</p>
                  <MousePointer className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.dailyStats.reduce((sum, day) => sum + (day.clicked || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.dailyStats.reduce((sum, day) => sum + (day.sent || 0), 0);
                      const clicked = analytics.dailyStats.reduce((sum, day) => sum + (day.clicked || 0), 0);
                      return sent > 0 ? `${((clicked / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>
              
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Replied</p>
                  <Reply className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.dailyStats.reduce((sum, day) => sum + (day.replied || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.dailyStats.reduce((sum, day) => sum + (day.sent || 0), 0);
                      const replied = analytics.dailyStats.reduce((sum, day) => sum + (day.replied || 0), 0);
                      return sent > 0 ? `${((replied / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>
              
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Bounced</p>
                  <XCircle className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.dailyStats.reduce((sum, day) => sum + (day.bounced || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.dailyStats.reduce((sum, day) => sum + (day.sent || 0), 0);
                      const bounced = analytics.dailyStats.reduce((sum, day) => sum + (day.bounced || 0), 0);
                      return sent > 0 ? `${((bounced / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>
              
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Unsubscribed</p>
                  <UserMinus className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.dailyStats.reduce((sum, day) => sum + (day.unsubscribed || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.dailyStats.reduce((sum, day) => sum + (day.sent || 0), 0);
                      const unsubscribed = analytics.dailyStats.reduce((sum, day) => sum + (day.unsubscribed || 0), 0);
                      return sent > 0 ? `${((unsubscribed / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>
            </div>
            <div className="relative">
              <div className={`transition-opacity duration-300 ${performanceChartLoading ? 'opacity-50' : 'opacity-100'}`}>
                <ChartContainer config={performanceChartConfig} className="h-[290px] w-full">
                  {performanceChartType === 'bar' ? (
                    <BarChart 
                      key={`performance-bar-${performanceTimeRange}-${analytics.dailyStats.length}`}
                      data={analytics.dailyStats}
                    >
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      />
                      <YAxis />
                      <ChartTooltip 
                        content={<ChartTooltipContent />}
                        labelFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      />
                      <Bar dataKey="sent" fill="var(--color-sent)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="opened" fill="var(--color-opened)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="clicked" fill="var(--color-clicked)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="replied" fill="var(--color-replied)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="bounced" fill="var(--color-bounced)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="unsubscribed" fill="var(--color-unsubscribed)" radius={[2, 2, 0, 0]} />
                    </BarChart>
                  ) : (
                    <LineChart 
                      key={`performance-line-${performanceTimeRange}-${analytics.dailyStats.length}`}
                      data={analytics.dailyStats}
                    >
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      />
                      <YAxis />
                      <ChartTooltip 
                        content={<ChartTooltipContent />}
                        labelFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="sent" 
                        stroke="var(--color-sent)" 
                        strokeWidth={2}
                        dot={analytics.dailyStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="opened" 
                        stroke="var(--color-opened)" 
                        strokeWidth={2}
                        dot={analytics.dailyStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="clicked" 
                        stroke="var(--color-clicked)" 
                        strokeWidth={2}
                        dot={analytics.dailyStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="replied" 
                        stroke="var(--color-replied)" 
                        strokeWidth={2}
                        dot={analytics.dailyStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="bounced" 
                        stroke="var(--color-bounced)" 
                        strokeWidth={2}
                        dot={analytics.dailyStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="unsubscribed" 
                        stroke="var(--color-unsubscribed)" 
                        strokeWidth={2}
                        dot={analytics.dailyStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                    </LineChart>
                  )}
                </ChartContainer>
              </div>
              {performanceChartLoading && (
                <div className="absolute top-4 right-4 z-20">
                  <div className="flex items-center space-x-2 bg-background/90 backdrop-blur-sm rounded-full px-3 py-1 border">
                    <div className="animate-spin w-3 h-3 border-2 border-primary border-t-transparent rounded-full"></div>
                    <span className="text-xs text-muted-foreground">Updating...</span>
                  </div>
                </div>
              )}
              {/* Show empty data indicator when no data array is available */}
              {analytics.dailyStats.length === 0 && (
                <div className="absolute inset-0 flex items-center justify-center bg-background/90 backdrop-blur-sm">
                  <div className="text-center">
                    <BarChart3 className="w-8 h-8 text-muted-foreground mx-auto mb-2" />
                    <p className="text-sm text-muted-foreground">No daily performance data available</p>
                    <p className="text-xs text-muted-foreground">Daily statistics will appear here once email activity begins</p>
                  </div>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Email Warmup Progress */}
        <Card className="mb-6">
          <CardHeader>
            <div className="flex justify-between items-center">
              <div>
                <CardTitle className="text-lg">Email Warmup Progress</CardTitle>
                <CardDescription>Daily warmup metrics and spam protection</CardDescription>
              </div>
              <div className="flex items-center gap-2">
                <div className="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold bg-green-500 text-white mr-4">
                  <div className="w-2 h-2 bg-green-300 rounded-full mr-1.5 animate-pulse"></div>
                  {analytics.status.toUpperCase()}
                </div>
                <div className="flex items-center gap-2">
                  <div className="flex gap-1">
                    {['7d', '2w', '1m', '3m', '6m', '1y', 'all'].map((range) => (
                      <Button
                        key={range}
                        variant={warmupTimeRange === range ? "default" : "outline"}
                        size="sm"
                        onClick={() => setWarmupTimeRange(range)}
                        className="h-8 px-3 text-xs"
                      >
                        {range === '2w' ? '2W' : range === '1m' ? '1M' : range === '3m' ? '3M' : 
                         range === '6m' ? '6M' : range === '1y' ? '1Y' : range.toUpperCase()}
                      </Button>
                    ))}
                  </div>
                  <div className="h-4 w-px bg-border mx-1" />
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setWarmupChartType(warmupChartType === 'bar' ? 'line' : 'bar')}
                    className="h-8 px-3 text-xs"
                  >
                    {warmupChartType === 'bar' ? <LineChartIcon className="w-4 h-4" /> : <BarChart3 className="w-4 h-4" />}
                  </Button>
                </div>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {/* Warmup Stats - Compact like Performance Cards */}
            <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-4 mb-6">
              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Warmup Sent</p>
                  <Mail className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.warmupStats.reduce((sum, day) => sum + (day.warmupSent || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    Avg: {Math.round(analytics.warmupStats.reduce((sum, day) => sum + (day.warmupSent || 0), 0) / Math.max(analytics.warmupStats.length, 1))}
                  </span>
                </div>
              </Card>

              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Warmup Replied</p>
                  <Reply className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.warmupStats.reduce((sum, day) => sum + (day.warmupReplied || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.warmupStats.reduce((sum, day) => sum + (day.warmupSent || 0), 0);
                      const replied = analytics.warmupStats.reduce((sum, day) => sum + (day.warmupReplied || 0), 0);
                      return sent > 0 ? `${((replied / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>

              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Saved from Spam</p>
                  <CheckCircle className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className="text-lg font-bold tracking-tight">
                    {analytics.warmupStats.reduce((sum, day) => sum + (day.savedFromSpam || 0), 0).toLocaleString()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    {(() => {
                      const sent = analytics.warmupStats.reduce((sum, day) => sum + (day.warmupSent || 0), 0);
                      const saved = analytics.warmupStats.reduce((sum, day) => sum + (day.savedFromSpam || 0), 0);
                      return sent > 0 ? `${((saved / sent) * 100).toFixed(1)}%` : '0%';
                    })()}
                  </span>
                </div>
              </Card>

              <Card className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-medium text-muted-foreground">Spam Rate</p>
                  <XCircle className="h-3.5 w-3.5 text-muted-foreground/70" />
                </div>
                <div className="flex items-baseline justify-between">
                  <span className={`text-lg font-bold tracking-tight ${(() => {
                    const sent = analytics.warmupStats.reduce((sum, day) => sum + (day.warmupSent || 0), 0);
                    const spam = analytics.warmupSpamCount || 0;
                    const rate = sent > 0 ? (spam / sent * 100) : 0;
                    return rate > 10 ? 'text-red-600' : rate > 5 ? 'text-yellow-600' : 'text-green-600';
                  })()}`}>
                    {(() => {
                      const sent = analytics.warmupStats.reduce((sum, day) => sum + (day.warmupSent || 0), 0);
                      const spam = analytics.warmupSpamCount || 0;
                      return sent > 0 ? `${((spam / sent) * 100).toFixed(1)}%` : '0.0%';
                    })()}
                  </span>
                  <span className="text-xs font-medium text-muted-foreground/80">
                    Est.
                  </span>
                </div>
              </Card>
            </div>

            <div className="relative">
              <div className={`transition-opacity duration-300 ${warmupChartLoading ? 'opacity-50' : 'opacity-100'}`}>
                <ChartContainer config={warmupChartConfig} className="h-[250px] w-full">
                  {warmupChartType === 'bar' ? (
                    <BarChart 
                      key={`warmup-bar-${warmupTimeRange}-${analytics.warmupStats.length}-${warmupChartVersion}`} 
                      data={analytics.warmupStats}
                    >
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      />
                      <YAxis />
                      <ChartTooltip 
                        content={<ChartTooltipContent />}
                        labelFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      />
                      <Bar dataKey="warmupSent" fill="var(--color-warmupSent)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="warmupReplied" fill="var(--color-warmupReplied)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="savedFromSpam" fill="var(--color-savedFromSpam)" radius={[2, 2, 0, 0]} />
                    </BarChart>
                  ) : (
                    <LineChart 
                      key={`warmup-line-${warmupTimeRange}-${analytics.warmupStats.length}-${warmupChartVersion}`} 
                      data={analytics.warmupStats}
                    >
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      />
                      <YAxis />
                      <ChartTooltip 
                        content={<ChartTooltipContent />}
                        labelFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="warmupSent" 
                        stroke="var(--color-warmupSent)" 
                        strokeWidth={2}
                        dot={analytics.warmupStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="warmupReplied" 
                        stroke="var(--color-warmupReplied)" 
                        strokeWidth={2}
                        dot={analytics.warmupStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="savedFromSpam" 
                        stroke="var(--color-savedFromSpam)" 
                        strokeWidth={2}
                        dot={analytics.warmupStats.length <= 1 ? { r: 4 } : false}
                        activeDot={{ r: 4 }}
                      />
                    </LineChart>
                  )}
                </ChartContainer>
              </div>
              {warmupChartLoading && (
                <div className="absolute top-4 right-4 z-20">
                  <div className="flex items-center space-x-2 bg-background/90 backdrop-blur-sm rounded-full px-3 py-1 border">
                    <div className="animate-spin w-3 h-3 border-2 border-primary border-t-transparent rounded-full"></div>
                    <span className="text-xs text-muted-foreground">Updating...</span>
                  </div>
                </div>
              )}
              {/* Show empty data indicator when no warmup data array is available */}
              {analytics.warmupStats.length === 0 && (
                <div className="absolute inset-0 flex items-center justify-center bg-background/90 backdrop-blur-sm">
                  <div className="text-center">
                    <TrendingUp className="w-8 h-8 text-muted-foreground mx-auto mb-2" />
                    <p className="text-sm text-muted-foreground">No warmup data available</p>
                    <p className="text-xs text-muted-foreground">Warmup statistics will appear here once warmup activity begins</p>
                  </div>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Campaigns Modal */}
      <CampaignsModal
        isOpen={showCampaignsModal}
        onClose={() => setShowCampaignsModal(false)}
        emailAccountId={params.id as string}
        emailAccountName={analytics?.email}
      />
    </div>
  );
}