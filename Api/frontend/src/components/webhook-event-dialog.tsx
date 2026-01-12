"use client"

import React, { useState, useEffect, useRef, useCallback } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { useForm } from 'react-hook-form'
import { useToast } from '@/hooks/use-toast'
import { apiClient, ENDPOINTS, handleApiErrorWithToast, PaginatedResponse } from '@/lib/api'
import type { 
  WebhookEventConfig, 
  CreateWebhookEventConfigRequest, 
  UpdateWebhookEventConfigRequest,
  EventTypeInfo,
  ClientListItem,
  CampaignListItem,
  UserListItem
} from '@/types'

interface WebhookEventDialogProps {
  open: boolean
  onClose: () => void
  webhookId: string
  webhookName: string
  eventConfig?: WebhookEventConfig | null
  onSaved: () => void
}

interface EventFormData {
  name: string
  description: string
  eventType: string
  thresholdPercent: number
  monitoringPeriodDays: number
  minimumEmailsSent: number
  daysSinceLastReply: number
  targetScopeType: 'clients' | 'campaigns' | 'users'
  selectedIds: string[]
}

export function WebhookEventDialog({ 
  open, 
  onClose, 
  webhookId, 
  webhookName,
  eventConfig, 
  onSaved 
}: WebhookEventDialogProps) {
  const [loading, setLoading] = useState(false)
  const [initialLoading, setInitialLoading] = useState(false)
  const [eventTypes, setEventTypes] = useState<EventTypeInfo[]>([])
  const [clients, setClients] = useState<ClientListItem[]>([])
  const [campaigns, setCampaigns] = useState<CampaignListItem[]>([])
  const [users, setUsers] = useState<UserListItem[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [filteredOptions, setFilteredOptions] = useState<(ClientListItem | CampaignListItem | UserListItem)[]>([])
  
  // Pagination state for all target scope types
  const [campaignsPagination, setCampaignsPagination] = useState({
    currentPage: 1,
    totalPages: 1,
    totalCount: 0,
    hasNext: false,
    loading: false
  })
  
  const [clientsPagination, setClientsPagination] = useState({
    currentPage: 1,
    totalPages: 1,
    totalCount: 0,
    hasNext: false,
    loading: false
  })
  
  const [usersPagination, setUsersPagination] = useState({
    currentPage: 1,
    totalPages: 1,
    totalCount: 0,
    hasNext: false,
    loading: false
  })
  
  // Ref for scroll container
  const scrollContainerRef = useRef<HTMLDivElement>(null)
  
  const { toast } = useToast()

  const form = useForm<EventFormData>({
    defaultValues: {
      name: '',
      description: '',
      eventType: 'reply_rate_drop',
      thresholdPercent: 5.0,
      monitoringPeriodDays: 7,
      minimumEmailsSent: 100,
      daysSinceLastReply: 7,
      targetScopeType: 'clients',
      selectedIds: [],
    },
  })

  const watchedTargetScopeType = form.watch('targetScopeType')
  const watchedEventType = form.watch('eventType')

  // Clear search term when target scope type changes and load first page
  useEffect(() => {
    setSearchTerm('')
    
    if (watchedTargetScopeType === 'campaigns') {
      setCampaignsPagination({
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        hasNext: false,
        loading: false
      })
      fetchCampaigns('', 1, false)
    } else if (watchedTargetScopeType === 'clients') {
      setClientsPagination({
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        hasNext: false,
        loading: false
      })
      fetchClients('', 1, false)
    } else if (watchedTargetScopeType === 'users') {
      setUsersPagination({
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        hasNext: false,
        loading: false
      })
      fetchUsers('', 1, false)
    }
  }, [watchedTargetScopeType])
  
  // Debounced search effect for all target scope types
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (watchedTargetScopeType === 'campaigns') {
        setCampaignsPagination(prev => ({
          ...prev,
          currentPage: 1,
          hasNext: false
        }))
        fetchCampaigns(searchTerm, 1, false)
      } else if (watchedTargetScopeType === 'clients') {
        setClientsPagination(prev => ({
          ...prev,
          currentPage: 1,
          hasNext: false
        }))
        fetchClients(searchTerm, 1, false)
      } else if (watchedTargetScopeType === 'users') {
        setUsersPagination(prev => ({
          ...prev,
          currentPage: 1,
          hasNext: false
        }))
        fetchUsers(searchTerm, 1, false)
      }
    }, 300) // 300ms debounce
    
    return () => clearTimeout(timeoutId)
  }, [searchTerm, watchedTargetScopeType])

  useEffect(() => {
    const initializeDialog = async () => {
      if (!open) return
      
      setInitialLoading(true)
      
      try {
        // Always fetch event types
        await fetchEventTypes()
        
        if (eventConfig) {
          // Edit mode - populate form with event config data
          form.reset({
            name: eventConfig.name,
            description: eventConfig.description,
            eventType: eventConfig.eventType,
            thresholdPercent: eventConfig.configParameters.thresholdPercent || 5.0,
            monitoringPeriodDays: eventConfig.configParameters.monitoringPeriodDays || 7,
            minimumEmailsSent: eventConfig.configParameters.minimumEmailsSent || 100,
            daysSinceLastReply: eventConfig.configParameters.daysSinceLastReply || 7,
            targetScopeType: eventConfig.targetScope.type,
            selectedIds: eventConfig.targetScope.ids,
          })
          
          // Fetch only the target type that's selected in edit mode
          if (eventConfig.targetScope.type === 'clients') {
            await fetchClients()
          } else if (eventConfig.targetScope.type === 'campaigns') {
            await fetchCampaigns()
          } else if (eventConfig.targetScope.type === 'users') {
            await fetchUsers()
          }
        } else {
          // Create mode - reset form
          form.reset({
            name: '',
            description: '',
            eventType: 'reply_rate_drop',
            thresholdPercent: 5.0,
            monitoringPeriodDays: 7,
            minimumEmailsSent: 100,
            daysSinceLastReply: 7,
            targetScopeType: 'clients',
            selectedIds: [],
          })
          
          // Fetch only clients (the default target type in create mode)
          await fetchClients()
        }
      } finally {
        setInitialLoading(false)
      }
    }

    initializeDialog()
  }, [open, eventConfig, form])

  const fetchEventTypes = async () => {
    try {
      const response = await apiClient.get<EventTypeInfo[]>(`${ENDPOINTS.webhookEvents}/event-types`)
      setEventTypes(response)
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch event types', toast)
    }
  }

  const fetchClients = async (searchQuery = '', page = 1, append = false) => {
    // Only use pagination for clients when they are selected
    if (watchedTargetScopeType !== 'clients') {
      // For non-client scopes, use the list endpoint with high limit (gets all)
      try {
        const response = await apiClient.get<PaginatedResponse<ClientListItem>>(ENDPOINTS.clientList)
        setClients(response.data)
      } catch (error) {
        handleApiErrorWithToast(error, 'fetch clients', toast)
        setClients([])
        setClientsPagination({
          currentPage: 1,
          totalPages: 1,
          totalCount: 0,
          hasNext: false,
          loading: false
        })
      }
      return
    }
    
    // For clients, always use proper server-side pagination
    try {
      setClientsPagination(prev => ({ ...prev, loading: true }))
      
      const limit = 100
      const offset = (page - 1) * limit
      const trimmedQuery = searchQuery.trim()
      
      // Build query parameters
      const searchParams = new URLSearchParams({
        limit: limit.toString(),
        offset: offset.toString()
      })
      
      // Add search query if provided and meets minimum length
      if (trimmedQuery.length >= 2) {
        searchParams.append('search', trimmedQuery)
      }
      
      // Use the list endpoint with pagination
      const response = await apiClient.get<PaginatedResponse<ClientListItem>>(
        `${ENDPOINTS.clientList}?${searchParams}`
      )
      
      if (append && page > 1) {
        setClients(prev => [...prev, ...response.data])
      } else {
        setClients(response.data)
      }
      
      setClientsPagination({
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        totalCount: response.totalCount,
        hasNext: response.hasNext,
        loading: false
      })
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch clients', toast)
      setClients([])
      setClientsPagination({
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        hasNext: false,
        loading: false
      })
    }
  }

  const fetchCampaigns = async (searchQuery = '', page = 1, append = false) => {
    // Only use pagination for campaigns when they are selected
    if (watchedTargetScopeType !== 'campaigns') {
      // For non-campaign scopes, use the simple list endpoint with high limit (gets all)
      try {
        const response = await apiClient.get<PaginatedResponse<CampaignListItem>>(ENDPOINTS.campaignList)
        setCampaigns(response.data)
      } catch (error) {
        handleApiErrorWithToast(error, 'fetch campaigns', toast)
        setCampaigns([])
        setCampaignsPagination({
          currentPage: 1,
          totalPages: 1,
          totalCount: 0,
          hasNext: false,
          loading: false
        })
      }
      return
    }
    
    // For campaigns, always use proper server-side pagination
    try {
      setCampaignsPagination(prev => ({ ...prev, loading: true }))
      
      const limit = 100
      const offset = (page - 1) * limit
      const trimmedQuery = searchQuery.trim()
      
      // Build query parameters
      const searchParams = new URLSearchParams({
        limit: limit.toString(),
        offset: offset.toString()
      })
      
      // Add search query if provided and meets minimum length
      if (trimmedQuery.length >= 2) {
        searchParams.append('search', trimmedQuery)
      } else if (trimmedQuery.length === 1) {
        // For single character search, don't send search param to get all results
        // User will see all results filtered client-side by the existing filter logic
      }
      
      // Use the list endpoint with pagination for all campaign requests
      const response = await apiClient.get<PaginatedResponse<CampaignListItem>>(
        `${ENDPOINTS.campaignList}?${searchParams}`
      )
      
      if (append && page > 1) {
        setCampaigns(prev => [...prev, ...response.data])
      } else {
        setCampaigns(response.data)
      }
      
      setCampaignsPagination({
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        totalCount: response.totalCount,
        hasNext: response.hasNext,
        loading: false
      })
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch campaigns', toast)
      setCampaigns([])
      setCampaignsPagination({
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        hasNext: false,
        loading: false
      })
    }
  }

  const fetchUsers = async (searchQuery = '', page = 1, append = false) => {
    // Only use pagination for users when they are selected
    if (watchedTargetScopeType !== 'users') {
      // For non-user scopes, use the list endpoint with high limit (gets all)
      try {
        const response = await apiClient.get<PaginatedResponse<UserListItem>>(ENDPOINTS.userList)
        setUsers(response.data)
      } catch (error) {
        handleApiErrorWithToast(error, 'fetch users', toast)
        setUsers([])
        setUsersPagination({
          currentPage: 1,
          totalPages: 1,
          totalCount: 0,
          hasNext: false,
          loading: false
        })
      }
      return
    }
    
    // For users, always use proper server-side pagination
    try {
      setUsersPagination(prev => ({ ...prev, loading: true }))
      
      const limit = 100
      const offset = (page - 1) * limit
      const trimmedQuery = searchQuery.trim()
      
      // Build query parameters
      const searchParams = new URLSearchParams({
        limit: limit.toString(),
        offset: offset.toString()
      })
      
      // Add search query if provided and meets minimum length
      if (trimmedQuery.length >= 2) {
        searchParams.append('search', trimmedQuery)
      }
      
      // Use the list endpoint with pagination
      const response = await apiClient.get<PaginatedResponse<UserListItem>>(
        `${ENDPOINTS.userList}?${searchParams}`
      )
      
      if (append && page > 1) {
        setUsers(prev => [...prev, ...response.data])
      } else {
        setUsers(response.data)
      }
      
      setUsersPagination({
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        totalCount: response.totalCount,
        hasNext: response.hasNext,
        loading: false
      })
    } catch (error) {
      handleApiErrorWithToast(error, 'fetch users', toast)
      setUsers([])
      setUsersPagination({
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        hasNext: false,
        loading: false
      })
    }
  }

  const handleSubmit = async (data: EventFormData) => {
    try {
      setLoading(true)

      // Configure parameters based on event type
      let configParameters = {}
      if (requiresDaysParams()) {
        configParameters = {
          daysSinceLastReply: data.daysSinceLastReply
        }
      } else if (requiresTriggerConditions()) {
        configParameters = {
          thresholdPercent: data.thresholdPercent,
          monitoringPeriodDays: data.monitoringPeriodDays,
          minimumEmailsSent: data.minimumEmailsSent
        }
      }

      const targetScope = requiresTargetScope() ? {
        type: data.targetScopeType,
        ids: data.selectedIds
      } : {
        type: 'clients' as const,
        ids: []
      }

      if (eventConfig) {
        // Update existing event config
        const updateData: UpdateWebhookEventConfigRequest = {
          name: data.name,
          description: data.description,
          configParameters,
          targetScope,
        }

        await apiClient.put(`${ENDPOINTS.webhookEvents}/${eventConfig.id}`, updateData)
        
        toast({
          title: 'Success',
          description: 'Webhook event updated successfully',
        })
      } else {
        // Create new event config
        const createData: CreateWebhookEventConfigRequest = {
          webhookId,
          eventType: data.eventType,
          name: data.name,
          description: data.description,
          configParameters,
          targetScope,
        }

        await apiClient.post(ENDPOINTS.webhookEvents, createData)
        
        toast({
          title: 'Success',
          description: 'Webhook event created successfully',
        })
      }

      onSaved()
    } catch (error) {
      handleApiErrorWithToast(error, eventConfig ? 'update webhook event' : 'create webhook event', toast)
    } finally {
      setLoading(false)
    }
  }

  const handleTargetSelection = (id: string, checked: boolean) => {
    const current = form.getValues('selectedIds')
    if (checked) {
      form.setValue('selectedIds', [...current, id])
    } else {
      form.setValue('selectedIds', current.filter(item => item !== id))
    }
  }

  const handleSelectAll = () => {
    const filteredOptions = getFilteredTargetOptions()
    const filteredIds = filteredOptions.map(item => item.id)
    const currentIds = form.getValues('selectedIds')

    // Add filtered IDs to current selection (avoid duplicates)
    const uniqueSet = new Set([...currentIds, ...filteredIds]);
    const newIds = Array.from(uniqueSet);
    form.setValue('selectedIds', newIds)
  }

  const handleUnselectAll = () => {
    if (searchTerm.trim()) {
      // If searching, only unselect filtered items
      const filteredOptions = getFilteredTargetOptions()
      const filteredIds = new Set(filteredOptions.map(item => item.id))
      const currentIds = form.getValues('selectedIds')
      const remainingIds = currentIds.filter(id => !filteredIds.has(id))
      form.setValue('selectedIds', remainingIds)
    } else {
      // If not searching, clear all
      form.setValue('selectedIds', [])
    }
  }

  const handleLoadMore = useCallback(async () => {
    if (watchedTargetScopeType === 'campaigns' && campaignsPagination.hasNext && !campaignsPagination.loading) {
      const nextPage = campaignsPagination.currentPage + 1
      await fetchCampaigns(searchTerm, nextPage, true)
    } else if (watchedTargetScopeType === 'clients' && clientsPagination.hasNext && !clientsPagination.loading) {
      const nextPage = clientsPagination.currentPage + 1
      await fetchClients(searchTerm, nextPage, true)
    } else if (watchedTargetScopeType === 'users' && usersPagination.hasNext && !usersPagination.loading) {
      const nextPage = usersPagination.currentPage + 1
      await fetchUsers(searchTerm, nextPage, true)
    }
  }, [
    watchedTargetScopeType, searchTerm,
    campaignsPagination.hasNext, campaignsPagination.loading, campaignsPagination.currentPage,
    clientsPagination.hasNext, clientsPagination.loading, clientsPagination.currentPage,
    usersPagination.hasNext, usersPagination.loading, usersPagination.currentPage
  ])

  const handleScroll = useCallback(() => {
    const container = scrollContainerRef.current
    if (!container) return
    
    const { scrollTop, scrollHeight, clientHeight } = container
    // Trigger load more when scrolled to within 50px of bottom
    const threshold = 50
    const isNearBottom = scrollTop + clientHeight >= scrollHeight - threshold
    
    if (isNearBottom) {
      const canLoadMore = 
        (watchedTargetScopeType === 'campaigns' && campaignsPagination.hasNext && !campaignsPagination.loading) ||
        (watchedTargetScopeType === 'clients' && clientsPagination.hasNext && !clientsPagination.loading) ||
        (watchedTargetScopeType === 'users' && usersPagination.hasNext && !usersPagination.loading)
      
      if (canLoadMore) {
        handleLoadMore()
      }
    }
  }, [
    watchedTargetScopeType, handleLoadMore,
    campaignsPagination.hasNext, campaignsPagination.loading,
    clientsPagination.hasNext, clientsPagination.loading,
    usersPagination.hasNext, usersPagination.loading
  ])

  // Scroll event listener for infinite scroll - must come after handleScroll is defined
  useEffect(() => {
    const container = scrollContainerRef.current
    if (!container) return
    
    container.addEventListener('scroll', handleScroll)
    return () => container.removeEventListener('scroll', handleScroll)
  }, [handleScroll])

  const getAllTargetOptions = () => {
    if (watchedTargetScopeType === 'clients') {
      return clients || []
    } else if (watchedTargetScopeType === 'campaigns') {
      return campaigns || []
    } else {
      return users || []
    }
  }

  const getFilteredTargetOptions = () => {
    const allOptions = getAllTargetOptions()
    
    if (!searchTerm.trim()) {
      return allOptions
    }
    
    return allOptions.filter(item => 
      (item.name || (watchedTargetScopeType === 'campaigns' ? 'Unnamed Campaign' : watchedTargetScopeType === 'users' ? 'Unnamed User' : 'Unnamed Client')).toLowerCase().includes(searchTerm.toLowerCase())
    )
  }

  // Keep this for backward compatibility
  const getTargetOptions = () => getFilteredTargetOptions()

  const getThresholdLabel = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'bounce_rate_high' 
      ? 'Bounce Rate Threshold (%)' 
      : 'Reply Rate Drop Threshold (%)'
  }

  const getThresholdDescription = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'bounce_rate_high'
      ? 'Maximum bounce rate percentage allowed before triggering alert'
      : 'Minimum drop in reply rate percentage to trigger alert'
  }

  const requiresTriggerConditions = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'reply_rate_drop' || currentEventType === 'bounce_rate_high'
  }

  const requiresTargetScope = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'reply_rate_drop' || currentEventType === 'bounce_rate_high' || currentEventType === 'no_positive_reply_for_x_days' || currentEventType === 'no_reply_for_x_days'
  }

  const requiresNoPositiveReplyParams = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'no_positive_reply_for_x_days'
  }

  const requiresNoReplyParams = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'no_reply_for_x_days'
  }

  const requiresDaysParams = () => {
    const currentEventType = eventConfig?.eventType || watchedEventType
    return currentEventType === 'no_positive_reply_for_x_days' || currentEventType === 'no_reply_for_x_days'
  }


  const selectedIds = form.watch('selectedIds')

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="w-[95vw] max-w-4xl max-h-[90vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>
            {eventConfig ? 'Edit Webhook Event' : 'Create Webhook Event'}
          </DialogTitle>
          <DialogDescription>
            {eventConfig 
              ? 'Update the webhook event configuration' 
              : `Configure a new event for webhook "${webhookName}"`
            }
          </DialogDescription>
        </DialogHeader>

        {initialLoading ? (
          <div className="space-y-6 flex-1 overflow-y-auto">
            {/* Event Configuration Skeleton */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Event Configuration</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-10 w-full" />
                  <Skeleton className="h-3 w-48" />
                </div>
                <div className="space-y-2">
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-20 w-full" />
                  <Skeleton className="h-3 w-56" />
                </div>
                {!eventConfig && (
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-20" />
                    <div className="space-y-3">
                      <div className="flex items-start space-x-2">
                        <Skeleton className="h-4 w-4 mt-1 rounded-full" />
                        <div className="flex-1 space-y-1">
                          <Skeleton className="h-4 w-32" />
                          <Skeleton className="h-3 w-64" />
                        </div>
                      </div>
                      <div className="flex items-start space-x-2">
                        <Skeleton className="h-4 w-4 mt-1 rounded-full" />
                        <div className="flex-1 space-y-1">
                          <Skeleton className="h-4 w-36" />
                          <Skeleton className="h-3 w-72" />
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Trigger Conditions Skeleton */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Trigger Conditions</CardTitle>
                <CardDescription>
                  Configure when this event should be triggered
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-40" />
                    <Skeleton className="h-10 w-full" />
                    <Skeleton className="h-3 w-48" />
                  </div>
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-36" />
                    <Skeleton className="h-10 w-full" />
                    <Skeleton className="h-3 w-44" />
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Target Scope Skeleton */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Target Scope</CardTitle>
                <CardDescription>
                  Select which clients, campaigns, or users to monitor
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Skeleton className="h-4 w-16" />
                  <div className="flex flex-row gap-6">
                    <div className="flex items-center space-x-2">
                      <Skeleton className="h-4 w-4 rounded-full" />
                      <Skeleton className="h-4 w-12" />
                    </div>
                    <div className="flex items-center space-x-2">
                      <Skeleton className="h-4 w-4 rounded-full" />
                      <Skeleton className="h-4 w-20" />
                    </div>
                    <div className="flex items-center space-x-2">
                      <Skeleton className="h-4 w-4 rounded-full" />
                      <Skeleton className="h-4 w-12" />
                    </div>
                  </div>
                </div>
                <div className="space-y-3">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-10 w-full" />
                  <div className="border rounded p-3 space-y-2">
                    {[...Array(3)].map((_, i) => (
                      <div key={i} className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Footer Skeleton */}
            <div className="flex justify-end space-x-2 pt-4">
              <Skeleton className="h-10 w-16" />
              <Skeleton className="h-10 w-24" />
            </div>
          </div>
        ) : (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6 flex-1 overflow-y-auto">
            <div className="grid grid-cols-1 gap-6">
              {/* Basic Information */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Event Configuration</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="name"
                    rules={{ required: 'Name is required' }}
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Event Name</FormLabel>
                        <FormControl>
                          <Input placeholder="Enter event name" {...field} />
                        </FormControl>
                        <FormDescription>
                          A descriptive name for this event configuration
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="description"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Description</FormLabel>
                        <FormControl>
                          <Textarea 
                            placeholder="Describe when this event should trigger..."
                            rows={3}
                            {...field} 
                          />
                        </FormControl>
                        <FormDescription>
                          Optional description of when this event should trigger
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  {!eventConfig && (
                    <FormField
                      control={form.control}
                      name="eventType"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Event Type</FormLabel>
                          <FormControl>
                            <RadioGroup
                              onValueChange={field.onChange}
                              defaultValue={field.value}
                              className="grid grid-cols-1 gap-4"
                            >
                              {eventTypes.map((eventType) => (
                                <div key={eventType.type} className="flex items-start space-x-2">
                                  <RadioGroupItem value={eventType.type} id={eventType.type} className="mt-1" />
                                  <div className="flex-1">
                                    <label 
                                      htmlFor={eventType.type}
                                      className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                                    >
                                      {eventType.name}
                                    </label>
                                    <p className="text-sm text-muted-foreground mt-1">
                                      {eventType.description}
                                    </p>
                                  </div>
                                </div>
                              ))}
                            </RadioGroup>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  )}
                </CardContent>
              </Card>

              {/* Event Parameters - Show for rate-based events */}
              {requiresTriggerConditions() && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">Trigger Conditions</CardTitle>
                    <CardDescription>
                      Configure when this event should be triggered
                    </CardDescription>
                  </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="thresholdPercent"
                      rules={requiresTriggerConditions() ? { 
                        required: 'Threshold is required',
                        min: { value: 0.001, message: 'Must be greater than 0%' },
                        max: { value: 100, message: 'Must be 100% or less' }
                      } : {}}
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{getThresholdLabel()}</FormLabel>
                          <FormControl>
                            <Input 
                              type="number" 
                              step="0.001"
                              min="0.001"
                              max="100"
                              placeholder="5.0"
                              {...field}
                              onChange={(e) => field.onChange(parseFloat(e.target.value))}
                            />
                          </FormControl>
                          <FormDescription>
                            {getThresholdDescription()}
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="monitoringPeriodDays"
                      rules={requiresTriggerConditions() ? { 
                        required: 'Monitoring period is required',
                        min: { value: 1, message: 'Must be at least 1 day' },
                        max: { value: 30, message: 'Must be 30 days or less' }
                      } : {}}
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Monitoring Period (days)</FormLabel>
                          <FormControl>
                            <Input 
                              type="number" 
                              min="1"
                              max="30"
                              placeholder="7"
                              {...field}
                              onChange={(e) => field.onChange(parseInt(e.target.value))}
                            />
                          </FormControl>
                          <FormDescription>
                            Number of days to compare performance
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                  
                  <FormField
                    control={form.control}
                    name="minimumEmailsSent"
                    rules={requiresTriggerConditions() ? { 
                      required: 'Minimum emails sent is required',
                      min: { value: 1, message: 'Must be at least 1 email' },
                      max: { value: 10000, message: 'Must be 10000 or less' }
                    } : {}}
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Minimum Emails Sent</FormLabel>
                        <FormControl>
                          <Input 
                            type="number" 
                            min="1"
                            max="10000"
                            placeholder="100"
                            {...field}
                            onChange={(e) => field.onChange(parseInt(e.target.value))}
                          />
                        </FormControl>
                        <FormDescription>
                          Only trigger alerts for campaigns that have sent at least this many emails
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
                </Card>
              )}

              {/* No Positive Reply Parameters */}
              {requiresNoPositiveReplyParams() && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">No Positive Reply Configuration</CardTitle>
                    <CardDescription>
                      Configure how many days without a positive reply should trigger the event
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="daysSinceLastReply"
                      rules={{ 
                        required: 'Days since last reply is required',
                        min: { value: 1, message: 'Must be at least 1 day' },
                        max: { value: 365, message: 'Must be 365 days or less' }
                      }}
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Days Since Last Positive Reply</FormLabel>
                          <FormControl>
                            <Input 
                              type="number" 
                              min="1"
                              max="365"
                              placeholder="7"
                              {...field}
                              onChange={(e) => field.onChange(parseInt(e.target.value))}
                            />
                          </FormControl>
                          <FormDescription>
                            Number of days since the last positive reply before triggering the alert
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>
              )}

              {/* No Reply Parameters */}
              {requiresNoReplyParams() && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">No Reply Configuration</CardTitle>
                    <CardDescription>
                      Configure how many days without any reply should trigger the event
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="daysSinceLastReply"
                      rules={{ 
                        required: 'Days since last reply is required',
                        min: { value: 1, message: 'Must be at least 1 day' },
                        max: { value: 365, message: 'Must be 365 days or less' }
                      }}
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Days Since Last Reply</FormLabel>
                          <FormControl>
                            <Input 
                              type="number" 
                              min="1"
                              max="365"
                              placeholder="7"
                              {...field}
                              onChange={(e) => field.onChange(parseInt(e.target.value))}
                            />
                          </FormControl>
                          <FormDescription>
                            Number of days since the last reply (positive or negative) before triggering the alert
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>
              )}

              {/* Target Scope - Show for events that need target selection */}
              {requiresTargetScope() && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Target Scope</CardTitle>
                  <CardDescription>
                    Select which clients, campaigns, or users to monitor
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="targetScopeType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Monitor</FormLabel>
                        <FormControl>
                          <RadioGroup
                            onValueChange={(value: 'clients' | 'campaigns' | 'users') => {
                              field.onChange(value)
                              form.setValue('selectedIds', []) // Clear selections when changing type
                            }}
                            defaultValue={field.value}
                            className="flex flex-row gap-6"
                          >
                            <div className="flex items-center space-x-2">
                              <RadioGroupItem value="clients" id="clients" />
                              <label htmlFor="clients" className="text-sm font-medium">
                                Clients
                              </label>
                            </div>
                            <div className="flex items-center space-x-2">
                              <RadioGroupItem value="campaigns" id="campaigns" />
                              <label htmlFor="campaigns" className="text-sm font-medium">
                                Specific Campaigns
                              </label>
                            </div>
                            <div className="flex items-center space-x-2">
                              <RadioGroupItem value="users" id="users" />
                              <label htmlFor="users" className="text-sm font-medium">
                                Users
                              </label>
                            </div>
                          </RadioGroup>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <FormLabel>
                        Select {watchedTargetScopeType === 'clients' ? 'Clients' : watchedTargetScopeType === 'campaigns' ? 'Campaigns' : 'Users'}
                      </FormLabel>
                      <div className="flex items-center gap-2">
                        {selectedIds.length > 0 && (
                          <Badge variant="secondary">
                            {selectedIds.length} selected
                          </Badge>
                        )}
                        <div className="flex gap-1">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={handleSelectAll}
                            className="h-7 px-2 text-xs"
                          >
                            {searchTerm.trim() ? `Select Filtered (${getFilteredTargetOptions().length})` : 'Select All'}
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={handleUnselectAll}
                            className="h-7 px-2 text-xs"
                            disabled={selectedIds.length === 0}
                          >
                            {searchTerm.trim() ? 'Clear Filtered' : 'Clear All'}
                          </Button>
                        </div>
                      </div>
                    </div>

                    <Input
                      placeholder={`Search ${watchedTargetScopeType}...`}
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="mb-2 focus:ring-0 focus:border-input"
                    />

                    {/* Show pagination info for all scope types */}
                    <div className="text-xs text-muted-foreground mb-2">
                      {searchTerm.trim() ? (
                        `Showing ${getFilteredTargetOptions().length} results`
                      ) : (
                        <>
                          {watchedTargetScopeType === 'campaigns' && (
                            `Showing ${campaigns?.length || 0} of ${campaignsPagination.totalCount} campaigns`
                          )}
                          {watchedTargetScopeType === 'clients' && (
                            `Showing ${clients?.length || 0} of ${clientsPagination.totalCount} clients`
                          )}
                          {watchedTargetScopeType === 'users' && (
                            `Showing ${users?.length || 0} of ${usersPagination.totalCount} users`
                          )}
                        </>
                      )}
                    </div>

                    <div 
                      ref={scrollContainerRef}
                      className={`grid grid-cols-1 gap-2 border rounded p-3 ${
                        watchedTargetScopeType === 'campaigns' ? (campaignsPagination.totalCount > 6 ? 'max-h-80 overflow-y-auto' : '') :
                        watchedTargetScopeType === 'clients' ? (clientsPagination.totalCount > 6 ? 'max-h-80 overflow-y-auto' : '') :
                        watchedTargetScopeType === 'users' ? (usersPagination.totalCount > 6 ? 'max-h-80 overflow-y-auto' : '') :
                        (getTargetOptions().length > 6 ? 'max-h-80 overflow-y-auto' : '')
                      }`}
                    >
                      {getTargetOptions().map((item) => (
                        <div key={item.id} className="flex items-center space-x-2">
                          <Checkbox
                            id={item.id}
                            checked={selectedIds.includes(item.id)}
                            onCheckedChange={(checked) => 
                              handleTargetSelection(item.id, checked as boolean)
                            }
                          />
                          <label
                            htmlFor={item.id}
                            className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 flex-1 cursor-pointer"
                          >
                            {item.name || (watchedTargetScopeType === 'campaigns' ? 'Unnamed Campaign' : watchedTargetScopeType === 'users' ? 'Unnamed User' : 'Unnamed Client')}
                          </label>
                        </div>
                      ))}
                      
                      {/* Infinite scroll loading indicator */}
                      {watchedTargetScopeType === 'campaigns' && campaignsPagination.loading && (
                        <div className="flex items-center justify-center py-2 text-sm text-muted-foreground">
                          <div className="animate-pulse">Loading more campaigns...</div>
                        </div>
                      )}
                      {watchedTargetScopeType === 'clients' && clientsPagination.loading && (
                        <div className="flex items-center justify-center py-2 text-sm text-muted-foreground">
                          <div className="animate-pulse">Loading more clients...</div>
                        </div>
                      )}
                      {watchedTargetScopeType === 'users' && usersPagination.loading && (
                        <div className="flex items-center justify-center py-2 text-sm text-muted-foreground">
                          <div className="animate-pulse">Loading more users...</div>
                        </div>
                      )}
                    </div>

                    {/* Show end indicator when no more items to load */}
                    {watchedTargetScopeType === 'campaigns' && !campaignsPagination.hasNext && !campaignsPagination.loading && (campaigns?.length || 0) > 0 && (
                      <div className="mt-2 text-center text-xs text-muted-foreground">
                        {campaignsPagination.totalCount > 100 
                          ? `All ${campaignsPagination.totalCount} campaigns loaded`
                          : `${campaigns?.length || 0} campaign${(campaigns?.length || 0) !== 1 ? 's' : ''} available`
                        }
                      </div>
                    )}
                    {watchedTargetScopeType === 'clients' && !clientsPagination.hasNext && !clientsPagination.loading && (clients?.length || 0) > 0 && (
                      <div className="mt-2 text-center text-xs text-muted-foreground">
                        {clientsPagination.totalCount > 100 
                          ? `All ${clientsPagination.totalCount} clients loaded`
                          : `${clients?.length || 0} client${(clients?.length || 0) !== 1 ? 's' : ''} available`
                        }
                      </div>
                    )}
                    {watchedTargetScopeType === 'users' && !usersPagination.hasNext && !usersPagination.loading && (users?.length || 0) > 0 && (
                      <div className="mt-2 text-center text-xs text-muted-foreground">
                        {usersPagination.totalCount > 100 
                          ? `All ${usersPagination.totalCount} users loaded`
                          : `${users?.length || 0} user${(users?.length || 0) !== 1 ? 's' : ''} available`
                        }
                      </div>
                    )}

                    {selectedIds.length === 0 && (
                      <p className="text-sm text-muted-foreground">
                        Please select at least one {watchedTargetScopeType === 'users' ? 'user' : watchedTargetScopeType.slice(0, -1)} to monitor
                      </p>
                    )}
                  </div>
                </CardContent>
              </Card>
              )}
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose}>
                Cancel
              </Button>
              <Button 
                type="submit" 
                disabled={loading || (requiresTargetScope() && selectedIds.length === 0)}
              >
                {loading ? 'Saving...' : eventConfig ? 'Update Event' : 'Create Event'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
        )}
      </DialogContent>
    </Dialog>
  )
}