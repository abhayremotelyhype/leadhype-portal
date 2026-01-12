"use client"

import React, { useState, useEffect, useCallback } from 'react'
import { Plus, Edit, Trash2, Eye } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useToast } from '@/hooks/use-toast'
import { apiClient, ENDPOINTS, formatDateTime, handleApiErrorWithToast } from '@/lib/api'
import type { WebhookEventConfig } from '@/types'
import { WebhookEventDialog } from '@/components/webhook-event-dialog'

interface WebhookEventsManagementDialogProps {
  open: boolean
  onClose: () => void
  webhookId: string
  webhookName: string
}

export function WebhookEventsManagementDialog({ 
  open, 
  onClose, 
  webhookId, 
  webhookName 
}: WebhookEventsManagementDialogProps) {
  const [events, setEvents] = useState<WebhookEventConfig[]>([])
  const [loading, setLoading] = useState(false)
  const [eventDialogOpen, setEventDialogOpen] = useState(false)
  const [selectedEvent, setSelectedEvent] = useState<WebhookEventConfig | null>(null)
  const { toast } = useToast()

  const fetchEvents = useCallback(async () => {
    if (!open) return
    
    try {
      setLoading(true)
      const allEvents = await apiClient.get<WebhookEventConfig[]>(ENDPOINTS.webhookEvents)
      const webhookEvents = allEvents.filter(event => event.webhookId === webhookId)
      setEvents(webhookEvents)
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch webhook events', toast)
    } finally {
      setLoading(false)
    }
  }, [open, webhookId, toast])

  useEffect(() => {
    fetchEvents()
  }, [open, webhookId, fetchEvents])

  const handleCreateNew = () => {
    setSelectedEvent(null)
    setEventDialogOpen(true)
  }

  const handleEditEvent = (event: WebhookEventConfig) => {
    setSelectedEvent(event)
    setEventDialogOpen(true)
  }

  const handleDeleteEvent = async (event: WebhookEventConfig) => {
    if (!confirm(`Are you sure you want to delete the event "${event.name}"?`)) {
      return
    }

    try {
      await apiClient.delete(`${ENDPOINTS.webhookEvents}/${event.id}`)
      toast({
        title: 'Success',
        description: 'Webhook event deleted successfully',
      })
      fetchEvents()
    } catch (error) {
      handleApiErrorWithToast(error, 'delete webhook event', toast)
    }
  }

  const handleToggleActive = async (event: WebhookEventConfig) => {
    try {
      await apiClient.put(`${ENDPOINTS.webhookEvents}/${event.id}`, {
        isActive: !event.isActive
      })
      toast({
        title: 'Success',
        description: `Event ${event.isActive ? 'deactivated' : 'activated'} successfully`,
      })
      fetchEvents()
    } catch (error) {
      handleApiErrorWithToast(error, 'update webhook event', toast)
    }
  }

  const handleEventSaved = () => {
    setEventDialogOpen(false)
    setSelectedEvent(null)
    fetchEvents()
  }

  const getEventTypeBadge = (eventType: string) => {
    switch (eventType) {
      case 'reply_rate_drop':
        return <Badge variant="destructive">Reply Rate Drop</Badge>
      case 'bounce_rate_high':
        return <Badge variant="default">High Bounce Rate</Badge>
      case 'no_positive_reply_for_x_days':
        return <Badge variant="outline">No Positive Reply</Badge>
      case 'no_reply_for_x_days':
        return <Badge variant="secondary">No Reply</Badge>
      default:
        return <Badge variant="secondary">{eventType}</Badge>
    }
  }

  const getTargetScope = (event: WebhookEventConfig) => {
    const scope = event.targetScope
    if (!scope) return 'Unknown'
    
    const type = scope.type === 'clients' ? 'Clients' : scope.type === 'campaigns' ? 'Campaigns' : 'Users'
    const count = scope.ids ? scope.ids.length : 0
    return `${count} ${type}`
  }

  if (loading) {
    return (
      <Dialog open={open} onOpenChange={onClose}>
        <DialogContent className="max-w-4xl">
          <div className="flex items-center justify-center p-8">
            <div className="text-muted-foreground">Loading events...</div>
          </div>
        </DialogContent>
      </Dialog>
    )
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onClose}>
        <DialogContent className="w-[95vw] max-w-4xl max-h-[85vh] flex flex-col mx-2">
          <DialogHeader>
            <DialogTitle>Manage Webhook Events</DialogTitle>
            <DialogDescription>
              Configure and manage events for webhook &quot;{webhookName}&quot;
            </DialogDescription>
          </DialogHeader>

          <div className="flex-1 overflow-y-auto space-y-4 px-1">
            <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3">
              <div className="text-sm text-muted-foreground">
                {events.length} event{events.length !== 1 ? 's' : ''} configured
              </div>
              <Button onClick={handleCreateNew} className="h-10 sm:h-9">
                <Plus className="h-4 w-4 mr-2" />
                Create Event
              </Button>
            </div>

            {events.length === 0 ? (
              <Card>
                <CardContent className="flex flex-col items-center justify-center py-12">
                  <Eye className="h-12 w-12 text-muted-foreground mb-4" />
                  <h3 className="text-lg font-semibold mb-2">No events configured</h3>
                  <p className="text-muted-foreground text-center mb-4 max-w-md">
                    Create your first webhook event to monitor campaign performance and receive alerts.
                  </p>
                  <Button onClick={handleCreateNew} className="h-10 sm:h-9">
                    <Plus className="h-4 w-4 mr-2" />
                    Create Event
                  </Button>
                </CardContent>
              </Card>
            ) : (
              <>
                {/* Desktop Table View */}
                <Card className="hidden lg:block">
                  <CardHeader>
                    <CardTitle>Webhook Events ({events.length})</CardTitle>
                    <CardDescription>
                      Monitor campaign metrics and trigger webhooks when thresholds are exceeded
                    </CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="overflow-x-auto">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>Name</TableHead>
                            <TableHead>Type</TableHead>
                            <TableHead>Threshold</TableHead>
                            <TableHead>Monitoring Scope</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Last Triggered</TableHead>
                            <TableHead className="w-[140px]">Actions</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {events.map((event) => (
                            <TableRow key={event.id}>
                              <TableCell className="font-medium">
                                <div>
                                  <div>{event.name}</div>
                                  {event.description && (
                                    <div className="text-sm text-muted-foreground truncate max-w-xs">
                                      {event.description}
                                    </div>
                                  )}
                                </div>
                              </TableCell>
                              <TableCell>
                                {getEventTypeBadge(event.eventType)}
                              </TableCell>
                              <TableCell>
                                <div className="text-sm">
                                  {event.eventType === 'no_positive_reply_for_x_days' ? (
                                    <>
                                      <div>{event.configParameters.daysSinceLastReply} days</div>
                                      <div className="text-muted-foreground">no positive reply</div>
                                    </>
                                  ) : event.eventType === 'no_reply_for_x_days' ? (
                                    <>
                                      <div>{event.configParameters.daysSinceLastReply} days</div>
                                      <div className="text-muted-foreground">no reply</div>
                                    </>
                                  ) : (
                                    <>
                                      <div>{event.configParameters.thresholdPercent}%</div>
                                      <div className="text-muted-foreground">
                                        {event.configParameters.monitoringPeriodDays}d period
                                      </div>
                                    </>
                                  )}
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="text-sm">
                                  {getTargetScope(event)}
                                </div>
                              </TableCell>
                              <TableCell>
                                {event.isActive ? (
                                  <Badge variant="default">Active</Badge>
                                ) : (
                                  <Badge variant="secondary">Inactive</Badge>
                                )}
                              </TableCell>
                              <TableCell className="text-sm text-muted-foreground">
                                {event.lastTriggeredAt ? formatDateTime(event.lastTriggeredAt) : 'Never'}
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-2">
                                  <Button 
                                    variant="ghost" 
                                    size="sm"
                                    onClick={() => handleEditEvent(event)}
                                  >
                                    <Edit className="h-4 w-4" />
                                  </Button>
                                  <Switch
                                    checked={event.isActive}
                                    onCheckedChange={() => handleToggleActive(event)}
                                    aria-label={`${event.isActive ? 'Deactivate' : 'Activate'} ${event.name}`}
                                  />
                                  <Button 
                                    variant="ghost" 
                                    size="sm"
                                    onClick={() => handleDeleteEvent(event)}
                                    className="text-destructive hover:text-destructive"
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                  </CardContent>
                </Card>

                {/* Mobile/Tablet Card View */}
                <div className="lg:hidden space-y-4">
                  <div className="flex items-center justify-between">
                    <h3 className="text-lg font-semibold">Events ({events.length})</h3>
                  </div>
                  {events.map((event) => (
                    <Card key={event.id}>
                      <CardContent className="p-4">
                        <div className="flex items-start justify-between mb-3">
                          <div className="flex-1 min-w-0">
                            <h4 className="font-medium truncate">{event.name}</h4>
                            {event.description && (
                              <p className="text-sm text-muted-foreground truncate mt-1">
                                {event.description}
                              </p>
                            )}
                          </div>
                          <div className="flex items-center gap-2 flex-shrink-0 ml-2">
                            <Button 
                              variant="ghost" 
                              size="sm"
                              onClick={() => handleEditEvent(event)}
                              className="h-9 w-9 p-0"
                            >
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Switch
                              checked={event.isActive}
                              onCheckedChange={() => handleToggleActive(event)}
                              aria-label={`${event.isActive ? 'Deactivate' : 'Activate'} ${event.name}`}
                            />
                            <Button 
                              variant="ghost" 
                              size="sm"
                              onClick={() => handleDeleteEvent(event)}
                              className="text-destructive hover:text-destructive h-9 w-9 p-0"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>
                        
                        <div className="space-y-3 text-sm">
                          <div className="flex justify-between items-start">
                            <span className="text-muted-foreground">Type:</span>
                            <div>{getEventTypeBadge(event.eventType)}</div>
                          </div>
                          <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">Status:</span>
                            <div>
                              {event.isActive ? (
                                <Badge variant="default">Active</Badge>
                              ) : (
                                <Badge variant="secondary">Inactive</Badge>
                              )}
                            </div>
                          </div>
                          <div className="flex justify-between items-start">
                            <span className="text-muted-foreground">Threshold:</span>
                            <div className="text-right">
                              {event.eventType === 'no_positive_reply_for_x_days' ? (
                                <>
                                  <div>{event.configParameters.daysSinceLastReply} days</div>
                                  <div className="text-xs text-muted-foreground">no positive reply</div>
                                </>
                              ) : event.eventType === 'no_reply_for_x_days' ? (
                                <>
                                  <div>{event.configParameters.daysSinceLastReply} days</div>
                                  <div className="text-xs text-muted-foreground">no reply</div>
                                </>
                              ) : (
                                <>
                                  <div>{event.configParameters.thresholdPercent}%</div>
                                  <div className="text-xs text-muted-foreground">
                                    {event.configParameters.monitoringPeriodDays}d period
                                  </div>
                                </>
                              )}
                            </div>
                          </div>
                          <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">Scope:</span>
                            <div className="text-xs text-right">
                              {getTargetScope(event)}
                            </div>
                          </div>
                          <div className="border-t pt-3">
                            <div className="flex justify-between items-center">
                              <span className="text-muted-foreground">Last Triggered:</span>
                              <div className="text-xs text-right">
                                {event.lastTriggeredAt ? formatDateTime(event.lastTriggeredAt) : 'Never'}
                              </div>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <WebhookEventDialog
        open={eventDialogOpen}
        onClose={() => setEventDialogOpen(false)}
        webhookId={webhookId}
        webhookName={webhookName}
        eventConfig={selectedEvent}
        onSaved={handleEventSaved}
      />
    </>
  )
}