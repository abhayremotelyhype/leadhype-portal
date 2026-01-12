'use client';

import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ArrowLeft, TrendingUp, Calendar, Download, BarChart3, LineChart as LineChartIcon, Mail, MessageCircle, FileText } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from '@/components/ui/chart';
import { Bar, BarChart, CartesianGrid, Line, LineChart, ResponsiveContainer, XAxis, YAxis, Tooltip } from 'recharts';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';
import { format } from 'date-fns';
import { EmailAccountsModal } from '@/components/email-accounts-modal';
import { LeadHistoryModal } from '@/components/lead-history-modal';
import { EmailTemplatesModal } from '@/components/email-templates-modal';

// Global cache to prevent duplicate requests across component re-mounts
const requestCache = new Map<string, Promise<any>>();

interface PositiveReplyData {
  date: string;
  count: number;
}

interface PositiveRepliesResponse {
  campaignId: string;
  data: PositiveReplyData[];
  totalPositiveReplies: number;
}

interface CampaignDetails {
  id: string;
  campaignId: number;
  name: string;
  totalLeads: number;
  totalSent: number;
  totalReplied: number;
  totalPositiveReplies: number;
  clientName?: string;
  clientColor?: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
  emailIds: number[];
  // Analytics data now included directly
  sent: Record<string, number>;
  opened: Record<string, number>;
  clicked: Record<string, number>;
  replied: Record<string, number>;
  positiveReplies: Record<string, number>;
}

// Chart configuration - Official shadcn/ui format
const positiveRepliesChartConfig = {
  count: {
    label: "Positive Replies",
    color: "hsl(var(--chart-1))",
  },
} satisfies ChartConfig;

// Combined chart configuration (includes positive replies + campaign metrics)
const combinedChartConfig = {
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
  positiveReplies: {
    label: "Positive Replies",
    color: "hsl(var(--chart-5))",
  },
} satisfies ChartConfig;

