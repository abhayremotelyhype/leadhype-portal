import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer, 
  AreaChart,
  Area,
  ComposedChart,
  Legend
} from 'recharts';
import { 
  ChartContainer, 
  ChartTooltip, 
  ChartTooltipContent, 
  ChartLegend, 
  ChartLegendContent,
  type ChartConfig 
} from '@/components/ui/chart';
import { TimeSeriesDataPoint } from '@/types';

const performanceChartConfig: ChartConfig = {
  emailsSent: {
    label: 'Emails Sent',
    color: 'hsl(var(--chart-1))',
  },
  emailsOpened: {
    label: 'Emails Opened',
    color: 'hsl(var(--chart-2))',
  },
  emailsReplied: {
    label: 'Emails Replied',
    color: 'hsl(var(--chart-3))',
  },
  emailsBounced: {
    label: 'Emails Bounced',
    color: 'hsl(var(--chart-4))',
  },
  openRate: {
    label: 'Open Rate %',
    color: '#8B5CF6',
  },
  replyRate: {
    label: 'Reply Rate %',
    color: '#06B6D4',
  },
};

interface PerformanceChartProps {
  performancePeriod: string;
  filteredPerformanceData: TimeSeriesDataPoint[];
  loadingPerformanceTrend: boolean;
  onPerformancePeriodChange: (period: string) => void;
}

