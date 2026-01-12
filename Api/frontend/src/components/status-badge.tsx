'use client';

import React, { useEffect, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { CheckCircle, XCircle, Clock, AlertCircle, PlayCircle, PauseCircle } from 'lucide-react';

interface StatusConfig {
  className: string;
  icon?: React.ReactNode;
  label?: string;
}

interface StatusBadgeProps {
  status: string;
  type: 'emailAccount' | 'campaign';
  className?: string;
}

// Email Account Status Configuration
const emailAccountStatusConfig: Record<string, StatusConfig> = {
  active: {
    className: 'border',
    icon: <CheckCircle className="w-3 h-3" />,
    label: 'Active'
  },
  inactive: {
    className: 'border',
    icon: <XCircle className="w-3 h-3" />,
    label: 'Inactive'
  },
  pending: {
    className: 'border',
    icon: <Clock className="w-3 h-3" />,
    label: 'Pending'
  },
  error: {
    className: 'border',
    icon: <AlertCircle className="w-3 h-3" />,
    label: 'Error'
  },
  warmup: {
    className: 'border',
    icon: <Clock className="w-3 h-3" />,
    label: 'Warmup'
  }
};

// Campaign Status Configuration
const campaignStatusConfig: Record<string, StatusConfig> = {
  active: {
    className: 'border',
    icon: <PlayCircle className="w-3 h-3" />,
    label: 'Active'
  },
  paused: {
    className: 'border',
    icon: <PauseCircle className="w-3 h-3" />,
    label: 'Paused'
  },
  completed: {
    className: 'border',
    icon: <CheckCircle className="w-3 h-3" />,
    label: 'Completed'
  },
  draft: {
    className: 'border',
    icon: <Clock className="w-3 h-3" />,
    label: 'Draft'
  },
  stopped: {
    className: 'border',
    icon: <XCircle className="w-3 h-3" />,
    label: 'Stopped'
  }
};

export function StatusBadge({ status, type, className }: StatusBadgeProps) {
  const [isDark, setIsDark] = useState(false);

  useEffect(() => {
    // Check if dark class exists on html or document element
    const checkDarkMode = () => {
      const isDarkMode = document.documentElement.classList.contains('dark');
      setIsDark(isDarkMode);
    };

    checkDarkMode();

    // Watch for changes to dark mode
    const observer = new MutationObserver(checkDarkMode);
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class']
    });

    return () => observer.disconnect();
  }, []);

  const statusConfig = type === 'emailAccount' ? emailAccountStatusConfig : campaignStatusConfig;
  const fallbackStatus = type === 'emailAccount' ? statusConfig.inactive : statusConfig.draft;
  const config = statusConfig[status?.toLowerCase() || ''] || fallbackStatus;

  // Define theme-aware colors
  const getStatusColors = (statusKey: string) => {
    const colors = {
      active: {
        bg: isDark ? 'hsl(142 76% 8% / 0.5)' : 'hsl(142 76% 96%)',
        text: isDark ? 'hsl(142 84% 65%)' : 'hsl(142 84% 24%)',
        border: isDark ? 'hsl(142 76% 20% / 0.3)' : 'hsl(142 76% 88%)'
      },
      paused: {
        bg: isDark ? 'hsl(48 96% 8% / 0.5)' : 'hsl(48 96% 96%)',
        text: isDark ? 'hsl(48 84% 65%)' : 'hsl(48 84% 24%)',
        border: isDark ? 'hsl(48 96% 20% / 0.3)' : 'hsl(48 96% 88%)'
      },
      completed: {
        bg: isDark ? 'hsl(213 96% 8% / 0.5)' : 'hsl(213 96% 96%)',
        text: isDark ? 'hsl(213 84% 65%)' : 'hsl(213 84% 24%)',
        border: isDark ? 'hsl(213 96% 20% / 0.3)' : 'hsl(213 96% 88%)'
      },
      draft: {
        bg: isDark ? 'hsl(210 40% 8% / 0.5)' : 'hsl(210 40% 96%)',
        text: isDark ? 'hsl(210 40% 65%)' : 'hsl(210 40% 24%)',
        border: isDark ? 'hsl(210 40% 20% / 0.3)' : 'hsl(210 40% 88%)'
      },
      stopped: {
        bg: isDark ? 'hsl(0 84% 8% / 0.5)' : 'hsl(0 84% 96%)',
        text: isDark ? 'hsl(0 84% 65%)' : 'hsl(0 84% 24%)',
        border: isDark ? 'hsl(0 84% 20% / 0.3)' : 'hsl(0 84% 88%)'
      },
      inactive: {
        bg: isDark ? 'hsl(210 40% 8% / 0.5)' : 'hsl(210 40% 96%)',
        text: isDark ? 'hsl(210 40% 65%)' : 'hsl(210 40% 24%)',
        border: isDark ? 'hsl(210 40% 20% / 0.3)' : 'hsl(210 40% 88%)'
      },
      pending: {
        bg: isDark ? 'hsl(48 96% 8% / 0.5)' : 'hsl(48 96% 96%)',
        text: isDark ? 'hsl(48 84% 65%)' : 'hsl(48 84% 24%)',
        border: isDark ? 'hsl(48 96% 20% / 0.3)' : 'hsl(48 96% 88%)'
      },
      error: {
        bg: isDark ? 'hsl(0 84% 8% / 0.5)' : 'hsl(0 84% 96%)',
        text: isDark ? 'hsl(0 84% 65%)' : 'hsl(0 84% 24%)',
        border: isDark ? 'hsl(0 84% 20% / 0.3)' : 'hsl(0 84% 88%)'
      },
      warmup: {
        bg: isDark ? 'hsl(213 96% 8% / 0.5)' : 'hsl(213 96% 96%)',
        text: isDark ? 'hsl(213 84% 65%)' : 'hsl(213 84% 24%)',
        border: isDark ? 'hsl(213 96% 20% / 0.3)' : 'hsl(213 96% 88%)'
      }
    };
    return colors[statusKey as keyof typeof colors] || colors.inactive;
  };

  const statusKey = status?.toLowerCase() || (type === 'emailAccount' ? 'inactive' : 'draft');
  const statusColors = getStatusColors(statusKey);

  return (
    <div 
      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border transition-colors ${className || ''}`}
      style={{
        backgroundColor: statusColors.bg,
        color: statusColors.text,
        borderColor: statusColors.border,
      }}
    >
      {config.icon}
      <span>
        {config.label || status || 'Unknown'}
      </span>
    </div>
  );
}