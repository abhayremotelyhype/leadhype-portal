"use client"

import React from 'react'
import { ChartConfig } from '@/components/ui/chart'

interface FallbackLineChartProps {
  data: Array<Record<string, any>>
  config: ChartConfig
  height?: number
}

export function FallbackLineChart({ 
  data, 
  config,
  height = 200 
}: FallbackLineChartProps) {
  if (!data.length) {
    return (
      <div className="flex h-[200px] w-full items-center justify-center">
        <div className="text-center">
          <div className="text-sm text-muted-foreground">No data available</div>
        </div>
      </div>
    )
  }

  const dataKeys = Object.keys(config).filter(key => key !== 'date')
  const maxValues = dataKeys.map(key => Math.max(...data.map(d => d[key] || 0)))
  const globalMax = Math.max(...maxValues)
  const width = 600
  const padding = { top: 20, right: 20, bottom: 40, left: 50 }

  const getPath = (key: string) => {
    const color = config[key]?.color || 'var(--color-chart-1)'
    const points = data.map((d, i) => {
      const x = (i * (width - padding.left - padding.right)) / (data.length - 1) + padding.left
      const y = height - padding.bottom - ((d[key] || 0) * (height - padding.top - padding.bottom)) / globalMax
      return `${x},${y}`
    }).join(' ')
    
    return (
      <polyline
        key={key}
        fill="none"
        stroke={color}
        strokeWidth="2"
        points={points}
        className="drop-shadow-sm"
      />
    )
  }

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr)
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  }

  return (
    <div className="w-full">
      <svg 
        width="100%" 
        height={height} 
        viewBox={`0 0 ${width} ${height}`} 
        className="overflow-visible"
      >
        {/* Grid lines - subtle like shadcn/ui */}
        <defs>
          <pattern id="smallGrid" width="40" height="30" patternUnits="userSpaceOnUse">
            <path 
              d={`M 40 0 L 0 0 0 30`} 
              fill="none" 
              stroke="hsl(var(--border))" 
              strokeWidth="0.5" 
              opacity="0.2"
            />
          </pattern>
        </defs>
        <rect 
          x={padding.left} 
          y={padding.top} 
          width={width - padding.left - padding.right} 
          height={height - padding.top - padding.bottom} 
          fill="url(#smallGrid)" 
        />
        
        {/* Chart lines */}
        {dataKeys.map((key) => getPath(key))}
        
        {/* Data points */}
        {dataKeys.map((key) => {
          const color = config[key]?.color || 'var(--color-chart-1)'
          return data.map((d, i) => {
            const x = (i * (width - padding.left - padding.right)) / (data.length - 1) + padding.left
            const y = height - padding.bottom - ((d[key] || 0) * (height - padding.top - padding.bottom)) / globalMax
            return (
              <circle
                key={`${key}-${i}`}
                cx={x}
                cy={y}
                r="3"
                fill={color}
                className="opacity-80 hover:opacity-100"
              />
            )
          })
        })}
        
        {/* Y-axis labels */}
        <text 
          x={padding.left - 10} 
          y={padding.top + 5} 
          fontSize="11" 
          fill="hsl(var(--muted-foreground))" 
          textAnchor="end"
          className="text-xs"
        >
          {globalMax.toLocaleString()}
        </text>
        <text 
          x={padding.left - 10} 
          y={height - padding.bottom + 5} 
          fontSize="11" 
          fill="hsl(var(--muted-foreground))" 
          textAnchor="end"
          className="text-xs"
        >
          0
        </text>
        
        {/* X-axis labels - first and last */}
        {data.length > 0 && (
          <>
            <text 
              x={padding.left} 
              y={height - 15} 
              fontSize="10" 
              fill="hsl(var(--muted-foreground))" 
              textAnchor="start"
              className="text-xs"
            >
              {formatDate(data[0].date)}
            </text>
            <text 
              x={width - padding.right} 
              y={height - 15} 
              fontSize="10" 
              fill="hsl(var(--muted-foreground))" 
              textAnchor="end"
              className="text-xs"
            >
              {formatDate(data[data.length - 1].date)}
            </text>
          </>
        )}
      </svg>
    </div>
  )
}