'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { PieChart, Pie, Cell, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';
import { 
  ChartContainer, 
  ChartTooltip, 
  ChartTooltipContent, 
  ChartLegend, 
  ChartLegendContent,
  type ChartConfig 
} from '@/components/ui/chart';

interface Client {
  id: string;
  name: string;
  totalSent: number;
  openRate: number;
  replyRate: number;
  campaignCount: number;
  emailAccountCount?: number;
  color: string;
  lastActivity: string;
}

interface ClientRankingsProps {
  topClients: Client[];
}

const COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#06B6D4', '#F97316', '#EC4899'];

const clientChartConfig: ChartConfig = {
  totalSent: {
    label: 'Emails Sent',
    color: 'hsl(var(--chart-1))',
  },
  openRate: {
    label: 'Open Rate',
    color: 'hsl(var(--chart-2))',
  },
  replyRate: {
    label: 'Reply Rate',
    color: 'hsl(var(--chart-3))',
  },
};

export function ClientRankings({ topClients }: ClientRankingsProps) {
  const formatNumber = (num: number) => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toString();
  };

  const formatRate = (rate: number) => {
    return `${(rate * 100).toFixed(1)}%`;
  };

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Client Email Volume Pie Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Email Volume Distribution</CardTitle>
            <CardDescription>Emails sent by client</CardDescription>
          </CardHeader>
          <CardContent>
            <ChartContainer config={clientChartConfig} className="h-64">
              <PieChart>
                <Pie
                  data={topClients.map((client, index) => ({
                    ...client,
                    fill: COLORS[index % COLORS.length]
                  }))}
                  dataKey="totalSent"
                  nameKey="name"
                  cx="50%"
                  cy="50%"
                  outerRadius={80}
                  label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                >
                  {topClients.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <ChartTooltip content={<ChartTooltipContent />} />
              </PieChart>
            </ChartContainer>
          </CardContent>
        </Card>

        {/* Client Performance Comparison */}
        <Card>
          <CardHeader>
            <CardTitle>Client Performance</CardTitle>
            <CardDescription>Open and reply rates comparison</CardDescription>
          </CardHeader>
          <CardContent>
            <ChartContainer config={clientChartConfig} className="h-64">
              <BarChart data={topClients.slice(0, 5)}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis 
                  dataKey="name" 
                  fontSize={11}
                  tickFormatter={(value) => value.length > 12 ? `${value.substring(0, 12)}...` : value}
                />
                <YAxis fontSize={11} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <ChartLegend content={<ChartLegendContent />} />
                <Bar dataKey="openRate" fill="var(--color-openRate)" radius={4} />
                <Bar dataKey="replyRate" fill="var(--color-replyRate)" radius={4} />
              </BarChart>
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      {/* Client Details List */}
      <Card>
        <CardHeader>
          <CardTitle>Top Performing Clients</CardTitle>
          <CardDescription>Clients ranked by email volume and engagement</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {topClients.map((client, index) => (
              <div key={client.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                <div className="flex items-center space-x-4">
                  <div className="flex items-center justify-center w-10 h-10 rounded-full bg-primary/10 text-primary font-bold">
                    #{index + 1}
                  </div>
                  <div className="flex items-center space-x-3">
                    <div 
                      className="w-6 h-6 rounded-full border-2 border-white shadow-sm"
                      style={{ backgroundColor: client.color }}
                    />
                    <div>
                      <h4 className="font-semibold text-lg">{client.name}</h4>
                      <p className="text-sm text-muted-foreground">
                        {client.campaignCount} campaigns • {client.emailAccountCount || 0} email accounts
                      </p>
                      <div className="flex items-center gap-4 mt-1">
                        <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded">
                          {formatRate(client.openRate)} open rate
                        </span>
                        <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                          {formatRate(client.replyRate)} reply rate
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-xl font-bold text-blue-600">{formatNumber(client.totalSent)}</div>
                  <p className="text-xs text-muted-foreground">Emails Sent</p>
                  <p className="text-xs text-muted-foreground mt-1">
                    Last activity: {new Date(client.lastActivity).toLocaleDateString()}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}