export default function CampaignAnalyticsPage() {
  const params = useParams();
  const router = useRouter();
  const { toast } = useToast();
  const [loading, setLoading] = useState(true);
  const [campaignDetails, setCampaignDetails] = useState<CampaignDetails | null>(null);
  const [filteredData, setFilteredData] = useState<PositiveReplyData[]>([]);
  const [chartType, setChartType] = useState<'bar' | 'line'>('line');
  const [timeRange, setTimeRange] = useState('1y');
  const [chartLoading, setChartLoading] = useState(false);
  const [showEmailAccountsModal, setShowEmailAccountsModal] = useState(false);
  const [showLeadHistoryModal, setShowLeadHistoryModal] = useState(false);
  const [showTemplatesModal, setShowTemplatesModal] = useState(false);
  const loadingRef = useRef(false);
  const lastLoadedCampaignId = useRef<string | null>(null);

  const filterDataByTimeRange = (data: any[], timeRange: string) => {
    if (!data || data.length === 0) return data;
    
    const days = timeRange === '7d' ? 7 : 
                 timeRange === '1m' ? 30 : 
                 timeRange === '3m' ? 90 : 
                 timeRange === '6m' ? 180 : 
                 timeRange === '1y' ? 365 : 
                 timeRange === 'all' ? data.length : data.length;
    
    if (timeRange === 'all') return data;
    
    // Get the last N days of data
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - days);
    
    return data.filter(item => new Date(item.date) >= cutoffDate);
  };


  const loadCampaignData = useCallback(async () => {
    if (!params.campaignId || loadingRef.current) return;
    
    // Skip if we already loaded data for this campaign
    if (lastLoadedCampaignId.current === params.campaignId.toString()) return;
    
    loadingRef.current = true;
    lastLoadedCampaignId.current = params.campaignId.toString();
    setLoading(true);
    
    try {
      const cacheKey = `campaign-details-${params.campaignId}`;
      
      // Check cache first
      let detailsResponse: CampaignDetails | null = null;
      if (requestCache.has(cacheKey)) {
        detailsResponse = await requestCache.get(cacheKey);
      } else {
        const promise = apiClient.get<CampaignDetails>(
          `${ENDPOINTS.campaigns}/by-campaign-id/${params.campaignId}`
        );
        requestCache.set(cacheKey, promise);
        detailsResponse = await promise;
      }

      if (detailsResponse) {
        setCampaignDetails(detailsResponse);
        
        // Convert positive replies dictionary to array for chart
        const positiveRepliesArray: PositiveReplyData[] = Object.entries(detailsResponse.positiveReplies || {})
          .map(([date, count]) => ({ date, count }))
          .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
        
        // Apply initial filtering
        const filtered = filterDataByTimeRange(positiveRepliesArray, timeRange);
        setFilteredData(filtered);
      }
    } catch (error) {
      // If there was an error, reset the cache so we can retry
      lastLoadedCampaignId.current = null;
      requestCache.delete(`campaign-details-${params.campaignId}`);
      console.error('Failed to load campaign data:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load campaign data',
      });
    } finally {
      setLoading(false);
      loadingRef.current = false;
    }
  }, [params.campaignId, timeRange, toast]);

  // Main effect to load campaign data when campaignId changes
  useEffect(() => {
    if (params.campaignId) {
      loadCampaignData();
    }
    
    // Cleanup function to reset loading state when campaignId changes
    return () => {
      loadingRef.current = false;
      lastLoadedCampaignId.current = null;
      // Clear cache for this campaign when navigating away
      if (params.campaignId) {
        requestCache.delete(`campaign-details-${params.campaignId}`);
      }
    };
  }, [params.campaignId, loadCampaignData]);

  // Update chart data when time range changes with debouncing
  useEffect(() => {
    if (campaignDetails && campaignDetails.positiveReplies) {
      setChartLoading(true);
      
      const timeoutId = setTimeout(() => {
        const positiveRepliesArray: PositiveReplyData[] = Object.entries(campaignDetails.positiveReplies)
          .map(([date, count]) => ({ date, count }))
          .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
        
        const filtered = filterDataByTimeRange(positiveRepliesArray, timeRange);
        setFilteredData(filtered);
        setChartLoading(false);
      }, 100); // 100ms debounce
      
      return () => {
        clearTimeout(timeoutId);
      };
    }
  }, [timeRange, campaignDetails]);

  // Memoize chart data calculations (must be before conditional returns)
  const chartData = useMemo(() => {
    return filteredData.length > 0 ? filteredData.filter(item => {
      // Filter out any items with invalid dates
      const date = new Date(item.date);
      return !isNaN(date.getTime()) && item.date && item.date.trim() !== '';
    }) : [];
  }, [filteredData]);

  // Prepare combined chart data (positive replies + campaign analytics)
  const prepareCombinedChartData = useCallback(() => {
    const combinedData = new Map();
    
    // Add positive replies data
    if (filteredData && filteredData.length > 0) {
      filteredData.forEach(item => {
        if (item.date && item.date.trim() !== '') {
          const date = item.date;
          if (!combinedData.has(date)) {
            combinedData.set(date, {
              date,
              sent: 0,
              opened: 0,
              clicked: 0,
              replied: 0,
              positiveReplies: 0
            });
          }
          combinedData.get(date).positiveReplies = item.count;
        }
      });
    }
    
    // Add campaign analytics data from campaignDetails
    if (campaignDetails) {
      // Add sent data
      Object.entries(campaignDetails.sent || {}).forEach(([date, count]) => {
        if (!combinedData.has(date)) {
          combinedData.set(date, {
            date,
            sent: 0,
            opened: 0,
            clicked: 0,
            replied: 0,
            positiveReplies: 0
          });
        }
        combinedData.get(date).sent = count;
      });
      
      // Add opened data
      Object.entries(campaignDetails.opened || {}).forEach(([date, count]) => {
        if (!combinedData.has(date)) {
          combinedData.set(date, {
            date,
            sent: 0,
            opened: 0,
            clicked: 0,
            replied: 0,
            positiveReplies: 0
          });
        }
        combinedData.get(date).opened = count;
      });
      
      // Add clicked data
      Object.entries(campaignDetails.clicked || {}).forEach(([date, count]) => {
        if (!combinedData.has(date)) {
          combinedData.set(date, {
            date,
            sent: 0,
            opened: 0,
            clicked: 0,
            replied: 0,
            positiveReplies: 0
          });
        }
        combinedData.get(date).clicked = count;
      });
      
      // Add replied data
      Object.entries(campaignDetails.replied || {}).forEach(([date, count]) => {
        if (!combinedData.has(date)) {
          combinedData.set(date, {
            date,
            sent: 0,
            opened: 0,
            clicked: 0,
            replied: 0,
            positiveReplies: 0
          });
        }
        combinedData.get(date).replied = count;
      });
    }
    
    // Fill in missing dates to create continuous timeline
    if (combinedData.size > 0) {
      const dates = Array.from(combinedData.keys()).sort();
      const startDate = new Date(dates[0]);
      const endDate = new Date(dates[dates.length - 1]);
      
      // Generate all dates between start and end
      const currentDate = new Date(startDate);
      while (currentDate <= endDate) {
        const dateStr = currentDate.toISOString().split('T')[0];
        if (!combinedData.has(dateStr)) {
          combinedData.set(dateStr, {
            date: dateStr,
            sent: 0,
            opened: 0,
            clicked: 0,
            replied: 0,
            positiveReplies: 0
          });
        }
        currentDate.setDate(currentDate.getDate() + 1);
      }
    }
    
    // Convert to array and sort by date
    const sortedData = Array.from(combinedData.values()).sort((a, b) => 
      new Date(a.date).getTime() - new Date(b.date).getTime()
    );
    
    // Filter by time range
    return filterDataByTimeRange(sortedData, timeRange);
  }, [filteredData, campaignDetails, timeRange]);

  const combinedChartData = useMemo(() => {
    return prepareCombinedChartData();
  }, [prepareCombinedChartData]);
  
  const hasData = chartData.length > 0;
  const hasCombinedData = combinedChartData.length > 0;

  // Calculate some statistics (memoized)
  const statistics = useMemo(() => {
    const averageReplies = hasData && campaignDetails?.totalPositiveReplies
      ? Math.round(campaignDetails.totalPositiveReplies / chartData.length) 
      : 0;
    const maxReplies = hasData 
      ? Math.max(...chartData.map(d => d.count)) 
      : 0;
    const minReplies = hasData 
      ? Math.min(...chartData.map(d => d.count)) 
      : 0;
    
    return { averageReplies, maxReplies, minReplies };
  }, [hasData, campaignDetails?.totalPositiveReplies, chartData]);

  const downloadCSV = () => {
    if (!filteredData || !filteredData.length) return;

    const csvContent = [
      ['Date', 'Positive Replies'],
      ...filteredData.map(item => [item.date, item.count.toString()])
    ].map(row => row.join(',')).join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `positive-replies-${campaignDetails?.name || 'campaign'}-${format(new Date(), 'yyyy-MM-dd')}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    toast({
      title: 'Success',
      description: 'Data downloaded successfully',
    });
  };

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  if (!campaignDetails) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">No data available</p>
        <Button variant="outline" onClick={() => router.push('/campaigns')}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Go Back
        </Button>
      </div>
    );
  }


  // Calculate reply rates
  const totalReplyRate = campaignDetails?.totalSent > 0 
    ? ((campaignDetails.totalReplied / campaignDetails.totalSent) * 100).toFixed(1)
    : '0.0';
  
  const positiveReplyRate = campaignDetails?.totalReplied > 0 
    ? ((campaignDetails.totalPositiveReplies / campaignDetails.totalReplied) * 100).toFixed(1)
    : '0.0';

  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      {/* Simple Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button 
            variant="outline" 
            size="sm" 
            onClick={() => router.push('/campaigns')}
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back
          </Button>
          <div>
            <h2 className="text-3xl font-bold tracking-tight">{campaignDetails?.name || 'Campaign Analytics'}</h2>
            <p className="text-muted-foreground">Performance metrics and insights</p>
          </div>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={downloadCSV}
          disabled={!hasData}
        >
          <Download className="w-4 h-4 mr-2" />
          Export
        </Button>
      </div>

      {/* Simple Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Leads</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{campaignDetails?.totalLeads?.toLocaleString() || '0'}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Sent</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{campaignDetails?.totalSent?.toLocaleString() || '0'}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Replies</CardTitle>
            <BarChart3 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {campaignDetails?.totalReplied?.toLocaleString() || '0'}
              {campaignDetails?.totalSent > 0 && (
                <span className="text-sm text-muted-foreground ml-2">
                  ({((campaignDetails.totalReplied / campaignDetails.totalSent) * 100).toFixed(1)}%)
                </span>
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Positive Replies</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {campaignDetails?.totalPositiveReplies?.toLocaleString() || '0'}
              {campaignDetails?.totalReplied > 0 && (
                <span className="text-sm text-muted-foreground ml-2">
                  ({((campaignDetails.totalPositiveReplies / campaignDetails.totalReplied) * 100).toFixed(1)}%)
                </span>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Main Chart */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Performance Overview</CardTitle>
              <CardDescription>Campaign engagement metrics over time</CardDescription>
            </div>
            <div className="flex items-center gap-2">
              {['7d', '1m', '3m', '6m', '1y', 'all'].map((range) => (
                <Button
                  key={range}
                  variant={timeRange === range ? "default" : "outline"}
                  size="sm"
                  onClick={() => setTimeRange(range)}
                >
                  {range === '1m' ? '1M' : range === '3m' ? '3M' : 
                   range === '6m' ? '6M' : range === '1y' ? '1Y' : range.toUpperCase()}
                </Button>
              ))}
              <div className="h-4 w-px bg-border mx-1" />
              <Button
                variant="outline"
                size="sm"
                onClick={() => setChartType(chartType === 'bar' ? 'line' : 'bar')}
              >
                {chartType === 'bar' ? <LineChartIcon className="w-4 h-4" /> : <BarChart3 className="w-4 h-4" />}
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="relative">
            {chartLoading && (
              <div className="absolute inset-0 bg-background/80 backdrop-blur-sm z-10 flex items-center justify-center">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
              </div>
            )}
            <div className={`transition-all duration-200 ${chartLoading ? 'opacity-50 scale-[0.98]' : 'opacity-100 scale-100'}`}>
              {hasCombinedData ? (
                <ChartContainer config={combinedChartConfig} className="h-[350px] w-full">
                  {chartType === 'bar' ? (
                    <BarChart data={combinedChartData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => {
                          try {
                            const date = new Date(value);
                            if (isNaN(date.getTime())) {
                              return value;
                            }
                            return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
                          } catch (error) {
                            return value;
                          }
                        }}
                      />
                      <YAxis />
                      <ChartTooltip content={<ChartTooltipContent />} />
                      <Bar dataKey="sent" fill="var(--color-sent)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="opened" fill="var(--color-opened)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="clicked" fill="var(--color-clicked)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="replied" fill="var(--color-replied)" radius={[2, 2, 0, 0]} />
                      <Bar dataKey="positiveReplies" fill="var(--color-positiveReplies)" radius={[2, 2, 0, 0]} />
                    </BarChart>
                  ) : (
                    <LineChart data={combinedChartData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="date" 
                        tickFormatter={(value) => {
                          try {
                            const date = new Date(value);
                            if (isNaN(date.getTime())) {
                              return value;
                            }
                            return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
                          } catch (error) {
                            return value;
                          }
                        }}
                      />
                      <YAxis />
                      <ChartTooltip content={<ChartTooltipContent />} />
                      <Line 
                        type="monotone" 
                        dataKey="sent" 
                        stroke="var(--color-sent)" 
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="opened" 
                        stroke="var(--color-opened)" 
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="clicked" 
                        stroke="var(--color-clicked)" 
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="replied" 
                        stroke="var(--color-replied)" 
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="positiveReplies" 
                        stroke="var(--color-positiveReplies)" 
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                    </LineChart>
                  )}
                </ChartContainer>
              ) : (
                <div className="flex h-[350px] items-center justify-center">
                  <div className="text-center">
                    <Calendar className="mx-auto h-12 w-12 text-muted-foreground" />
                    <p className="mt-2 text-muted-foreground">No campaign data available</p>
                    <p className="text-sm text-muted-foreground mt-1">Data will appear here once campaign metrics are tracked</p>
                  </div>
                </div>
              )}
            </div>
            {chartLoading && (
              <div className="absolute top-4 right-4 z-20">
                <div className="flex items-center space-x-2 bg-background/90 backdrop-blur-sm rounded-full px-3 py-1 border">
                  <div className="animate-spin w-3 h-3 border-2 border-primary border-t-transparent rounded-full"></div>
                  <span className="text-xs text-muted-foreground">Updating...</span>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Additional Info */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Performance Insights</CardTitle>
            <CardDescription>Key metrics and trends from your campaign</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Peak performance day</span>
                <span className="font-medium">{statistics.maxReplies.toLocaleString()} replies</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Daily average</span>
                <span className="font-medium">{statistics.averageReplies.toLocaleString()} replies</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Data points</span>
                <span className="font-medium">{chartData.length.toLocaleString()} days</span>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick Actions</CardTitle>
            <CardDescription>Export and analyze your campaign data</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-2">
              <Button 
                variant="outline" 
                className="w-full justify-start" 
                onClick={downloadCSV}
                disabled={!hasData}
              >
                <Download className="w-4 h-4 mr-2" />
                Export data as CSV
              </Button>
              <Button 
                variant="outline" 
                className="w-full justify-start" 
                onClick={() => setShowEmailAccountsModal(true)}
                disabled={!campaignDetails?.emailIds || campaignDetails.emailIds.length === 0}
              >
                <Mail className="w-4 h-4 mr-2" />
                View email accounts ({campaignDetails?.emailIds?.length || 0})
              </Button>
              <Button 
                variant="outline" 
                className="w-full justify-start" 
                onClick={() => setShowLeadHistoryModal(true)}
              >
                <MessageCircle className="w-4 h-4 mr-2" />
                View lead conversations
              </Button>
              <Button 
                variant="outline" 
                className="w-full justify-start" 
                onClick={() => setShowTemplatesModal(true)}
              >
                <FileText className="w-4 h-4 mr-2" />
                View email templates
              </Button>
              <Button 
                variant="outline" 
                className="w-full justify-start" 
                disabled
              >
                <TrendingUp className="w-4 h-4 mr-2" />
                Generate report (Coming soon)
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Email Accounts Modal */}
      <EmailAccountsModal
        isOpen={showEmailAccountsModal}
        onClose={() => setShowEmailAccountsModal(false)}
        campaignId={params.campaignId as string}
        campaignName={campaignDetails?.name}
      />

      {/* Lead History Modal */}
      <LeadHistoryModal
        isOpen={showLeadHistoryModal}
        onClose={() => setShowLeadHistoryModal(false)}
        campaignId={params.campaignId as string}
        campaignName={campaignDetails?.name}
      />

      {/* Email Templates Modal */}
      <EmailTemplatesModal
        isOpen={showTemplatesModal}
        onClose={() => setShowTemplatesModal(false)}
        campaignId={params.campaignId as string}
        campaignName={campaignDetails?.name}
      />
    </div>
  );
}