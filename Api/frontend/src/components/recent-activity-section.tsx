'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Clock } from 'lucide-react';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid } from 'recharts';
import { 
  ChartContainer, 
  ChartTooltip, 
  ChartTooltipContent,
  type ChartConfig 
} from '@/components/ui/chart';

interface RecentActivity {
  id: string;
  title: string;
  description: string;
  type: string;
  timestamp: string;
  color: string;
}

interface RecentActivitySectionProps {
  recentActivities: RecentActivity[];
}

const emailAccountChartConfig: ChartConfig = {
  active: {
    label: 'Active',
    color: 'hsl(var(--chart-1))',
  },
  warming: {
    label: 'Warming Up',
    color: 'hsl(var(--chart-2))',
  },
  paused: {
    label: 'Paused',
    color: 'hsl(var(--chart-3))',
  },
  issues: {
    label: 'Issues',
    color: 'hsl(var(--chart-4))',
  },
};

export function RecentActivitySection({ recentActivities }: RecentActivitySectionProps) {
  // Transform activities data for the chart
  const chartData = recentActivities.reduce((acc, activity) => {
    const date = new Date(activity.timestamp).toISOString().split('T')[0];
    const existing = acc.find(item => item.date === date);
    if (existing) {
      existing[activity.type] = (existing[activity.type] || 0) + 1;
    } else {
      acc.push({ 
        date, 
        [activity.type]: 1 
      });
    }
    return acc;
  }, [] as any[]).slice(-7);

  return (
    <div className="space-y-3">
      {/* Activity Overview Chart */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Activity Distribution</CardTitle>
          <CardDescription className="text-sm">Activity types over the last 7 days</CardDescription>
        </CardHeader>
        <CardContent className="p-4">
          <ChartContainer config={emailAccountChartConfig} className="h-40">
            <AreaChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis 
                dataKey="date" 
                fontSize={11}
                tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
              />
              <YAxis fontSize={11} />
              <ChartTooltip content={<ChartTooltipContent />} />
              <Area 
                type="monotone" 
                dataKey="Campaign" 
                stackId="1"
                stroke="var(--color-active)" 
                fill="var(--color-active)" 
                fillOpacity={0.6}
              />
              <Area 
                type="monotone" 
                dataKey="Email" 
                stackId="1"
                stroke="var(--color-warming)" 
                fill="var(--color-warming)" 
                fillOpacity={0.6}
              />
              <Area 
                type="monotone" 
                dataKey="System" 
                stackId="1"
                stroke="var(--color-paused)" 
                fill="var(--color-paused)" 
                fillOpacity={0.6}
              />
            </AreaChart>
          </ChartContainer>
        </CardContent>
      </Card>

      {/* Activity Timeline */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Recent Activity Timeline</CardTitle>
          <CardDescription className="text-sm">Latest updates and events across your campaigns</CardDescription>
        </CardHeader>
        <CardContent className="p-4">
          <div className="relative">
            {/* Timeline line */}
            <div className="absolute left-6 top-0 bottom-0 w-0.5 bg-border"></div>
            
            <div className="space-y-4 max-h-80 overflow-y-auto">
              {recentActivities.map((activity, index) => (
                <div key={activity.id} className="relative flex items-start space-x-3 p-2 hover:bg-gray-50 rounded-lg transition-colors">
                  {/* Timeline dot */}
                  <div className={`relative z-10 w-2.5 h-2.5 rounded-full mt-2 flex-shrink-0 border-2 border-white shadow-sm bg-${activity.color}-500`} />
                  
                  <div className="flex-1 min-w-0 pt-1">
                    <div className="flex items-start justify-between">
                      <div>
                        <p className="text-xs font-medium">{activity.title}</p>
                        <p className="text-xs text-muted-foreground mt-1">{activity.description}</p>
                      </div>
                      <Badge variant="outline" className="text-xs ml-3">
                        {activity.type}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground mt-2 flex items-center">
                      <Clock className="w-3 h-3 mr-1" />
                      {new Date(activity.timestamp).toLocaleString('en-US', { 
                        month: 'short', 
                        day: 'numeric',
                        hour: 'numeric',
                        minute: '2-digit',
                        hour12: true
                      })}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}