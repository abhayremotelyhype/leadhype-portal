'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { 
  ComposedChart, 
  Line, 
  XAxis, 
  YAxis, 
  CartesianGrid
} from 'recharts';
import { 
  ChartContainer, 
  ChartTooltip, 
  ChartTooltipContent, 
  ChartLegend, 
  ChartLegendContent,
  type ChartConfig 
} from '@/components/ui/chart';

interface PerformanceDataPoint {
  date: string;
  emailsSent: number;
  emailsOpened: number;
  emailsReplied: number;
  emailsBounced: number;
  openRate: number;
  replyRate: number;
}

interface CampaignPerformanceChartProps {
  data: PerformanceDataPoint[];
  period: string;
  onPeriodChange: (period: string) => void;
  loading: boolean;
}

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

export function CampaignPerformanceChart({ 
  data, 
  period, 
  onPeriodChange, 
  loading 
}: CampaignPerformanceChartProps) {
  const getPeriodLabel = (period: string) => {
    switch (period) {
      case 'all': return 'year';
      case '6m': return '6 months';
      case '1y': return 'year';
      default: return `${period} days`;
    }
  };

  const periods = [
    { value: '7', label: '7D' },
    { value: '30', label: '30D' },
    { value: '90', label: '90D' },
    { value: '6m', label: '6M' },
    { value: '1y', label: '1Y' },
    { value: 'all', label: 'All' }
  ];

  return (
    <Card className="lg:col-span-2">
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-lg">Campaign Performance Trends</CardTitle>
            <CardDescription className="text-sm">
              Campaign email activity and performance rates over the last {getPeriodLabel(period)} ({data.length} days)
            </CardDescription>
          </div>
          <div className="flex items-center gap-1 flex-wrap">
            {periods.map(({ value, label }) => (
              <Button 
                key={value}
                variant={period === value ? 'default' : 'outline'} 
                size="sm" 
                onClick={() => onPeriodChange(value)}
                disabled={loading}
                className="transition-all duration-200"
              >
                {label}
              </Button>
            ))}
          </div>
        </div>
      </CardHeader>
      <CardContent className="p-4">
        <div className="w-full h-64 relative">
          {loading && (
            <div className="absolute inset-0 bg-white/80 backdrop-blur-sm flex items-center justify-center z-10">
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 border-2 border-primary border-t-transparent rounded-full animate-spin"></div>
                <span className="text-sm text-muted-foreground">Loading campaign trends...</span>
              </div>
            </div>
          )}
          <ChartContainer 
            key={`campaign-chart-${period}`} 
            config={performanceChartConfig} 
            className="w-full h-full"
          >
            <ComposedChart 
              data={data}
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
                      return `Campaign Activity - ${dateStr}`;
                    } catch (e) {
                      return value as string;
                    }
                  }}
                />}
              />
              <ChartLegend content={<ChartLegendContent />} />
              <Line 
                type="monotone" 
                dataKey="emailsSent" 
                name="Emails Sent"
                yAxisId="count"
                stroke="var(--color-emailsSent)" 
                strokeWidth={3} 
                dot={{ fill: 'var(--color-emailsSent)', strokeWidth: 2, r: 4 }}
              />
              <Line 
                type="monotone" 
                dataKey="emailsOpened" 
                name="Emails Opened"
                yAxisId="count"
                stroke="var(--color-emailsOpened)" 
                strokeWidth={3} 
                dot={{ fill: 'var(--color-emailsOpened)', strokeWidth: 2, r: 4 }}
              />
              <Line 
                type="monotone" 
                dataKey="emailsReplied" 
                name="Emails Replied"
                yAxisId="count"
                stroke="var(--color-emailsReplied)" 
                strokeWidth={3} 
                dot={{ fill: 'var(--color-emailsReplied)', strokeWidth: 2, r: 4 }}
              />
              <Line 
                type="monotone" 
                dataKey="emailsBounced" 
                name="Emails Bounced"
                yAxisId="count"
                stroke="var(--color-emailsBounced)" 
                strokeWidth={2} 
                dot={{ fill: 'var(--color-emailsBounced)', strokeWidth: 2, r: 3 }}
              />
              <Line 
                type="monotone" 
                dataKey="openRate" 
                name="Open Rate %"
                yAxisId="rate"
                stroke="#8B5CF6" 
                strokeWidth={2} 
                strokeDasharray="5 5"
                dot={{ fill: '#8B5CF6', strokeWidth: 2, r: 3 }}
              />
              <Line 
                type="monotone" 
                dataKey="replyRate" 
                name="Reply Rate %"
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
            📈 Campaign email counts (left axis) & rates (right axis) • Dashed lines show percentages
          </p>
        </div>
      </CardContent>
    </Card>
  );
}