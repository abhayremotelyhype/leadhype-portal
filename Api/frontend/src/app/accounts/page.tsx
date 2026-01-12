'use client';

import { useState, useEffect, useCallback } from 'react';
import { ProtectedRoute } from '@/components/protected-route';
import { Plus, RefreshCw, AtSign, MoreHorizontal, Eye, WifiOff, AlertCircle, MoreVertical } from 'lucide-react';
import { SidebarTrigger } from '@/components/ui/sidebar';
import { PageHeader } from '@/components/page-header';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useToast } from '@/hooks/use-toast';
import { useErrorHandling } from '@/hooks/use-error-handling';

const formSchema = z.object({
  apiKey: z.string().min(1, 'API Key is required').startsWith('sk_', 'API Key must start with sk_'),
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
});

interface LeadHypeAccount {
  id: string;
  apiKey: string;
  email: string;
  password: string;
  status: 'Active' | 'Inactive' | 'Error';
  emailAccountsCount: number;
  campaignsCount: number;
  totalSent: number;
  totalOpened: number;
  totalReplied: number;
  totalLeads: number;
  openRate: number;
  replyRate: number;
  lastSyncAt?: string;
  createdAt: string;
}

interface EmailAccount {
  id: string;
  email: string;
  name?: string;
  status: 'Active' | 'Inactive' | 'Warming';
  sent: number;
  opened: number;
  replied: number;
  bounced: number;
}

