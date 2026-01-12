import { cn } from "@/lib/utils"

function Skeleton({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("skeleton skeleton-shimmer rounded-md bg-muted", className)}
      {...props}
    />
  )
}

function SkeletonTable({ rows = 5, columns = 4 }: { rows?: number; columns?: number }) {
  return (
    <div className="space-y-4">
      {/* Table Header */}
      <div className="flex space-x-4">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} className="h-4 flex-1" />
        ))}
      </div>
      
      {/* Table Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex space-x-4">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton 
              key={colIndex} 
              className={cn(
                "h-4 flex-1",
                colIndex === 0 && "w-16", // First column narrower
                colIndex === columns - 1 && "w-20" // Last column for actions
              )}
            />
          ))}
        </div>
      ))}
    </div>
  )
}

function SkeletonCard() {
  return (
    <div className="space-y-3 p-4 border rounded-lg">
      <Skeleton className="h-4 w-3/4" />
      <div className="space-y-2">
        <Skeleton className="h-3 w-full" />
        <Skeleton className="h-3 w-5/6" />
      </div>
      <div className="flex justify-between items-center">
        <Skeleton className="h-3 w-1/4" />
        <Skeleton className="h-8 w-20" />
      </div>
    </div>
  )
}

function SkeletonStats() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className="space-y-2 p-4 border rounded-lg">
          <Skeleton className="h-3 w-1/2" />
          <Skeleton className="h-8 w-3/4" />
          <Skeleton className="h-2 w-1/3" />
        </div>
      ))}
    </div>
  )
}

function SkeletonChart({ height = "h-64" }: { height?: string }) {
  return (
    <div className={cn("rounded-lg border p-4", height)}>
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-3 w-16" />
        </div>
        <div className="flex items-end space-x-2" style={{ height: '200px' }}>
          {Array.from({ length: 12 }).map((_, i) => (
            <Skeleton 
              key={i} 
              className="w-8 animate-pulse"
              style={{ 
                height: `${Math.random() * 60 + 40}%`,
                animationDelay: `${i * 100}ms`
              }}
            />
          ))}
        </div>
      </div>
    </div>
  )
}

export { 
  Skeleton, 
  SkeletonTable, 
  SkeletonCard, 
  SkeletonStats, 
  SkeletonChart 
}
