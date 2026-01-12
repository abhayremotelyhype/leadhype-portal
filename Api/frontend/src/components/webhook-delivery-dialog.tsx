"use client"

import React, { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Skeleton, SkeletonTable } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { 
  CheckCircle, 
  XCircle, 
  Clock, 
  AlertTriangle, 
  RefreshCw, 
  Activity,
  Calendar,
  Hash,
  MessageCircle,
  AlertCircle,
  ChevronRight,
  Loader2,
  ChevronLeft,
  Eye,
  Copy,
  ExternalLink
} from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { apiClient, ENDPOINTS, formatDateTime, handleApiErrorWithToast } from '@/lib/api'
import { formatEventType } from '@/lib/utils'
import type { WebhookDelivery } from '@/types'

interface WebhookDeliveryDialogProps {
  open: boolean
  onClose: () => void
  webhookId: string | null
  webhookName?: string
  showFailuresOnly?: boolean
}

interface PaginationInfo {
  currentPage: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
  hasPrevious: boolean
}

export function WebhookDeliveryDialog({ open, onClose, webhookId, webhookName, showFailuresOnly = false }: WebhookDeliveryDialogProps) {
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([])
  const [loading, setLoading] = useState(false)
  const [paginationLoading, setPaginationLoading] = useState(false)
  const [selectedDelivery, setSelectedDelivery] = useState<WebhookDelivery | null>(null)
  const [pagination, setPagination] = useState<PaginationInfo>({
    currentPage: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasNext: false,
    hasPrevious: false
  })
  const { toast } = useToast()

  const fetchDeliveries = async (page: number = 1, isPagination: boolean = false) => {
    if (!webhookId) return

    try {
      if (isPagination) {
        setPaginationLoading(true)
        setSelectedDelivery(null) // Clear selection on pagination
      } else {
        setLoading(true)
        setSelectedDelivery(null)
      }

      const params: Record<string, string> = {
        page: page.toString(),
        pageSize: '20'
      }

      // Add failuresOnly parameter if needed
      if (showFailuresOnly) {
        params.failuresOnly = 'true'
      }

      const response = await apiClient.get<{
        data: WebhookDelivery[]
        pagination: PaginationInfo
      }>(
        `${ENDPOINTS.webhooks}/${webhookId}/deliveries`,
        params
      )
      
      setDeliveries(response.data)
      setPagination(response.pagination)
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch webhook deliveries', toast)
    } finally {
      if (isPagination) {
        setPaginationLoading(false)
      } else {
        setLoading(false)
      }
    }
  }

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= pagination.totalPages && newPage !== pagination.currentPage) {
      fetchDeliveries(newPage, true) // Pass true to indicate pagination loading
    }
  }

  useEffect(() => {
    if (open && webhookId) {
      fetchDeliveries(1)
    }
  }, [open, webhookId])

  const getStatusConfig = (delivery: WebhookDelivery) => {
    if (!delivery.statusCode) {
      return {
        icon: Clock,
        color: 'text-yellow-600',
        variant: 'secondary' as const,
        label: 'Pending'
      }
    }

    if (delivery.statusCode >= 200 && delivery.statusCode < 300) {
      return {
        icon: CheckCircle,
        color: 'text-green-600',
        variant: 'default' as const,
        label: 'Success'
      }
    }

    if (delivery.statusCode >= 400 && delivery.statusCode < 500) {
      return {
        icon: AlertTriangle,
        color: 'text-orange-600',
        variant: 'secondary' as const,
        label: 'Client Error'
      }
    }

    if (delivery.statusCode >= 500) {
      return {
        icon: XCircle,
        color: 'text-red-600',
        variant: 'destructive' as const,
        label: 'Server Error'
      }
    }

    return {
      icon: AlertCircle,
      color: 'text-gray-600',
      variant: 'secondary' as const,
      label: 'Unknown'
    }
  }


  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
    toast({
      title: 'Copied',
      description: 'Content copied to clipboard',
    })
  }

  const formatResponseBody = (responseBody: string) => {
    // Check if it's JSON
    try {
      const parsed = JSON.parse(responseBody)
      return {
        type: 'json',
        content: JSON.stringify(parsed, null, 2)
      }
    } catch {
      // Check if it's HTML
      if (responseBody.trim().startsWith('<') && responseBody.includes('</')) {
        return {
          type: 'html',
          content: responseBody
        }
      }
      // Default to plain text
      return {
        type: 'text',
        content: responseBody
      }
    }
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-6xl w-full h-[85vh] max-h-[800px] p-0 gap-0">
        <DialogHeader className="px-6 pt-6 pb-4 border-b bg-muted/20 flex-shrink-0">
          <DialogTitle className="text-xl font-semibold flex items-center gap-2">
            {showFailuresOnly ? (
              <>
                <AlertTriangle className="h-5 w-5 text-red-500" />
                Webhook Failures
                {webhookName && <span className="text-muted-foreground">- {webhookName}</span>}
              </>
            ) : (
              <>
                <Activity className="h-5 w-5" />
                Webhook Delivery History
              </>
            )}
          </DialogTitle>
          <DialogDescription>
            {showFailuresOnly 
              ? 'Failed webhook delivery attempts with error details and debugging information'
              : 'View delivery logs, status codes, and responses for webhook events'
            }
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-1 overflow-hidden">
          {/* Left Panel - Deliveries List */}
          <div className="w-1/2 flex flex-col border-r">
            {/* Stats Bar */}
            <div className="px-6 py-3 bg-background border-b flex items-center justify-between">
              <div className="flex items-center gap-4 text-sm">
                <span className="text-muted-foreground">
                  Total: <strong>{pagination.totalCount}</strong>
                </span>
                {pagination.totalCount > 0 && (
                  <span className="text-muted-foreground">
                    Page: <strong>{pagination.currentPage}</strong> of <strong>{pagination.totalPages}</strong>
                  </span>
                )}
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => fetchDeliveries(pagination.currentPage)}
                disabled={loading}
                className="gap-2"
              >
                <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
            </div>

            {/* Content Area */}
            <div className="flex-1 overflow-hidden">
              {loading && deliveries.length === 0 ? (
                <div className="p-6">
                  <SkeletonTable rows={8} columns={3} />
                </div>
              ) : deliveries.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-center">
                  {showFailuresOnly ? (
                    <>
                      <XCircle className="h-16 w-16 text-muted-foreground/30 mb-4" />
                      <h3 className="text-lg font-semibold mb-2">No Failed Deliveries</h3>
                      <p className="text-muted-foreground max-w-md">
                        This webhook has no failed delivery attempts.
                      </p>
                    </>
                  ) : (
                    <>
                      <MessageCircle className="h-16 w-16 text-muted-foreground/30 mb-4" />
                      <h3 className="text-lg font-semibold mb-2">No deliveries found</h3>
                      <p className="text-muted-foreground max-w-md">
                        This webhook hasn't been triggered yet. Deliveries will appear here once events are sent.
                      </p>
                    </>
                  )}
                </div>
              ) : (
                <div className="h-full overflow-y-auto">
                  <div className="p-6">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-[100px]">Status</TableHead>
                          <TableHead>Event</TableHead>
                          <TableHead className="w-[140px]">Created</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {paginationLoading ? (
                          // Skeleton rows during pagination
                          Array.from({ length: pagination.pageSize }, (_, i) => (
                            <TableRow key={`skeleton-${i}`}>
                              <TableCell>
                                <div className="flex items-center gap-1">
                                  <Skeleton className="h-3 w-3 rounded-full" />
                                  <Skeleton className="h-4 w-12 rounded" />
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="space-y-1">
                                  <Skeleton className="h-4 w-32" />
                                  <Skeleton className="h-3 w-24" />
                                </div>
                              </TableCell>
                              <TableCell>
                                <Skeleton className="h-3 w-20" />
                              </TableCell>
                            </TableRow>
                          ))
                        ) : (
                          deliveries.map((delivery) => {
                            const statusConfig = getStatusConfig(delivery)
                            
                            return (
                              <TableRow 
                                key={delivery.id}
                                className={`cursor-pointer hover:bg-muted/50 transition-colors ${
                                  selectedDelivery?.id === delivery.id ? 'bg-primary/5 border-primary/20' : ''
                                }`}
                                onClick={() => setSelectedDelivery(selectedDelivery?.id === delivery.id ? null : delivery)}
                              >
                                <TableCell>
                                  <div className="flex items-center gap-1">
                                    <statusConfig.icon className={`h-3 w-3 ${statusConfig.color}`} />
                                    <Badge variant={statusConfig.variant} className="font-mono text-xs px-1.5 py-0.5">
                                      {delivery.statusCode || 'Pending'}
                                    </Badge>
                                  </div>
                                </TableCell>
                                <TableCell>
                                  <div className="font-medium text-sm">
                                    {formatEventType(delivery.eventType)}
                                  </div>
                                  <div className="text-xs text-muted-foreground flex items-center gap-1">
                                    {delivery.attemptCount > 1 && (
                                      <>
                                        <RefreshCw className="h-3 w-3" />
                                        <span>Retry #{delivery.attemptCount}</span>
                                      </>
                                    )}
                                    {delivery.deliveredAt && (
                                      <span>• {formatDateTime(delivery.deliveredAt)}</span>
                                    )}
                                  </div>
                                </TableCell>
                                <TableCell>
                                  <div className="text-xs text-muted-foreground">
                                    {formatDateTime(delivery.createdAt)}
                                  </div>
                                </TableCell>
                              </TableRow>
                            )
                          })
                        )}
                      </TableBody>
                    </Table>
                  </div>
                </div>
              )}
            </div>

            {/* Pagination */}
            {!loading && pagination.totalPages > 1 && (
              <div className="px-6 py-4 border-t bg-muted/10 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handlePageChange(pagination.currentPage - 1)}
                    disabled={!pagination.hasPrevious || paginationLoading}
                    className="gap-1 transition-all"
                  >
                    {paginationLoading ? (
                      <Loader2 className="h-3 w-3 animate-spin" />
                    ) : (
                      <ChevronLeft className="h-3 w-3" />
                    )}
                    Prev
                  </Button>
                  <div className="flex items-center gap-1">
                    {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
                      let pageNum: number
                      if (pagination.totalPages <= 5) {
                        pageNum = i + 1
                      } else {
                        const start = Math.max(1, pagination.currentPage - 2)
                        const end = Math.min(pagination.totalPages, start + 4)
                        pageNum = start + i
                        if (pageNum > end) return null
                      }
                      
                      return (
                        <Button
                          key={pageNum}
                          variant={pageNum === pagination.currentPage ? "default" : "outline"}
                          size="sm"
                          onClick={() => handlePageChange(pageNum)}
                          disabled={paginationLoading}
                          className="h-7 w-7 p-0 text-xs transition-all"
                        >
                          {pageNum}
                        </Button>
                      )
                    })}
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handlePageChange(pagination.currentPage + 1)}
                    disabled={!pagination.hasNext || paginationLoading}
                    className="gap-1 transition-all"
                  >
                    Next
                    {paginationLoading ? (
                      <Loader2 className="h-3 w-3 animate-spin" />
                    ) : (
                      <ChevronRight className="h-3 w-3" />
                    )}
                  </Button>
                </div>
                <div className="text-xs text-muted-foreground">
                  {paginationLoading ? (
                    <span className="flex items-center gap-1">
                      <Loader2 className="h-3 w-3 animate-spin" />
                      Loading...
                    </span>
                  ) : (
                    <span>
                      Showing {((pagination.currentPage - 1) * pagination.pageSize) + 1} to{' '}
                      {Math.min(pagination.currentPage * pagination.pageSize, pagination.totalCount)} of{' '}
                      {pagination.totalCount} deliveries
                    </span>
                  )}
                </div>
              </div>
            )}
          </div>

          {/* Right Panel - Details */}
          <div className="w-1/2 bg-muted/10 flex flex-col transition-all duration-200">
            <div className="opacity-100 transition-opacity duration-200">
              {selectedDelivery ? (
                <>
                  <div className="p-6 border-b bg-background">
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="font-semibold text-lg mb-1">
                          {formatEventType(selectedDelivery.eventType)}
                        </h3>
                        <p className="text-sm text-muted-foreground">
                          Delivery ID: {selectedDelivery.id}
                        </p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setSelectedDelivery(null)}
                        className="h-8 w-8 p-0 hover:bg-muted transition-colors"
                      >
                        ×
                      </Button>
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="text-sm font-medium text-muted-foreground">Status</label>
                        <div className="mt-1">
                          {(() => {
                            const statusConfig = getStatusConfig(selectedDelivery)
                            return (
                              <div className="flex items-center gap-2">
                                <statusConfig.icon className={`h-4 w-4 ${statusConfig.color}`} />
                                <Badge variant={statusConfig.variant}>
                                  {selectedDelivery.statusCode || 'Pending'}
                                </Badge>
                                <span className="text-sm text-muted-foreground">
                                  {statusConfig.label}
                                </span>
                              </div>
                            )
                          })()}
                        </div>
                      </div>
                      <div>
                        <label className="text-sm font-medium text-muted-foreground">Attempts</label>
                        <div className="mt-1 flex items-center gap-1">
                          {selectedDelivery.attemptCount > 1 && (
                            <RefreshCw className="h-3 w-3 text-muted-foreground" />
                          )}
                          <span>{selectedDelivery.attemptCount}</span>
                        </div>
                      </div>
                      <div>
                        <label className="text-sm font-medium text-muted-foreground">Created</label>
                        <div className="mt-1 text-sm">
                          {formatDateTime(selectedDelivery.createdAt)}
                        </div>
                      </div>
                      <div>
                        <label className="text-sm font-medium text-muted-foreground">Delivered</label>
                        <div className="mt-1 text-sm">
                          {selectedDelivery.deliveredAt ? formatDateTime(selectedDelivery.deliveredAt) : 'Not delivered'}
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="flex-1 p-6 overflow-y-auto">
                    <div className="space-y-4 h-full">
                      {/* Response Body */}
                      {selectedDelivery.responseBody && (
                        <div className="animate-in fade-in-50 duration-200 flex-1">
                          <div className="flex items-center justify-between mb-2">
                            <h4 className="font-medium">Response Body</h4>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => copyToClipboard(selectedDelivery.responseBody || '')}
                              className="h-6 gap-1 hover:bg-muted transition-colors"
                            >
                              <Copy className="h-3 w-3" />
                              Copy
                            </Button>
                          </div>
                          <Card className="h-full">
                            <CardContent className="p-4 h-full">
                              {(() => {
                                const formatted = formatResponseBody(selectedDelivery.responseBody!)
                                
                                if (formatted.type === 'html') {
                                  return (
                                    <div className="space-y-2 flex flex-col h-[400px]">
                                      <div className="text-xs text-muted-foreground flex-shrink-0">HTML Content (formatted for readability):</div>
                                      <pre className="text-xs font-mono whitespace-pre-wrap break-all bg-gray-50 p-3 rounded overflow-y-scroll flex-1 min-h-0">
                                        {formatted.content}
                                      </pre>
                                    </div>
                                  )
                                } else if (formatted.type === 'json') {
                                  return (
                                    <div className="space-y-2 flex flex-col h-[400px]">
                                      <div className="text-xs text-muted-foreground flex-shrink-0">JSON Response (formatted):</div>
                                      <pre className="text-xs font-mono whitespace-pre-wrap bg-blue-50 p-3 rounded overflow-y-scroll flex-1 min-h-0">
                                        {formatted.content}
                                      </pre>
                                    </div>
                                  )
                                } else {
                                  return (
                                    <pre className="text-xs font-mono whitespace-pre-wrap break-all overflow-y-scroll h-[400px] p-3 rounded border">
                                      {formatted.content}
                                    </pre>
                                  )
                                }
                              })()
                              }
                            </CardContent>
                          </Card>
                        </div>
                      )}

                      {/* Error Message */}
                      {selectedDelivery.errorMessage && (
                        <div className="animate-in fade-in-50 duration-200">
                          <div className="flex items-center justify-between mb-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => copyToClipboard(selectedDelivery.errorMessage || '')}
                              className="h-6 gap-1 hover:bg-muted transition-colors ml-auto"
                            >
                              <Copy className="h-3 w-3" />
                              Copy
                            </Button>
                          </div>
                          <Alert variant="destructive">
                            <AlertCircle />
                            <AlertTitle>Delivery Error</AlertTitle>
                            <AlertDescription>{selectedDelivery.errorMessage}</AlertDescription>
                          </Alert>
                        </div>
                      )}
                    </div>
                  </div>
                </>
              ) : (
                <div className="flex flex-col items-center justify-center h-full text-center animate-in fade-in-50 duration-200">
                  <Eye className="h-16 w-16 text-muted-foreground/30 mb-4" />
                  <h3 className="text-lg font-semibold mb-2">Select a delivery</h3>
                  <p className="text-muted-foreground max-w-sm">
                    Click on any delivery from the list to view detailed information about the webhook event, status, and response.
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}