export function PerformanceChart({
  performancePeriod,
  filteredPerformanceData,
  loadingPerformanceTrend,
  onPerformancePeriodChange
}: PerformanceChartProps) {
  return (
    <Card className="lg:col-span-2">
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-lg">Email Performance Trends</CardTitle>
            <CardDescription className="text-sm">
              Email activity and performance rates over the last {performancePeriod === 'all' ? 'year' : performancePeriod === '6m' ? '6 months' : performancePeriod === '1y' ? 'year' : `${performancePeriod} days`} ({filteredPerformanceData.length} days)
            </CardDescription>
          </div>
          <div className="flex items-center gap-1 flex-wrap">
            <Button 
              variant={performancePeriod === '7' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => onPerformancePeriodChange('7')}
              disabled={loadingPerformanceTrend}
              className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
            >
              7D
            </Button>
            <Button 
              variant={performancePeriod === '30' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => onPerformancePeriodChange('30')}
              disabled={loadingPerformanceTrend}
              className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
            >
              30D
            </Button>
            <Button 
              variant={performancePeriod === '90' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => onPerformancePeriodChange('90')}
              disabled={loadingPerformanceTrend}
              className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
            >
              90D
            </Button>
            <Button 
              variant={performancePeriod === '6m' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => onPerformancePeriodChange('6m')}
              disabled={loadingPerformanceTrend}
              className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
            >
              6M
            </Button>
            <Button 
              variant={performancePeriod === '1y' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => onPerformancePeriodChange('1y')}
              disabled={loadingPerformanceTrend}
              className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
            >
              1Y
            </Button>
            <Button 
              variant={performancePeriod === 'all' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => onPerformancePeriodChange('all')}
              disabled={loadingPerformanceTrend}
              className="transition-all duration-200 hover:scale-105 hover:shadow-md focus:ring-2 focus:ring-primary/50 focus:outline-none"
            >
              All
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="p-4">
        <div className="w-full h-64 relative">
          {loadingPerformanceTrend && (
            <div className="absolute inset-0 bg-background/80 backdrop-blur-sm flex flex-col justify-end p-4 z-10">
              {/* Chart skeleton */}
              <div className="space-y-2">
                {/* Y-axis labels */}
                <div className="flex items-end justify-between h-48">
                  {[...Array(7)].map((_, i) => {
                    const heights = ['h-16', 'h-24', 'h-32', 'h-20', 'h-28', 'h-12', 'h-36'];
                    return (
                      <div key={i} className="flex flex-col items-center space-y-1">
                        <Skeleton className={`w-6 ${heights[i]} rounded-t animate-pulse`} />
                        <Skeleton className="w-6 h-8 rounded-t opacity-70 animate-pulse" style={{animationDelay: `${i * 100}ms`}} />
                      </div>
                    );
                  })}
                </div>
                {/* X-axis labels */}
                <div className="flex justify-between">
                  {[...Array(7)].map((_, i) => (
                    <Skeleton key={i} className="h-3 w-8" />
                  ))}
                </div>
              </div>
            </div>
          )}
          <ChartContainer 
            key={`performance-chart-${performancePeriod}`} 
            config={performanceChartConfig} 
            className="w-full h-full"
          >
            <ComposedChart 
              data={filteredPerformanceData}
              margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis 
                dataKey="date" 
                tickFormatter={(value) => {
                  try {
                    const date = new Date(value);
                    return isNaN(date.getTime()) ? value : date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
                  } catch (e) {
                    return value;
                  }
                }}
                fontSize={12}
                interval="preserveStartEnd"
                tick={{ fontSize: 12 }}
              />
              <YAxis 
                yAxisId="count"
                fontSize={12}
                tick={{ fontSize: 12 }}
              />
              <YAxis 
                yAxisId="rate"
                orientation="right"
                fontSize={12}
                tick={{ fontSize: 12 }}
                domain={[0, 100]}
                tickFormatter={(value) => `${value}%`}
              />
              <ChartTooltip 
                content={<ChartTooltipContent 
                  labelFormatter={(value) => {
                    try {
                      const date = new Date(value as string);
                      const dateStr = isNaN(date.getTime()) ? value as string : date.toLocaleDateString('en-US', { 
                        weekday: 'short',
                        month: 'long', 
                        day: 'numeric', 
                        year: 'numeric' 
                      });
                      return `Activity - ${dateStr}`;
                    } catch (e) {
                      return value as string;
                    }
                  }}
                />}
              />
              <ChartLegend content={<ChartLegendContent />} />
              <Area 
                type="monotone" 
                dataKey="emailsSent" 
                yAxisId="count"
                fill="var(--color-emailsSent)" 
                fillOpacity={0.1}
                stroke="var(--color-emailsSent)" 
                strokeWidth={2} 
              />
              <Line 
                type="monotone" 
                dataKey="emailsOpened" 
                yAxisId="count"
                stroke="var(--color-emailsOpened)" 
                strokeWidth={3} 
                dot={{ fill: 'var(--color-emailsOpened)', strokeWidth: 2, r: 4 }}
              />
              <Line 
                type="monotone" 
                dataKey="emailsReplied" 
                yAxisId="count"
                stroke="var(--color-emailsReplied)" 
                strokeWidth={3} 
                dot={{ fill: 'var(--color-emailsReplied)', strokeWidth: 2, r: 4 }}
              />
              <Line 
                type="monotone" 
                dataKey="emailsBounced" 
                yAxisId="count"
                stroke="var(--color-emailsBounced)" 
                strokeWidth={2} 
                dot={{ fill: 'var(--color-emailsBounced)', strokeWidth: 2, r: 3 }}
              />
              <Line 
                type="monotone" 
                dataKey="openRate" 
                yAxisId="rate"
                stroke="#8B5CF6" 
                strokeWidth={2} 
                strokeDasharray="5 5"
                dot={{ fill: '#8B5CF6', strokeWidth: 2, r: 3 }}
              />
              <Line 
                type="monotone" 
                dataKey="replyRate" 
                yAxisId="rate"
                stroke="#06B6D4" 
                strokeWidth={2} 
                strokeDasharray="3 3"
                dot={{ fill: '#06B6D4', strokeWidth: 2, r: 3 }}
              />
            </ComposedChart>
          </ChartContainer>
        </div>
        <div className="flex items-center justify-center mt-2">
          <p className="text-xs text-muted-foreground">
            📊 Email counts (left axis) & rates (right axis) • Dashed lines show percentages
          </p>
        </div>
      </CardContent>
    </Card>
  );
}