'use client';

import { BarChart3, TrendingUp, Users, Mail, Target, Activity, Filter, RefreshCw, Download, ArrowUpDown } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { PageHeader } from '@/components/page-header';
import { ProtectedRoute } from '@/components/protected-route';
import { apiClient, ENDPOINTS, handleApiErrorWithToast } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';
import { useState, useEffect } from 'react';
import { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, BarChart, Bar, PieChart, Pie, Cell } from 'recharts';
import type { AnalyticsDashboardResponse, AnalyticsOverview, PerformanceTrendDataPoint, EmailAccountPerformanceMetric, ClientComparisonMetric } from '@/types';

export default function AnalyticsPage() {
  const { toast } = useToast();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [period, setPeriod] = useState('30d');
  const [overview, setOverview] = useState<AnalyticsOverview | null>(null);
  const [performanceTrends, setPerformanceTrends] = useState<PerformanceTrendDataPoint[]>([]);
  const [emailAccountPerformance, setEmailAccountPerformance] = useState<EmailAccountPerformanceMetric[]>([]);
  const [clientComparison, setClientComparison] = useState<ClientComparisonMetric[]>([]);
  
  // Advanced filters
  const [dateRange, setDateRange] = useState<{from?: Date, to?: Date}>({});
  const [autoRefresh, setAutoRefresh] = useState(false);

  // Color schemes for charts
  const chartColors = ['#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'];

  // Load all analytics data in one optimized call
  const loadAnalyticsDashboard = async (selectedPeriod: string, showRefreshing = false) => {
    try {
      if (showRefreshing) setRefreshing(true);

      const params = {
        period: selectedPeriod,
        ...(dateRange.from && { startDate: dateRange.from.toISOString().split('T')[0] }),
        ...(dateRange.to && { endDate: dateRange.to.toISOString().split('T')[0] })
      };

      const response = await apiClient.get<AnalyticsDashboardResponse>(ENDPOINTS.analytics.dashboard, params);

      // Set all data from single response
      setOverview(response.overview);
      setPerformanceTrends(response.performanceTrends || []);
      setEmailAccountPerformance(response.emailAccountPerformance || []);
      setClientComparison(response.clientComparison || []);

    } catch (error: any) {
      handleApiErrorWithToast(error, 'load analytics dashboard', toast);
    } finally {
      if (showRefreshing) setRefreshing(false);
    }
  };

  // Load all data with single optimized call
  const loadAllData = async (showRefreshing = false) => {
    setLoading(true);
    
    // Single API call for all dashboard data
    await loadAnalyticsDashboard(period, showRefreshing);
    
    setLoading(false);
  };

  // Handle period change with single API call
  const handlePeriodChange = async (newPeriod: string) => {
    setPeriod(newPeriod);
    await loadAnalyticsDashboard(newPeriod);
  };

  // Handle date range change
  const handleDateRangeChange = async (newDateRange: {from?: Date, to?: Date}) => {
    setDateRange(newDateRange);
    if (newDateRange.from && newDateRange.to) {
      await loadAnalyticsDashboard(period);
    }
  };

  // Load data on component mount
  useEffect(() => {
    document.title = 'Analytics - LeadHype';
    loadAllData();
  }, []);

  // Helper functions
  const formatNumber = (num: number) => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toString();
  };

  const getPeriodDescription = (periodValue: string) => {
    const descriptions = {
      '1d': 'Today\'s data',
      '3d': 'Last 3 days',
      '7d': 'Last 7 days',
      '14d': 'Last 2 weeks',
      '30d': 'Last 30 days',
      '60d': 'Last 2 months',
      '90d': 'Last 3 months', 
      '180d': 'Last 6 months',
      '365d': 'Last 12 months',
      'all': 'All time data'
    };
    return descriptions[periodValue as keyof typeof descriptions] || 'Selected period';
  };

  const getChangeColor = (change: number) => {
    if (change > 0) return 'text-green-600';
    if (change < 0) return 'text-red-600';
    return 'text-muted-foreground';
  };

  const getChangeIcon = (change: number) => {
    if (change > 0) return <TrendingUp className="h-3 w-3 sm:h-4 sm:w-4" />;
    if (change < 0) return <TrendingUp className="h-3 w-3 sm:h-4 sm:w-4 rotate-180" />;
    return <ArrowUpDown className="h-3 w-3 sm:h-4 sm:w-4" />;
  };

  if (loading) {
    return (
      <ProtectedRoute>
        <div className="flex h-full flex-col">
          <PageHeader 
            title="Analytics"
            description="Advanced analytics and performance insights"
            mobileDescription="Analytics insights"
            icon={TrendingUp}
          />
          <div className="flex-1 overflow-auto p-4 space-y-6">
            {/* Skeleton Loading */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {[1,2,3,4].map(i => (
                <Card key={i} className="animate-pulse">
                  <CardHeader>
                    <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                  </CardHeader>
                  <CardContent>
                    <div className="h-8 bg-gray-200 rounded w-1/2 mb-2"></div>
                    <div className="h-3 bg-gray-200 rounded w-full"></div>
                  </CardContent>
                </Card>
              ))}
            </div>
            
            <Card className="animate-pulse">
              <CardHeader>
                <div className="h-6 bg-gray-200 rounded w-1/4"></div>
              </CardHeader>
              <CardContent>
                <div className="h-64 bg-gray-200 rounded"></div>
              </CardContent>
            </Card>
            
            <div className="text-center py-4">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
              <p className="text-sm text-muted-foreground">Loading analytics data...</p>
            </div>
          </div>
        </div>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute>
      <div className="flex h-full flex-col">
        <PageHeader 
          title="Analytics"
          description="Advanced analytics and performance insights"
          mobileDescription="Analytics insights"
          icon={TrendingUp}
          actions={
            <div className="flex items-center gap-2">
              <Select value={period} onValueChange={handlePeriodChange}>
                <SelectTrigger className="w-[180px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1d">Today</SelectItem>
                  <SelectItem value="3d">Last 3 days</SelectItem>
                  <SelectItem value="7d">Last 7 days</SelectItem>
                  <SelectItem value="14d">Last 2 weeks</SelectItem>
                  <SelectItem value="30d">Last 30 days</SelectItem>
                  <SelectItem value="60d">Last 2 months</SelectItem>
                  <SelectItem value="90d">Last 3 months</SelectItem>
                  <SelectItem value="180d">Last 6 months</SelectItem>
                  <SelectItem value="365d">Last year</SelectItem>
                  <SelectItem value="all">All time</SelectItem>
                </SelectContent>
              </Select>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => loadAllData(true)}
                disabled={refreshing}
              >
                {refreshing ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current sm:mr-2" />
                    <span className="hidden sm:inline">Refreshing...</span>
                  </>
                ) : (
                  <>
                    <RefreshCw className="w-4 h-4 sm:mr-2" />
                    <span className="hidden sm:inline">Refresh</span>
                  </>
                )}
              </Button>
            </div>
          }
        />

        <div className="flex-1 overflow-auto p-4 space-y-6">
          {/* Overview Stats */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Total Campaigns</CardTitle>
                <Target className="h-4 w-4 text-blue-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{overview?.totalCampaigns || 0}</div>
                <div className="flex items-center mt-2">
                  {getChangeIcon(overview?.campaignGrowth || 0)}
                  <span className={`text-sm ml-1 ${getChangeColor(overview?.campaignGrowth || 0)}`}>
                    {(overview?.campaignGrowth || 0) > 0 ? '+' : ''}{(overview?.campaignGrowth || 0).toFixed(1)}%
                  </span>
                  <span className="text-sm text-muted-foreground ml-1">vs last period</span>
                </div>
                <div className="text-xs text-muted-foreground mt-1">
                  {overview?.activeCampaigns || 0} active • {overview?.pausedCampaigns || 0} paused
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Email Accounts</CardTitle>
                <Mail className="h-4 w-4 text-green-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{overview?.totalEmailAccounts || 0}</div>
                <div className="flex items-center mt-2">
                  {getChangeIcon(overview?.emailAccountGrowth || 0)}
                  <span className={`text-sm ml-1 ${getChangeColor(overview?.emailAccountGrowth || 0)}`}>
                    {(overview?.emailAccountGrowth || 0) > 0 ? '+' : ''}{(overview?.emailAccountGrowth || 0).toFixed(1)}%
                  </span>
                  <span className="text-sm text-muted-foreground ml-1">vs last period</span>
                </div>
                <div className="text-xs text-muted-foreground mt-1">
                  Active across all campaigns
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Active Clients</CardTitle>
                <Users className="h-4 w-4 text-orange-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{overview?.activeClients || 0}</div>
                <div className="flex items-center mt-2">
                  {getChangeIcon(overview?.clientGrowth || 0)}
                  <span className={`text-sm ml-1 ${getChangeColor(overview?.clientGrowth || 0)}`}>
                    {(overview?.clientGrowth || 0) > 0 ? '+' : ''}{(overview?.clientGrowth || 0).toFixed(1)}%
                  </span>
                  <span className="text-sm text-muted-foreground ml-1">vs last period</span>
                </div>
                <div className="text-xs text-muted-foreground mt-1">
                  With active campaigns
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Reply Rate</CardTitle>
                <Activity className="h-4 w-4 text-purple-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{overview?.replyRate || 0}%</div>
                <div className="flex items-center mt-2">
                  {getChangeIcon(overview?.replyRateChange || 0)}
                  <span className={`text-sm ml-1 ${getChangeColor(overview?.replyRateChange || 0)}`}>
                    {(overview?.replyRateChange || 0) > 0 ? '+' : ''}{(overview?.replyRateChange || 0).toFixed(1)}%
                  </span>
                  <span className="text-sm text-muted-foreground ml-1">vs last period</span>
                </div>
                <div className="text-xs text-muted-foreground mt-1">
                  Open: {overview?.openRate || 0}% • Bounce: {overview?.bounceRate || 0}%
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Analytics Tabs */}
          <Tabs defaultValue="trends" className="space-y-4">
            <TabsList className="grid w-full grid-cols-4">
              <TabsTrigger value="trends">Performance Trends</TabsTrigger>
              <TabsTrigger value="accounts">Email Accounts</TabsTrigger>
              <TabsTrigger value="clients">Client Comparison</TabsTrigger>
              <TabsTrigger value="insights">Insights</TabsTrigger>
            </TabsList>

            <TabsContent value="trends" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Performance Over Time</CardTitle>
                </CardHeader>
                <CardContent>
                  {performanceTrends.length > 0 ? (
                    <ResponsiveContainer width="100%" height={300}>
                      <LineChart data={performanceTrends}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" />
                        <YAxis yAxisId="left" />
                        <YAxis yAxisId="right" orientation="right" />
                        <Tooltip />
                        <Legend />
                        <Bar yAxisId="left" dataKey="sent" fill="#3b82f6" name="Sent" />
                        <Bar yAxisId="left" dataKey="opened" fill="#10b981" name="Opened" />
                        <Line yAxisId="right" type="monotone" dataKey="replyRate" stroke="#ef4444" name="Reply Rate %" />
                        <Line yAxisId="right" type="monotone" dataKey="openRate" stroke="#8b5cf6" name="Open Rate %" />
                      </LineChart>
                    </ResponsiveContainer>
                  ) : (
                    <div className="flex items-center justify-center h-64 text-muted-foreground">
                      No performance data available for the selected period
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="accounts" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Top Performing Email Accounts</CardTitle>
                </CardHeader>
                <CardContent>
                  {emailAccountPerformance.length > 0 ? (
                    <div className="space-y-4">
                      {emailAccountPerformance.map((account, index) => (
                        <div key={account.emailAccountId} className="flex items-center justify-between p-3 border rounded-lg">
                          <div className="flex-1">
                            <div className="font-medium">{account.email}</div>
                            <div className="text-sm text-muted-foreground">{account.name}</div>
                          </div>
                          <div className="text-right space-y-1">
                            <div className="font-medium">{formatNumber(account.sent)} sent</div>
                            <div className="flex gap-2 text-sm">
                              <Badge variant="secondary">{account.replyRate}% reply</Badge>
                              <Badge variant="outline">{account.openRate}% open</Badge>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="flex items-center justify-center h-64 text-muted-foreground">
                      No email account data available for the selected period
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="clients" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Client Performance Comparison</CardTitle>
                </CardHeader>
                <CardContent>
                  {clientComparison.length > 0 ? (
                    <div className="space-y-4">
                      {clientComparison.map((client) => (
                        <div key={client.clientId} className="p-4 border rounded-lg">
                          <div className="flex items-center justify-between mb-3">
                            <div className="flex items-center gap-2">
                              {client.clientColor && (
                                <div className="w-3 h-3 rounded-full" style={{ backgroundColor: client.clientColor }}></div>
                              )}
                              <span className="font-medium">{client.clientName}</span>
                              <Badge variant="outline">{client.campaigns} campaigns</Badge>
                            </div>
                            <div className="text-right">
                              <div className="font-medium">{formatNumber(client.sent)} sent</div>
                              <div className="text-sm text-muted-foreground">{client.activeCampaigns} active</div>
                            </div>
                          </div>
                          <div className="grid grid-cols-3 gap-4 text-sm">
                            <div>
                              <div className="text-muted-foreground">Reply Rate</div>
                              <div className="font-medium">{client.replyRate}%</div>
                              <Progress value={client.replyRate} className="h-1 mt-1" />
                            </div>
                            <div>
                              <div className="text-muted-foreground">Open Rate</div>
                              <div className="font-medium">{client.openRate}%</div>
                              <Progress value={client.openRate} className="h-1 mt-1" />
                            </div>
                            <div>
                              <div className="text-muted-foreground">Bounce Rate</div>
                              <div className="font-medium">{client.bounceRate}%</div>
                              <Progress value={client.bounceRate} className="h-1 mt-1" />
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="flex items-center justify-center h-64 text-muted-foreground">
                      No client comparison data available for the selected period
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="insights" className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Card>
                  <CardHeader>
                    <CardTitle>Performance Insights</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    {overview && (
                      <>
                        <div className="flex items-center justify-between p-3 bg-muted/50 rounded">
                          <span>Campaign Efficiency</span>
                          <Badge variant={overview.replyRate > 15 ? "default" : "secondary"}>
                            {overview.replyRate > 15 ? "Excellent" : "Good"}
                          </Badge>
                        </div>
                        <div className="flex items-center justify-between p-3 bg-muted/50 rounded">
                          <span>Email Deliverability</span>
                          <Badge variant={overview.bounceRate < 5 ? "default" : "secondary"}>
                            {overview.bounceRate < 5 ? "Healthy" : "Needs Attention"}
                          </Badge>
                        </div>
                        <div className="flex items-center justify-between p-3 bg-muted/50 rounded">
                          <span>Growth Trend</span>
                          <Badge variant={overview.campaignGrowth > 0 ? "default" : "secondary"}>
                            {overview.campaignGrowth > 0 ? "Growing" : "Stable"}
                          </Badge>
                        </div>
                      </>
                    )}
                  </CardContent>
                </Card>
                
                <Card>
                  <CardHeader>
                    <CardTitle>Quick Stats</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    {overview && (
                      <>
                        <div className="text-sm">
                          <div className="text-muted-foreground">Avg. Reply Rate</div>
                          <div className="text-2xl font-bold">{overview.replyRate}%</div>
                        </div>
                        <div className="text-sm">
                          <div className="text-muted-foreground">Total Campaigns</div>
                          <div className="text-2xl font-bold">{overview.totalCampaigns}</div>
                        </div>
                        <div className="text-sm">
                          <div className="text-muted-foreground">Active Clients</div>
                          <div className="text-2xl font-bold">{overview.activeClients}</div>
                        </div>
                      </>
                    )}
                  </CardContent>
                </Card>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </ProtectedRoute>
  );
}
