'use client';

import React, { useState, useEffect } from 'react';
import { X, FileText, Mail, Hash, Copy, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import { apiClient, ENDPOINTS } from '@/lib/api';
import { useToast } from '@/hooks/use-toast';

interface EmailTemplate {
  subject: string;
  body: string;
  sequenceNumber: number;
}

interface TemplatesData {
  campaign: {
    id: string;
    campaignId: number;
    name: string;
  };
  templates: EmailTemplate[];
  totalTemplates: number;
}

interface EmailTemplatesModalProps {
  isOpen: boolean;
  onClose: () => void;
  campaignId: string;
  campaignName?: string;
}

export function EmailTemplatesModal({ isOpen, onClose, campaignId, campaignName }: EmailTemplatesModalProps) {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<TemplatesData | null>(null);
  const [selectedTemplate, setSelectedTemplate] = useState<number | null>(null);
  const { toast } = useToast();

  useEffect(() => {
    if (isOpen && campaignId) {
      fetchTemplates();
    }
  }, [isOpen, campaignId]);

  const fetchTemplates = async () => {
    setLoading(true);
    try {
      const response = await apiClient.get<{ success: boolean; data: TemplatesData }>(
        `${ENDPOINTS.v1.campaigns}/${campaignId}/templates`
      );
      
      if (response.success && response.data) {
        setData(response.data);
        // Auto-select first template if available
        if (response.data.templates.length > 0) {
          setSelectedTemplate(response.data.templates[0].sequenceNumber);
        }
      }
    } catch (error) {
      console.error('Failed to fetch templates:', error);
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load email templates',
      });
    } finally {
      setLoading(false);
    }
  };

  const selectedTemplateData = data?.templates.find(template => template.sequenceNumber === selectedTemplate);

  const copyToClipboard = (text: string, type: 'subject' | 'body') => {
    navigator.clipboard.writeText(text).then(() => {
      toast({
        title: 'Copied!',
        description: `Email ${type} copied to clipboard`,
      });
    });
  };

  const getSequenceColor = (sequence: number) => {
    const colors = [
      'bg-blue-100 text-blue-800 hover:bg-blue-100 hover:text-blue-800',
      'bg-green-100 text-green-800 hover:bg-green-100 hover:text-green-800',
      'bg-purple-100 text-purple-800 hover:bg-purple-100 hover:text-purple-800',
      'bg-orange-100 text-orange-800 hover:bg-orange-100 hover:text-orange-800',
      'bg-pink-100 text-pink-800 hover:bg-pink-100 hover:text-pink-800',
    ];
    return colors[(sequence - 1) % colors.length] || 'bg-gray-100 text-gray-800 hover:bg-gray-100 hover:text-gray-800';
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-6xl max-h-[90vh] overflow-hidden">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Email Templates - {campaignName}
          </DialogTitle>
        </DialogHeader>
        
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin" />
            <span className="ml-2">Loading email templates...</span>
          </div>
        ) : data ? (
          <div className="flex gap-4 h-[70vh]">
            {/* Template List Sidebar */}
            <div className="w-1/3 border-r pr-4">
              <div className="mb-4">
                <h3 className="font-semibold text-sm text-muted-foreground mb-2">
                  Email Sequences ({data.totalTemplates})
                </h3>
              </div>
              <div className="space-y-2 overflow-y-auto max-h-[calc(70vh-100px)]">
                {loading ? (
                  // Skeleton loading for templates list
                  Array.from({ length: 5 }, (_, index) => (
                    <div key={index} className="p-3 rounded-lg border bg-muted/20">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <Skeleton className="h-4 w-4" />
                          <Skeleton className="h-4 w-20" />
                        </div>
                        <Skeleton className="h-5 w-16 rounded-full" />
                      </div>
                      <div className="text-xs">
                        <Skeleton className="h-3 w-3/4" />
                      </div>
                    </div>
                  ))
                ) : data ? (
                  data.templates.map((template) => (
                    <div
                      key={template.sequenceNumber}
                      className={`p-3 rounded-lg border cursor-pointer transition-all ${
                        selectedTemplate === template.sequenceNumber 
                          ? 'border-primary bg-primary/5 shadow-sm' 
                          : 'border-border hover:bg-muted hover:border-muted-foreground/20'
                      }`}
                      onClick={() => setSelectedTemplate(template.sequenceNumber)}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <Hash className="h-4 w-4" />
                          <span className="font-medium text-sm">Sequence {template.sequenceNumber}</span>
                        </div>
                        <Badge className={`text-xs ${getSequenceColor(template.sequenceNumber)}`}>
                          Email {template.sequenceNumber}
                        </Badge>
                      </div>
                      <div className="text-xs text-muted-foreground truncate">
                        {template.subject || 'No Subject'}
                      </div>
                    </div>
                  ))
                ) : null}
              </div>
            </div>

            {/* Template Detail View */}
            <div className="flex-1">
              {loading ? (
                <div className="h-full">
                  <div className="mb-4">
                    <div className="flex items-center gap-4 mb-4">
                      <Skeleton className="h-5 w-20 rounded-full" />
                      <Skeleton className="h-4 w-24" />
                    </div>
                  </div>

                  {/* Skeleton Template Preview */}
                  <Card>
                    <CardHeader className="pb-4">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Skeleton className="h-4 w-4" />
                          <Skeleton className="h-4 w-32" />
                        </div>
                        <div className="flex gap-2">
                          <Skeleton className="h-8 w-24" />
                          <Skeleton className="h-8 w-20" />
                        </div>
                      </div>
                    </CardHeader>
                    <CardContent>
                      <div className="border rounded-md bg-white">
                        <div className="border-b bg-gray-50 px-4 py-3">
                          <div className="flex items-center gap-2 mb-1">
                            <Skeleton className="h-3 w-12" />
                          </div>
                          <Skeleton className="h-4 w-3/4" />
                        </div>
                        <div className="p-4 space-y-2">
                          <Skeleton className="h-3 w-full" />
                          <Skeleton className="h-3 w-5/6" />
                          <Skeleton className="h-3 w-4/5" />
                          <Skeleton className="h-3 w-3/4" />
                          <Skeleton className="h-3 w-2/3" />
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  {/* Skeleton Stats */}
                  <div className="mt-4 p-3 bg-muted rounded-md">
                    <div className="flex items-center gap-4">
                      <Skeleton className="h-3 w-16" />
                      <Skeleton className="h-3 w-20" />
                      <Skeleton className="h-3 w-18" />
                    </div>
                  </div>
                </div>
              ) : selectedTemplateData ? (
                <div className="h-full overflow-y-auto">
                  <div className="mb-4">
                    <div className="flex items-center gap-4 mb-4">
                      <Badge className={getSequenceColor(selectedTemplateData.sequenceNumber)}>
                        Sequence {selectedTemplateData.sequenceNumber}
                      </Badge>
                      <span className="text-sm text-muted-foreground">
                        Email Template
                      </span>
                    </div>
                  </div>

                  {/* Email Template Preview */}
                  <Card>
                    <CardHeader className="pb-4">
                      <div className="flex items-center justify-between">
                        <CardTitle className="text-sm font-medium flex items-center gap-2">
                          <Mail className="h-4 w-4" />
                          Email Template Preview
                        </CardTitle>
                        <div className="flex gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => copyToClipboard(selectedTemplateData.subject, 'subject')}
                          >
                            <Copy className="h-3 w-3 mr-1" />
                            Copy Subject
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => copyToClipboard(selectedTemplateData.body, 'body')}
                          >
                            <Copy className="h-3 w-3 mr-1" />
                            Copy Body
                          </Button>
                        </div>
                      </div>
                    </CardHeader>
                    <CardContent>
                      {/* Email Header */}
                      <div className="border rounded-md bg-white">
                        <div className="border-b bg-gray-50 px-4 py-3">
                          <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
                            <span className="font-medium">Subject:</span>
                          </div>
                          <div className="font-medium text-gray-900">
                            {selectedTemplateData.subject || 'No Subject'}
                          </div>
                        </div>
                        
                        {/* Email Body */}
                        <div className="p-4">
                          <div 
                            className="text-sm prose prose-sm max-w-none"
                            dangerouslySetInnerHTML={{ 
                              __html: selectedTemplateData.body || 'No content available' 
                            }}
                          />
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  {/* Template Stats */}
                  <div className="mt-4 p-3 bg-muted rounded-md">
                    <div className="flex items-center gap-4 text-xs text-muted-foreground">
                      <span>Sequence: {selectedTemplateData.sequenceNumber}</span>
                      <span>Subject Length: {selectedTemplateData.subject?.length || 0} chars</span>
                      <span>Body Length: {selectedTemplateData.body?.length || 0} chars</span>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-center h-full text-muted-foreground">
                  <div className="text-center">
                    <FileText className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>Select a template to view details</p>
                  </div>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="text-center py-12 text-muted-foreground">
            <FileText className="h-12 w-12 mx-auto mb-2 opacity-50" />
            <p>No email templates available</p>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}