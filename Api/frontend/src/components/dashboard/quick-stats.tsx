import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Mail, Target, Play, Users, Send, Eye, Reply, Activity, AlertTriangle } from 'lucide-react';

interface Stats {
  totalEmailAccounts: number;
  totalCampaigns: number;
  activeCampaigns: number;
  totalClients: number;
  totalEmailsSent: number;
  openRate: number;
  replyRate: number;
  clickRate: number;
  bounceRate: number;
}

interface QuickStatsProps {
  stats: Stats;
}

export function QuickStats({ stats }: QuickStatsProps) {
  const formatRate = (rate: number) => `${rate.toFixed(1)}%`;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">Quick Stats</CardTitle>
        <CardDescription className="text-sm">Key metrics at a glance</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3 p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Mail className="w-4 h-4 text-blue-500" />
            <span className="text-xs font-medium">Email Accounts</span>
          </div>
          <span className="text-sm font-medium">{stats.totalEmailAccounts}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Target className="w-4 h-4 text-green-500" />
            <span className="text-xs font-medium">Total Campaigns</span>
          </div>
          <span className="text-sm font-medium">{stats.totalCampaigns}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Play className="w-4 h-4 text-emerald-500" />
            <span className="text-xs font-medium">Active Campaigns</span>
          </div>
          <span className="text-sm font-medium">{stats.activeCampaigns}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Users className="w-4 h-4 text-purple-500" />
            <span className="text-xs font-medium">Clients</span>
          </div>
          <span className="text-sm font-medium">{stats.totalClients}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Send className="w-4 h-4 text-blue-600" />
            <span className="text-xs font-medium">Total Emails Sent</span>
          </div>
          <span className="text-sm font-medium">{stats.totalEmailsSent?.toLocaleString()}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Eye className="w-4 h-4 text-teal-500" />
            <span className="text-xs font-medium">Open Rate</span>
          </div>
          <span className="text-sm font-medium">{formatRate(stats.openRate || 0)}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Reply className="w-4 h-4 text-green-600" />
            <span className="text-xs font-medium">Reply Rate</span>
          </div>
          <span className="text-sm font-medium">{formatRate(stats.replyRate || 0)}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Activity className="w-4 h-4 text-orange-500" />
            <span className="text-xs font-medium">Click Rate</span>
          </div>
          <span className="text-sm font-medium">{formatRate(stats.clickRate || 0)}</span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <AlertTriangle className="w-4 h-4 text-red-500" />
            <span className="text-xs font-medium">Bounce Rate</span>
          </div>
          <span className="text-sm font-medium">{formatRate(stats.bounceRate || 0)}</span>
        </div>
      </CardContent>
    </Card>
  );
}