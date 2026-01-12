import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Send, Eye, Reply, Target, TrendingUp, TrendingDown, Minus } from 'lucide-react';

interface Stats {
  totalEmailsSent: number;
  totalEmailsSentChange?: number;
  recentEmailsSent: number;
  openRate: number;
  openRateChange: number;
  totalEmailsOpened: number;
  replyRate: number;
  replyRateChange: number;
  totalEmailsReplied: number;
  totalCampaigns: number;
  activeCampaigns: number;
  pausedCampaigns: number;
  completedCampaigns: number;
  totalPositiveReplies: number;
  positiveReplyRate: number;
  positiveReplyRateChange: number;
}

interface KPICardsProps {
  stats: Stats;
}

export function KPICards({ stats }: KPICardsProps) {
  const formatNumber = (num: number) => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toLocaleString();
  };

  const formatRate = (rate: number) => `${rate.toFixed(1)}%`;

  const getChangeData = (change: number) => {
    const isPositive = change > 0;
    const isNegative = change < 0;
    
    return {
      trend: isPositive ? 'up' : isNegative ? 'down' : 'neutral',
      color: isPositive ? 'text-green-600' : isNegative ? 'text-red-600' : 'text-gray-500',
      icon: isPositive ? TrendingUp : isNegative ? TrendingDown : Minus,
      text: change === 0 ? 'No change' : `${change > 0 ? '+' : ''}${change.toFixed(1)}%`
    };
  };

  const metrics = [
    {
      title: "Total Emails Sent",
      value: formatNumber(stats.totalEmailsSent),
      change: stats.totalEmailsSentChange || 0,
      subtitle: "Steady growth trend",
      description: `${formatNumber(stats.recentEmailsSent)} in last 7 days`,
      icon: Send,
      iconColor: "text-blue-500",
    },
    {
      title: "Open Rate",
      value: formatRate(stats.openRate),
      change: stats.openRateChange,
      subtitle: "Performance this period",
      description: `${formatNumber(stats.totalEmailsOpened)} total opens`,
      icon: Eye,
      iconColor: "text-green-500",
    },
    {
      title: "Reply Rate",
      value: formatRate(stats.replyRate),
      change: stats.replyRateChange,
      subtitle: "Engagement performance",
      description: `${formatNumber(stats.totalEmailsReplied)} total replies`,
      icon: Reply,
      iconColor: "text-orange-500",
    },
    {
      title: "Positive Replies",
      value: formatRate(stats.positiveReplyRate),
      change: stats.positiveReplyRateChange,
      subtitle: "Quality engagement",
      description: `${formatNumber(stats.totalPositiveReplies)} of ${formatNumber(stats.totalEmailsReplied)} replies`,
      icon: Reply,
      iconColor: "text-emerald-500",
    },
    {
      title: "Total Campaigns",
      value: stats.totalCampaigns.toString(),
      change: 0, // No change data for campaigns typically
      subtitle: `${stats.activeCampaigns} active`,
      description: `${stats.pausedCampaigns} paused • ${stats.completedCampaigns} completed`,
      icon: Target,
      iconColor: "text-purple-500",
    },
  ];

  return (
    <div className="space-y-6">
      <div className="space-y-1 mb-6">
        <h2 className="text-lg font-semibold">Overview</h2>
        <p className="text-sm text-muted-foreground">Key metrics from your campaigns</p>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
        {metrics.map((metric, index) => {
          const Icon = metric.icon;
          const changeData = getChangeData(metric.change);
          const TrendIcon = changeData.icon;
          
          return (
            <Card key={index} className="hover:shadow-md transition-shadow rounded-xl">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2 pt-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">{metric.title}</CardTitle>
                <div className="flex flex-col items-end space-y-0.5">
                  <div className="flex items-center space-x-1">
                    <TrendIcon className={`h-4 w-4 ${changeData.color.replace('text-', 'text-')}`} />
                    <span
                      className={`text-xs font-medium ${changeData.color}`}
                    >
                      {changeData.text}
                    </span>
                  </div>
                  <span className="text-xs text-muted-foreground">from last week</span>
                </div>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="text-2xl font-bold">{metric.value}</div>
                <div className="mt-1 flex items-center space-x-1">
                  <Icon className={`h-3 w-3 ${metric.iconColor}`} />
                  <p className="text-xs text-muted-foreground">
                    {(() => {
                      const change = metric.change;
                      if (change === 0) return 'No change';
                      if (change > 0) {
                        if (metric.title.includes('Rate')) return 'Improving performance';
                        if (metric.title.includes('Campaigns')) return 'Growing activity';
                        return 'Trending upward';
                      } else {
                        if (metric.title.includes('Rate')) return 'Needs attention';
                        if (metric.title.includes('Campaigns')) return 'Activity down';
                        return 'Declining trend';
                      }
                    })()}
                  </p>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">{metric.description}</p>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}