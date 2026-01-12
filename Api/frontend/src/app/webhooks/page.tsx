"use client"

import React, { useState, useEffect } from 'react'
import { Plus, Webhook as WebhookIcon, MoreHorizontal, Edit, Trash2, Eye, Play, Pause, Zap, Settings } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Skeleton } from '@/components/ui/skeleton'
import { toast } from 'sonner'
import { apiClient, ENDPOINTS, formatDateTime, handleApiErrorWithToast } from '@/lib/api'
import type { Webhook, WebhookEventConfig } from '@/types'
import { WebhookDialog } from '@/components/webhook-dialog'
import { WebhookDeliveryDialog } from '@/components/webhook-delivery-dialog'
import { WebhookEventsManagementDialog } from '@/components/webhook-events-management-dialog'
import { PageHeader } from '@/components/page-header'

export default function WebhooksPage() {
  const [webhooks, setWebhooks] = useState<Webhook[]>([])
  const [loading, setLoading] = useState(true)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [deliveryDialogOpen, setDeliveryDialogOpen] = useState(false)
  const [eventsManagementDialogOpen, setEventsManagementDialogOpen] = useState(false)
  const [selectedWebhook, setSelectedWebhook] = useState<Webhook | null>(null)
  const [selectedWebhookId, setSelectedWebhookId] = useState<string | null>(null)
  const [selectedWebhookForFailures, setSelectedWebhookForFailures] = useState<Webhook | null>(null)
  const [selectedWebhookForEvents, setSelectedWebhookForEvents] = useState<Webhook | null>(null)
  const [webhookEvents, setWebhookEvents] = useState<Record<string, WebhookEventConfig[]>>({})

  // Set page title
  useEffect(() => {
    document.title = 'Webhooks - LeadHype';
  }, []);

  const fetchWebhooks = async () => {
    try {
      setLoading(true)
      const response = await apiClient.get<Webhook[]>(ENDPOINTS.webhooks)
      setWebhooks(response)
      
      // Fetch events for each webhook
      await fetchWebhookEvents(response)
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch webhooks', toast)
    } finally {
      setLoading(false)
    }
  }

  const fetchWebhookEvents = async (webhookList: Webhook[] = webhooks) => {
    try {
      const allEvents = await apiClient.get<WebhookEventConfig[]>(ENDPOINTS.webhookEvents)
      
      // Group events by webhook ID
      const eventsByWebhook: Record<string, WebhookEventConfig[]> = {}
      webhookList.forEach(webhook => {
        eventsByWebhook[webhook.id] = allEvents.filter(event => event.webhookId === webhook.id)
      })
      
      setWebhookEvents(eventsByWebhook)
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch webhook events', toast)
    }
  }

  useEffect(() => {
    fetchWebhooks()
  }, [])

  const handleCreate = () => {
    setSelectedWebhook(null)
    setDialogOpen(true)
  }

  const handleEdit = (webhook: Webhook) => {
    setSelectedWebhook(webhook)
    setDialogOpen(true)
  }

  const handleDelete = async (webhook: Webhook) => {
    if (!confirm(`Are you sure you want to delete the webhook "${webhook.name}"?`)) {
      return
    }

    try {
      await apiClient.delete(`${ENDPOINTS.webhooks}/${webhook.id}`)
      toast.success('Webhook deleted successfully')
      fetchWebhooks()
    } catch (error) {
      handleApiErrorWithToast(error, 'delete webhook', toast)
    }
  }

  const handleToggleActive = async (webhook: Webhook) => {
    try {
      await apiClient.put(`${ENDPOINTS.webhooks}/${webhook.id}`, {
        isActive: !webhook.isActive
      })
      toast.success(`Webhook ${webhook.isActive ? 'deactivated' : 'activated'} successfully`)
      fetchWebhooks()
    } catch (error) {
      handleApiErrorWithToast(error, 'update webhook', toast)
    }
  }

  const handleTest = async (webhook: Webhook) => {
    try {
      await apiClient.post(`${ENDPOINTS.webhooks}/${webhook.id}/test`)
      toast.success('Test webhook has been sent successfully')
    } catch (error) {
      handleApiErrorWithToast(error, 'test webhook', toast)
    }
  }

  const handleViewDeliveries = (webhook: Webhook) => {
    setSelectedWebhookId(webhook.id)
    setDeliveryDialogOpen(true)
  }

  const handleViewFailures = (webhook: Webhook) => {
    setSelectedWebhookForFailures(webhook)
  }

  const handleWebhookSaved = () => {
    setDialogOpen(false)
    fetchWebhooks()
  }

  const handleManageEvents = (webhook: Webhook) => {
    setSelectedWebhookForEvents(webhook)
    setEventsManagementDialogOpen(true)
  }

  const handleEventsManagementClosed = () => {
    setEventsManagementDialogOpen(false)
    setSelectedWebhookForEvents(null)
    fetchWebhookEvents() // Refresh events
  }

  const getStatusBadge = (webhook: Webhook) => {
    if (!webhook.isActive) {
      return <Badge variant="secondary">Inactive</Badge>
    }
    if (webhook.failureCount > 0) {
      return <Badge variant="destructive">Issues</Badge>
    }
    return <Badge variant="default">Active</Badge>
  }

  const getEventsBadge = (webhook: Webhook) => {
    const events = webhookEvents[webhook.id] || []
    const activeEvents = events.filter(e => e.isActive).length
    
    if (events.length === 0) {
      return <span className="text-muted-foreground text-sm">No events</span>
    }
    
    return (
      <div className="flex gap-1">
        <Badge variant={activeEvents > 0 ? "default" : "secondary"}>
          {activeEvents} active
        </Badge>
        {events.length > activeEvents && (
          <Badge variant="secondary">
            {events.length - activeEvents} inactive
          </Badge>
        )}
      </div>
    )
  }


  if (loading) {
    return (
      <div className="container max-w-7xl mx-auto py-4 px-4 sm:py-6 space-y-4 sm:space-y-6">
        <div className="space-y-2">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-4 w-96" />
        </div>
        
        {/* Desktop Table View Skeleton */}
        <Card className="hidden lg:block">
          <CardHeader>
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-4 w-64" />
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <div className="space-y-4">
                <div className="flex items-center gap-4 pb-2 border-b">
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-4 w-8" />
                </div>
                {[...Array(3)].map((_, i) => (
                  <div key={i} className="flex items-center gap-4 py-3">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-6 w-12 rounded-full" />
                    <Skeleton className="h-5 w-16 rounded-full" />
                    <Skeleton className="h-5 w-20 rounded-full" />
                    <Skeleton className="h-4 w-8" />
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-4 w-16" />
                    <Skeleton className="h-8 w-8 rounded" />
                  </div>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Mobile Card View Skeleton */}
        <div className="lg:hidden space-y-4">
          <Skeleton className="h-6 w-40" />
          {[...Array(3)].map((_, i) => (
            <Card key={i}>
              <CardContent className="p-4">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex-1 min-w-0 space-y-2">
                    <Skeleton className="h-5 w-32" />
                    <Skeleton className="h-3 w-48" />
                  </div>
                  <div className="flex items-center gap-2 flex-shrink-0">
                    <Skeleton className="h-6 w-12 rounded-full" />
                    <Skeleton className="h-8 w-8 rounded" />
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-3 text-sm">
                  <div className="space-y-2">
                    <Skeleton className="h-3 w-12" />
                    <Skeleton className="h-5 w-16 rounded-full" />
                  </div>
                  <div className="space-y-2">
                    <Skeleton className="h-3 w-12" />
                    <Skeleton className="h-5 w-20 rounded-full" />
                  </div>
                  <div className="space-y-2">
                    <Skeleton className="h-3 w-20" />
                    <Skeleton className="h-3 w-16" />
                  </div>
                  <div className="space-y-2">
                    <Skeleton className="h-3 w-16" />
                    <Skeleton className="h-4 w-8" />
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="container max-w-7xl mx-auto py-4 px-4 sm:py-6 space-y-4 sm:space-y-6">
      <PageHeader
        title="Webhooks"
        description="Manage webhook endpoints for real-time event notifications"
        actions={
          <Button onClick={handleCreate} className="w-full sm:w-auto">
            <Plus className="h-4 w-4 mr-2" />
            <span className="hidden sm:inline">Create Webhook</span>
            <span className="sm:hidden">Create</span>
          </Button>
        }
      />

      {webhooks.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <WebhookIcon className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No webhooks configured</h3>
            <p className="text-muted-foreground text-center mb-4 max-w-md">
              Create your first webhook to receive real-time notifications when events occur in your campaigns.
            </p>
            <Button onClick={handleCreate}>
              <Plus className="h-4 w-4 mr-2" />
              Create Webhook
            </Button>
          </CardContent>
        </Card>
      ) : (
        <>
          {/* Desktop Table View */}
          <Card className="hidden lg:block">
            <CardHeader>
              <CardTitle>Webhooks ({webhooks.length})</CardTitle>
              <CardDescription>
                Real-time event notifications for your campaigns and email accounts
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Name</TableHead>
                      <TableHead>Active</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Events</TableHead>
                      <TableHead>Failures</TableHead>
                      <TableHead>URL</TableHead>
                      <TableHead>Last Triggered</TableHead>
                      <TableHead className="w-[70px]"></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {webhooks.map((webhook) => (
                      <TableRow key={webhook.id}>
                        <TableCell className="font-medium">
                          {webhook.name}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center">
                            <Switch
                              checked={webhook.isActive}
                              onCheckedChange={() => handleToggleActive(webhook)}
                              aria-label={`Toggle ${webhook.name}`}
                            />
                          </div>
                        </TableCell>
                        <TableCell>
                          {webhook.failureCount > 0 ? (
                            <Badge variant="destructive">Issues</Badge>
                          ) : (
                            <Badge variant="default">Healthy</Badge>
                          )}
                        </TableCell>
                        <TableCell>
                          {getEventsBadge(webhook)}
                        </TableCell>
                        <TableCell>
                          {webhook.failureCount > 0 ? (
                            <Button
                              variant="ghost"
                              className="h-auto p-0 hover:bg-transparent"
                              onClick={() => handleViewFailures(webhook)}
                            >
                              <Badge variant="destructive" className="cursor-pointer hover:bg-red-600">
                                {webhook.failureCount}
                              </Badge>
                            </Button>
                          ) : (
                            <span className="text-muted-foreground">0</span>
                          )}
                        </TableCell>
                        <TableCell className="max-w-xs truncate font-mono text-sm">
                          {webhook.url}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {webhook.lastTriggeredAt ? formatDateTime(webhook.lastTriggeredAt) : 'Never'}
                        </TableCell>
                        <TableCell>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" className="h-8 w-8 p-0">
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <DropdownMenuItem onClick={() => handleEdit(webhook)}>
                                <Edit className="h-4 w-4 mr-2" />
                                Edit
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => handleManageEvents(webhook)}>
                                <Zap className="h-4 w-4 mr-2" />
                                Manage Events
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => handleViewDeliveries(webhook)}>
                                <Eye className="h-4 w-4 mr-2" />
                                View Deliveries
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => handleTest(webhook)}>
                                <Play className="h-4 w-4 mr-2" />
                                Test Webhook
                              </DropdownMenuItem>
                              <DropdownMenuSeparator />
                              <DropdownMenuItem 
                                onClick={() => handleDelete(webhook)}
                                className="text-destructive"
                              >
                                <Trash2 className="h-4 w-4 mr-2" />
                                Delete
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>

          {/* Mobile Card View */}
          <div className="lg:hidden space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">Webhooks ({webhooks.length})</h2>
            </div>
            {webhooks.map((webhook) => (
              <Card key={webhook.id}>
                <CardContent className="p-4">
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex-1 min-w-0">
                      <h3 className="font-medium truncate">{webhook.name}</h3>
                      <p className="text-sm text-muted-foreground font-mono truncate mt-1">
                        {webhook.url}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <Switch
                        checked={webhook.isActive}
                        onCheckedChange={() => handleToggleActive(webhook)}
                        aria-label={`Toggle ${webhook.name}`}
                      />
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" className="h-8 w-8 p-0">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => handleEdit(webhook)}>
                          <Edit className="h-4 w-4 mr-2" />
                          Edit
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleManageEvents(webhook)}>
                          <Zap className="h-4 w-4 mr-2" />
                          Events
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleViewDeliveries(webhook)}>
                          <Eye className="h-4 w-4 mr-2" />
                          Deliveries
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleTest(webhook)}>
                          <Play className="h-4 w-4 mr-2" />
                          Test
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem 
                          onClick={() => handleDelete(webhook)}
                          className="text-destructive"
                        >
                          <Trash2 className="h-4 w-4 mr-2" />
                          Delete
                        </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  </div>
                  
                  <div className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <span className="text-muted-foreground">Status:</span>
                      <div className="mt-1">
                        {webhook.failureCount > 0 ? (
                          <Badge variant="destructive">Issues</Badge>
                        ) : (
                          <Badge variant="default">Healthy</Badge>
                        )}
                      </div>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Events:</span>
                      <div className="mt-1">{getEventsBadge(webhook)}</div>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Last Triggered:</span>
                      <div className="mt-1 text-xs">
                        {webhook.lastTriggeredAt ? formatDateTime(webhook.lastTriggeredAt) : 'Never'}
                      </div>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Failures:</span>
                      <div className="mt-1">
                        {webhook.failureCount > 0 ? (
                          <Button
                            variant="ghost"
                            className="h-auto p-0 hover:bg-transparent"
                            onClick={() => handleViewFailures(webhook)}
                          >
                            <Badge variant="destructive" className="cursor-pointer hover:bg-red-600">
                              {webhook.failureCount}
                            </Badge>
                          </Button>
                        ) : (
                          <span className="text-muted-foreground">0</span>
                        )}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </>
      )}

      <WebhookDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        webhook={selectedWebhook}
        onSaved={handleWebhookSaved}
      />

      <WebhookDeliveryDialog
        open={deliveryDialogOpen}
        onClose={() => setDeliveryDialogOpen(false)}
        webhookId={selectedWebhookId}
      />

      <WebhookDeliveryDialog
        open={!!selectedWebhookForFailures}
        onClose={() => setSelectedWebhookForFailures(null)}
        webhookId={selectedWebhookForFailures?.id || null}
        webhookName={selectedWebhookForFailures?.name}
        showFailuresOnly={true}
      />

      {selectedWebhookForEvents && (
        <WebhookEventsManagementDialog
          open={eventsManagementDialogOpen}
          onClose={handleEventsManagementClosed}
          webhookId={selectedWebhookForEvents.id}
          webhookName={selectedWebhookForEvents.name}
        />
      )}
    </div>
  )
}