export default function AccountsPage() {
  const { toast } = useToast();
  const { error, handleApiError, handleSuccess } = useErrorHandling({ resetOnSuccess: true, showToast: false });
  const [accounts, setAccounts] = useState<LeadHypeAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEmailModalOpen, setIsEmailModalOpen] = useState(false);
  const [currentAccount, setCurrentAccount] = useState<LeadHypeAccount | null>(null);
  const [emailAccounts, setEmailAccounts] = useState<EmailAccount[]>([]);
  const [emailLoading, setEmailLoading] = useState(false);

  // Set page title
  useEffect(() => {
    document.title = 'Accounts - LeadHype';
  }, []);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      apiKey: '',
      email: '',
      password: '',
    },
  });

  const loadAccounts = useCallback(async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch('/api/accounts', {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to load accounts: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      setAccounts(data.data || []);
      handleSuccess();
    } catch (error: any) {
      handleApiError(error, 'load accounts');
    } finally {
      setLoading(false);
    }
  }, [handleApiError, handleSuccess]);

  const onSubmit = async (values: z.infer<typeof formSchema>) => {
    try {
      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch('/api/accounts', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          apiKey: values.apiKey,
          email: values.email,
          password: values.password,
        }),
      });

      if (!response.ok) {
        throw new Error(`Failed to add account: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      toast({
        title: "Success",
        description: data.message || "Account added successfully",
      });
      
      form.reset();
      setIsAddModalOpen(false);
      await loadAccounts();
    } catch (error: any) {
      toast({
        title: "Error",
        description: error.message || "Failed to add account",
        variant: "destructive",
      });
    }
  };

  const viewEmailAccounts = async (account: LeadHypeAccount) => {
    setCurrentAccount(account);
    setEmailLoading(true);
    setIsEmailModalOpen(true);
    
    try {
      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch(`/api/accounts/${account.id}/email-accounts`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to load email accounts: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      setEmailAccounts(data.data || []);
    } catch (error: any) {
      toast({
        title: "Error",
        description: error.message || "Failed to load email accounts",
        variant: "destructive",
      });
      setEmailAccounts([]);
    } finally {
      setEmailLoading(false);
    }
  };

  const getStatusBadge = (status: LeadHypeAccount['status']) => {
    const variants = {
      Active: 'default',
      Inactive: 'secondary', 
      Error: 'destructive',
    } as const;

    return (
      <Badge variant={variants[status]}>
        {status}
      </Badge>
    );
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  useEffect(() => {
    loadAccounts();
  }, [loadAccounts]);

  return (
    <ProtectedRoute requireAdmin={true}>
      <div className="flex h-full flex-col">
      <PageHeader 
        title="LeadHype Accounts"
        description="Connect and sync your LeadHype accounts to access campaigns and email data"
        mobileDescription="LeadHype integrations"
        icon={AtSign}
        actions={
          <>
            <Button variant="outline" size="sm" onClick={loadAccounts} className="h-8 w-8 sm:w-auto p-0 sm:px-3 text-xs">
              <RefreshCw className="w-3 h-3 sm:mr-2" />
              <span className="hidden sm:inline">Refresh</span>
            </Button>
            <Dialog open={isAddModalOpen} onOpenChange={setIsAddModalOpen}>
              <DialogTrigger asChild>
                <Button size="sm" className="h-8 w-8 sm:w-auto p-0 sm:px-3 text-xs">
                  <Plus className="w-3 h-3 sm:mr-2" />
                  <span className="hidden sm:inline">Add Account</span>
                </Button>
              </DialogTrigger>
              <DialogContent className="mx-4 sm:max-w-[425px]">
                <DialogHeader>
                  <DialogTitle>Add LeadHype Account</DialogTitle>
                  <DialogDescription>
                    Connect your LeadHype account to start managing email campaigns.
                  </DialogDescription>
                </DialogHeader>
                <Separator />
                <Form {...form}>
                  <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                    <FormField
                      control={form.control}
                      name="apiKey"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>API Key</FormLabel>
                          <FormControl>
                            <Input placeholder="sk_live_..." {...field} />
                          </FormControl>
                          <FormDescription>
                            Find your API key in LeadHype Settings → API Keys
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="email"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Email Address</FormLabel>
                          <FormControl>
                            <Input type="email" placeholder="your@email.com" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="password"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Password</FormLabel>
                          <FormControl>
                            <Input type="password" placeholder="••••••••" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <Separator />
                    <div className="flex justify-end space-x-2">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => setIsAddModalOpen(false)}
                      >
                        Cancel
                      </Button>
                      <Button type="submit">Add Account</Button>
                    </div>
                  </form>
                </Form>
              </DialogContent>
            </Dialog>
          </>
        }
      />

      {/* Content */}
      <div className="flex-1 overflow-auto p-1 sm:p-2">
        {!loading && (!accounts || accounts.length === 0) && !error ? (
          <div className="flex flex-col items-center justify-center h-full min-h-[60vh] p-8 text-center space-y-6">
            <div className="relative">
              <div className="w-20 h-20 bg-muted/50 rounded-full flex items-center justify-center">
                <AtSign className="w-10 h-10 text-muted-foreground" />
              </div>
              <div className="absolute -bottom-1 -right-1 w-8 h-8 bg-primary/20 rounded-full flex items-center justify-center">
                <Plus className="w-4 h-4 text-primary" />
              </div>
            </div>
            
            <div className="space-y-3 max-w-md">
              <h3 className="text-xl font-semibold text-foreground">No accounts connected</h3>
              <p className="text-muted-foreground">
                Get started by connecting your first LeadHype account to manage email campaigns and track performance.
              </p>
            </div>
            
            <Button 
              onClick={() => setIsAddModalOpen(true)}
              size="lg"
              className="gap-2"
            >
              <Plus className="w-4 h-4" />
              Add Your First Account
            </Button>
            
            <div className="text-sm text-muted-foreground/80 mt-6 space-y-2 p-4 bg-muted/30 rounded-lg">
              <div className="flex items-center gap-2">
                <span className="text-lg">💡</span>
                <span>You'll need your LeadHype API key to get started</span>
              </div>
              <p className="text-xs">Find it in LeadHype Settings → API Keys</p>
            </div>
          </div>
        ) : (
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="min-w-[180px]">Account</TableHead>
                  <TableHead className="hidden sm:table-cell">Status</TableHead>
                  <TableHead className="hidden md:table-cell">API Key</TableHead>
                  <TableHead className="hidden lg:table-cell">Email Accounts</TableHead>
                  <TableHead className="hidden lg:table-cell">Campaigns</TableHead>
                  <TableHead className="hidden xl:table-cell">Total Sent</TableHead>
                  <TableHead className="hidden xl:table-cell">Open Rate</TableHead>
                  <TableHead className="hidden xl:table-cell">Reply Rate</TableHead>
                  <TableHead className="hidden xl:table-cell">Leads</TableHead>
                  <TableHead className="hidden 2xl:table-cell">Last Sync</TableHead>
                  <TableHead className="hidden 2xl:table-cell">Created</TableHead>
                  <TableHead className="w-16">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {error ? (
                  <TableRow>
                    <TableCell colSpan={12} className="h-48">
                      <div className="flex flex-col items-center justify-center p-8 text-center space-y-4">
                        <div className="relative">
                          <div className="w-12 h-12 bg-destructive/10 rounded-full flex items-center justify-center">
                            <WifiOff className="w-6 h-6 text-destructive/60" />
                          </div>
                          <div className="absolute -top-1 -right-1 w-5 h-5 bg-destructive/20 rounded-full flex items-center justify-center">
                            <AlertCircle className="w-3 h-3 text-destructive" />
                          </div>
                        </div>
                        
                        <div className="space-y-2">
                          <h3 className="text-base font-semibold text-foreground">Connection Error</h3>
                          <p className="text-sm text-muted-foreground max-w-md">
                            Unable to load accounts. Please check your internet connection and try again.
                          </p>
                        </div>
                        
                        <Button 
                          onClick={() => window.location.reload()}
                          variant="outline" 
                          size="sm"
                          className="mt-2 gap-2"
                        >
                          <RefreshCw className="w-4 h-4" />
                          Try Again
                        </Button>
                        
                        <div className="text-xs text-muted-foreground/60 mt-2">
                          {error}
                        </div>
                      </div>
                    </TableCell>
                  </TableRow>
                ) : loading ? (
                  <>
                    {[...Array(3)].map((_, i) => (
                      <TableRow key={i}>
                        <TableCell>
                          <div className="flex items-center space-x-2">
                            <Skeleton className="h-6 w-6 rounded-full" />
                            <div className="space-y-1">
                              <Skeleton className="h-4 w-40" />
                              <Skeleton className="h-3 w-20" />
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="hidden sm:table-cell"><Skeleton className="h-5 w-16" /></TableCell>
                        <TableCell className="hidden md:table-cell"><Skeleton className="h-4 w-24" /></TableCell>
                        <TableCell className="hidden lg:table-cell"><Skeleton className="h-4 w-8" /></TableCell>
                        <TableCell className="hidden lg:table-cell"><Skeleton className="h-4 w-8" /></TableCell>
                        <TableCell className="hidden xl:table-cell"><Skeleton className="h-4 w-16" /></TableCell>
                        <TableCell className="hidden xl:table-cell"><Skeleton className="h-4 w-12" /></TableCell>
                        <TableCell className="hidden xl:table-cell"><Skeleton className="h-4 w-12" /></TableCell>
                        <TableCell className="hidden xl:table-cell"><Skeleton className="h-4 w-8" /></TableCell>
                        <TableCell className="hidden 2xl:table-cell"><Skeleton className="h-4 w-20" /></TableCell>
                        <TableCell className="hidden 2xl:table-cell"><Skeleton className="h-4 w-20" /></TableCell>
                        <TableCell className="w-16">
                          <div className="flex gap-0.5">
                            <Skeleton className="h-7 w-7" />
                            <Skeleton className="h-7 w-7" />
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </>
                ) : (
                  accounts?.map((account) => (
                    <TableRow key={account.id}>
                      <TableCell className="min-w-[180px]">
                        <div className="flex items-center">
                          <div className="mr-2 flex h-6 w-6 items-center justify-center rounded-full bg-primary/10 flex-shrink-0">
                            <span className="text-xs font-semibold text-primary">
                              {account.email?.charAt(0)?.toUpperCase() || '@'}
                            </span>
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="text-xs truncate">{account.email}</div>
                            <div className="text-xs text-muted-foreground truncate">{account.id}</div>
                            <div className="sm:hidden flex flex-wrap gap-1 mt-1">
                              {getStatusBadge(account.status)}
                              <span className="text-xs text-blue-600">📧{account.emailAccountsCount}</span>
                              <span className="text-xs text-purple-600">📊{account.campaignsCount}</span>
                            </div>
                          </div>
                        </div>
                      </TableCell>
                      <TableCell className="hidden sm:table-cell">{getStatusBadge(account.status)}</TableCell>
                      <TableCell className="hidden md:table-cell">
                        <span className="text-xs font-mono text-muted-foreground">
                          {account.apiKey.slice(0, 8)}...
                        </span>
                      </TableCell>
                      <TableCell className="hidden lg:table-cell">
                        <span className="text-xs text-blue-600">
                          {account.emailAccountsCount}
                        </span>
                      </TableCell>
                      <TableCell className="hidden lg:table-cell">
                        <span className="text-xs text-purple-600">
                          {account.campaignsCount}
                        </span>
                      </TableCell>
                      <TableCell className="hidden xl:table-cell">
                        <span className="text-xs">
                          {account.totalSent.toLocaleString()}
                        </span>
                      </TableCell>
                      <TableCell className="hidden xl:table-cell">
                        <span className="text-xs text-blue-600">
                          {account.openRate}%
                        </span>
                      </TableCell>
                      <TableCell className="hidden xl:table-cell">
                        <span className="text-xs text-green-600">
                          {account.replyRate}%
                        </span>
                      </TableCell>
                      <TableCell className="hidden xl:table-cell">
                        <span className="text-xs text-orange-600">
                          {account.totalLeads.toLocaleString()}
                        </span>
                      </TableCell>
                      <TableCell className="hidden 2xl:table-cell">
                        <span className="text-xs text-muted-foreground">
                          {account.lastSyncAt ? formatDate(account.lastSyncAt) : 'Never'}
                        </span>
                      </TableCell>
                      <TableCell className="hidden 2xl:table-cell">
                        <span className="text-xs text-muted-foreground">
                          {formatDate(account.createdAt)}
                        </span>
                      </TableCell>
                      <TableCell className="w-16">
                        <div className="flex items-center gap-0.5">
                          <Button 
                            variant="ghost" 
                            size="sm" 
                            onClick={() => viewEmailAccounts(account)}
                            className="h-7 w-7 p-0"
                            title="View email accounts"
                          >
                            <Eye className="w-3 h-3" />
                          </Button>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="h-7 w-7 p-0"
                              >
                                <MoreVertical className="w-3 h-3" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end" className="w-48">
                              <DropdownMenuItem onClick={() => viewEmailAccounts(account)}>
                                <Eye className="w-4 h-4 mr-2" />
                                View Email Accounts
                              </DropdownMenuItem>
                              <DropdownMenuItem>
                                <RefreshCw className="w-4 h-4 mr-2" />
                                Sync Account
                              </DropdownMenuItem>
                              <DropdownMenuItem className="text-red-600 focus:text-red-600">
                                <AlertCircle className="w-4 h-4 mr-2" />
                                Remove Account
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        )}
      </div>

      {/* Email Accounts Modal */}
      <Dialog open={isEmailModalOpen} onOpenChange={setIsEmailModalOpen}>
        <DialogContent className="mx-4 sm:max-w-[700px]">
          <DialogHeader>
            <DialogTitle>
              Email Accounts - {currentAccount?.email}
            </DialogTitle>
            <DialogDescription>
              View all email accounts connected to this LeadHype account.
            </DialogDescription>
          </DialogHeader>
          <Separator />
          <div className="max-h-[350px] sm:max-h-[400px] overflow-y-auto">
            {emailLoading ? (
              <div className="space-y-3">
                {[...Array(2)].map((_, i) => (
                  <div key={i} className="space-y-2">
                    <div className="flex items-center space-x-2">
                      <Skeleton className="h-4 w-40" />
                      <Skeleton className="h-5 w-16" />
                    </div>
                    <div className="flex space-x-4">
                      <Skeleton className="h-4 w-12" />
                      <Skeleton className="h-4 w-12" />
                      <Skeleton className="h-4 w-12" />
                      <Skeleton className="h-4 w-12" />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="min-w-[150px]">Email</TableHead>
                      <TableHead className="hidden sm:table-cell">Status</TableHead>
                      <TableHead className="hidden md:table-cell">Sent</TableHead>
                      <TableHead className="hidden md:table-cell">Opened</TableHead>
                      <TableHead className="hidden lg:table-cell">Replied</TableHead>
                      <TableHead className="hidden lg:table-cell">Bounced</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {emailAccounts?.map((emailAccount) => (
                      <TableRow key={emailAccount.id}>
                        <TableCell className="min-w-[150px]">
                          <div>
                            <div className="text-xs truncate">{emailAccount.email}</div>
                            {emailAccount.name && (
                              <div className="text-xs text-muted-foreground truncate">{emailAccount.name}</div>
                            )}
                            <div className="sm:hidden flex flex-wrap gap-1 mt-1">
                              <Badge variant={emailAccount.status === 'Active' ? 'default' : 'secondary'} className="text-xs px-1.5 py-0.5">
                                {emailAccount.status}
                              </Badge>
                              <span className="text-xs">📧{emailAccount.sent}</span>
                              <span className="text-xs text-blue-600">👀{emailAccount.opened}</span>
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="hidden sm:table-cell">
                          <Badge variant={emailAccount.status === 'Active' ? 'default' : 'secondary'} className="text-xs px-2 py-1">
                            {emailAccount.status}
                          </Badge>
                        </TableCell>
                        <TableCell className="hidden md:table-cell">
                          <span className="text-xs">{emailAccount.sent}</span>
                        </TableCell>
                        <TableCell className="hidden md:table-cell">
                          <span className="text-xs text-blue-600">{emailAccount.opened}</span>
                        </TableCell>
                        <TableCell className="hidden lg:table-cell">
                          <span className="text-xs text-green-600">{emailAccount.replied}</span>
                        </TableCell>
                        <TableCell className="hidden lg:table-cell">
                          <span className="text-xs text-red-600">{emailAccount.bounced}</span>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>
      </div>
    </ProtectedRoute>
  );
}