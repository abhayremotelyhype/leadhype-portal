"use client"

import React, { useState, useEffect } from 'react'
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
import { Checkbox } from '@/components/ui/checkbox'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Plus, X } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useToast } from '@/hooks/use-toast'
import { apiClient, ENDPOINTS, handleApiErrorWithToast } from '@/lib/api'
import type { Webhook, CreateWebhookRequest, UpdateWebhookRequest } from '@/types'

interface WebhookDialogProps {
  open: boolean
  onClose: () => void
  webhook?: Webhook | null
  onSaved: () => void
}

interface WebhookFormData {
  name: string
  url: string
  retryCount: number
  timeoutSeconds: number
  customHeaders: { key: string; value: string }[]
}

export function WebhookDialog({ open, onClose, webhook, onSaved }: WebhookDialogProps) {
  const [loading, setLoading] = useState(false)
  const { toast } = useToast()

  const form = useForm<WebhookFormData>({
    defaultValues: {
      name: '',
      url: '',
      retryCount: 3,
      timeoutSeconds: 30,
      customHeaders: [],
    },
  })


  useEffect(() => {
    if (open) {
      if (webhook) {
        // Edit mode - populate form with webhook data
        const headers = Object.entries(webhook.headers || {}).map(([key, value]) => ({
          key,
          value,
        }))
        
        form.reset({
          name: webhook.name,
          url: webhook.url,
          retryCount: webhook.retryCount,
          timeoutSeconds: webhook.timeoutSeconds,
          customHeaders: headers,
        })
      } else {
        // Create mode - reset form
        form.reset({
          name: '',
          url: '',
          retryCount: 3,
          timeoutSeconds: 30,
          customHeaders: [],
        })
      }
    }
  }, [open, webhook, form])

  const handleSubmit = async (data: WebhookFormData) => {
    try {
      setLoading(true)

      const headers: Record<string, string> = {}
      data.customHeaders.forEach(({ key, value }) => {
        if (key && value) {
          headers[key] = value
        }
      })

      if (webhook) {
        // Update existing webhook
        const updateData: UpdateWebhookRequest = {
          name: data.name,
          url: data.url,
          headers,
          retryCount: data.retryCount,
          timeoutSeconds: data.timeoutSeconds,
        }

        await apiClient.put(`${ENDPOINTS.webhooks}/${webhook.id}`, updateData)
        
        toast({
          title: 'Success',
          description: 'Webhook updated successfully',
        })
      } else {
        // Create new webhook
        const createData: CreateWebhookRequest = {
          name: data.name,
          url: data.url,
          headers,
          retryCount: data.retryCount,
          timeoutSeconds: data.timeoutSeconds,
        }

        await apiClient.post(ENDPOINTS.webhooks, createData)
        
        toast({
          title: 'Success',
          description: 'Webhook created successfully',
        })
      }

      onSaved()
    } catch (error) {
      handleApiErrorWithToast(error, webhook ? 'update webhook' : 'create webhook', toast)
    } finally {
      setLoading(false)
    }
  }


  const addCustomHeader = () => {
    const current = form.getValues('customHeaders')
    form.setValue('customHeaders', [...current, { key: '', value: '' }])
  }

  const removeCustomHeader = (index: number) => {
    const current = form.getValues('customHeaders')
    form.setValue('customHeaders', current.filter((_, i) => i !== index))
  }

  const updateCustomHeader = (index: number, field: 'key' | 'value', value: string) => {
    const current = form.getValues('customHeaders')
    current[index][field] = value
    form.setValue('customHeaders', [...current])
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="w-[95vw] max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {webhook ? 'Edit Webhook' : 'Create Webhook'}
          </DialogTitle>
          <DialogDescription>
            {webhook ? 'Update webhook configuration' : 'Configure a new webhook endpoint for real-time notifications'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
            <div className="grid grid-cols-1 gap-6">
              {/* Basic Information */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Basic Information</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="name"
                    rules={{ required: 'Name is required' }}
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Name</FormLabel>
                        <FormControl>
                          <Input placeholder="My Webhook" {...field} />
                        </FormControl>
                        <FormDescription>
                          A friendly name to identify this webhook
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="url"
                    rules={{ 
                      required: 'URL is required',
                      pattern: {
                        value: /^https?:\/\/.+/,
                        message: 'Must be a valid HTTP or HTTPS URL'
                      }
                    }}
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Endpoint URL</FormLabel>
                        <FormControl>
                          <Input placeholder="https://example.com/webhook" {...field} />
                        </FormControl>
                        <FormDescription>
                          The URL where webhook events will be sent
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Configuration */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Configuration</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="retryCount"
                      rules={{ 
                        required: 'Retry count is required',
                        min: { value: 0, message: 'Must be 0 or greater' },
                        max: { value: 10, message: 'Must be 10 or less' }
                      }}
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Retry Count</FormLabel>
                          <FormControl>
                            <Input 
                              type="number" 
                              min="0" 
                              max="10"
                              {...field}
                              onChange={(e) => field.onChange(parseInt(e.target.value))}
                            />
                          </FormControl>
                          <FormDescription>
                            Number of retries on failure
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="timeoutSeconds"
                      rules={{ 
                        required: 'Timeout is required',
                        min: { value: 1, message: 'Must be 1 second or more' },
                        max: { value: 300, message: 'Must be 300 seconds or less' }
                      }}
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Timeout (seconds)</FormLabel>
                          <FormControl>
                            <Input 
                              type="number" 
                              min="1" 
                              max="300"
                              {...field}
                              onChange={(e) => field.onChange(parseInt(e.target.value))}
                            />
                          </FormControl>
                          <FormDescription>
                            Request timeout in seconds
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                </CardContent>
              </Card>

              {/* Custom Headers */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Custom Headers</CardTitle>
                  <CardDescription>
                    Add custom headers to be sent with webhook requests
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <FormField
                    control={form.control}
                    name="customHeaders"
                    render={({ field }) => (
                      <FormItem>
                        <div className="space-y-3">
                          {field.value.map((header, index) => (
                            <div key={index} className="flex items-center space-x-2">
                              <Input
                                placeholder="Header name"
                                value={header.key}
                                onChange={(e) => updateCustomHeader(index, 'key', e.target.value)}
                                className="flex-1"
                              />
                              <Input
                                placeholder="Header value"
                                value={header.value}
                                onChange={(e) => updateCustomHeader(index, 'value', e.target.value)}
                                className="flex-1"
                              />
                              <Button
                                type="button"
                                variant="outline"
                                size="icon"
                                onClick={() => removeCustomHeader(index)}
                              >
                                <X className="h-4 w-4" />
                              </Button>
                            </div>
                          ))}
                          <Button
                            type="button"
                            variant="outline"
                            onClick={addCustomHeader}
                            className="w-full"
                          >
                            <Plus className="h-4 w-4 mr-2" />
                            Add Header
                          </Button>
                        </div>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose}>
                Cancel
              </Button>
              <Button type="submit" disabled={loading}>
                {loading ? 'Saving...' : webhook ? 'Update Webhook' : 'Create Webhook'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}