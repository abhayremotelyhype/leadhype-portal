import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { Badge } from '@/components/ui/badge';
import { Filter, RefreshCw, Users, Target, Mail, Crown, UserCheck } from 'lucide-react';

interface User {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: string;
  isActive: boolean;
  assignedClientIds?: string[];
}

interface Client {
  id: string;
  name: string;
}

interface Campaign {
  id: string;
  name: string;
  clientId: string;
  clientName: string;
}

interface EmailAccount {
  id: string;
  email: string;
  name?: string;
  campaignIds: string[];
}

interface DashboardFiltersProps {
  isFilterOpen: boolean;
  setIsFilterOpen: (open: boolean) => void;
  selectedUsers: string[];
  selectedClients: string[];
  selectedCampaigns: string[];
  selectedEmailAccounts: string[];
  selectAllUsers: boolean;
  selectAllClients: boolean;
  selectAllCampaigns: boolean;
  selectAllEmailAccounts: boolean;
  allUsers: User[];
  allClients: Client[];
  allCampaigns: Campaign[];
  allEmailAccounts: EmailAccount[];
  loadingFilterData: boolean;
  userSearchQuery: string;
  setUserSearchQuery: (query: string) => void;
  campaignSearchQuery: string;
  setCampaignSearchQuery: (query: string) => void;
  handleUserToggle: (userId: string) => void;
  handleClientToggle: (clientId: string) => void;
  handleCampaignToggle: (campaignId: string) => void;
  handleEmailAccountToggle: (emailAccountId: string) => void;
  handleSelectAllUsers: (checked: boolean) => void;
  handleSelectAllClients: (checked: boolean) => void;
  handleSelectAllCampaigns: (checked: boolean) => void;
  handleSelectAllEmailAccounts: (checked: boolean) => void;
  applyFilters: () => Promise<void>;
  clearFilters: () => Promise<void>;
  refreshing: boolean;
  onRefresh: () => void;
  isAdmin?: boolean;
  campaignScope?: 'all' | 'specific';
  setCampaignScope?: (scope: 'all' | 'specific') => void;
}

export function DashboardFilters({
  isFilterOpen,
  setIsFilterOpen,
  selectedUsers,
  selectedClients,
  selectedCampaigns,
  selectedEmailAccounts,
  selectAllUsers,
  selectAllClients,
  selectAllCampaigns,
  selectAllEmailAccounts,
  allUsers,
  allClients,
  allCampaigns,
  allEmailAccounts,
  loadingFilterData,
  userSearchQuery,
  setUserSearchQuery,
  campaignSearchQuery,
  setCampaignSearchQuery,
  handleUserToggle,
  handleClientToggle,
  handleCampaignToggle,
  handleEmailAccountToggle,
  handleSelectAllUsers,
  handleSelectAllClients,
  handleSelectAllCampaigns,
  handleSelectAllEmailAccounts,
  applyFilters,
  clearFilters,
  refreshing,
  onRefresh,
  isAdmin = false,
  campaignScope = 'specific',
  setCampaignScope
}: DashboardFiltersProps) {
  // Filter users by search
  const filterUsersBySearch = (users: User[]) => {
    if (!userSearchQuery) return users;
    return users.filter(user => {
      const searchTerm = userSearchQuery.toLowerCase();
      return (
        user.username.toLowerCase().includes(searchTerm) ||
        user.email.toLowerCase().includes(searchTerm) ||
        (user.firstName && user.firstName.toLowerCase().includes(searchTerm)) ||
        (user.lastName && user.lastName.toLowerCase().includes(searchTerm))
      );
    });
  };

  // Filter clients by selected users
  const filterClientsByUsers = (clients: Client[]) => {
    if (selectedUsers.length === 0) return clients;
    
    // Get all assigned client IDs from selected users
    const assignedClientIds = new Set<string>();
    selectedUsers.forEach(userId => {
      const user = allUsers.find(u => u.id === userId);
      if (user?.assignedClientIds) {
        user.assignedClientIds.forEach(clientId => assignedClientIds.add(clientId));
      }
    });
    
    return clients.filter(client => assignedClientIds.has(client.id));
  };

  // Filter campaigns by search and selected clients
  const filterCampaignsBySearchAndClient = (campaigns: Campaign[]) => {
    return campaigns.filter(campaign => {
      const matchesSearch = campaignSearchQuery === '' || 
        campaign.name.toLowerCase().includes(campaignSearchQuery.toLowerCase()) ||
        campaign.clientName.toLowerCase().includes(campaignSearchQuery.toLowerCase());
      
      const matchesClient = selectedClients.length === 0 || selectedClients.includes(campaign.clientId);
      
      return matchesSearch && matchesClient;
    });
  };

  const filteredUsers = filterUsersBySearch(allUsers);
  const filteredClientsFromUsers = filterClientsByUsers(allClients);
  const filteredCampaigns = filterCampaignsBySearchAndClient(allCampaigns);
  
  const allVisibleUsersSelected = filteredUsers.length > 0 && 
    filteredUsers.every(user => selectedUsers.includes(user.id));
  const allVisibleClientsSelected = filteredClientsFromUsers.length > 0 && 
    filteredClientsFromUsers.every(client => selectedClients.includes(client.id));
  const allVisibleCampaignsSelected = filteredCampaigns.length > 0 && 
    filteredCampaigns.every(campaign => selectedCampaigns.includes(campaign.id));

  return (
    <TooltipProvider>
      <div className="flex items-center gap-2">
      <Dialog open={isFilterOpen} onOpenChange={setIsFilterOpen}>
        <DialogTrigger asChild>
          <Button variant="outline" size="sm">
            <Filter className="w-4 h-4 sm:mr-2" />
            <span className="hidden sm:inline">Filter</span>
          </Button>
        </DialogTrigger>
        <DialogContent className="max-w-7xl w-[95vw] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Filter className="w-5 h-5" />
              Dashboard Filters
            </DialogTitle>
            <DialogDescription>
              Filter your dashboard data by clients, campaigns, and email accounts
            </DialogDescription>
          </DialogHeader>
          
          {/* Admin Campaign Scope Selection */}
          {isAdmin && setCampaignScope && (
            <div className="border-b pb-4 mb-4">
              <div className="flex items-center gap-2 mb-3">
                <Crown className="w-4 h-4 text-amber-500" />
                <Label className="text-sm font-semibold">Admin: Campaign Scope</Label>
              </div>
              <RadioGroup 
                value={campaignScope} 
                onValueChange={(value: 'all' | 'specific') => setCampaignScope(value)}
                className="flex flex-col gap-2"
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="all" id="scope-all" />
                  <Label htmlFor="scope-all" className="text-sm font-medium">
                    All Campaigns System-Wide
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="specific" id="scope-specific" />
                  <Label htmlFor="scope-specific" className="text-sm font-medium">
                    Campaigns of Specific Clients
                  </Label>
                </div>
              </RadioGroup>
              <p className="text-xs text-muted-foreground mt-2">
                {campaignScope === 'all' 
                  ? 'View performance data across all campaigns from all clients'
                  : 'Filter campaigns by selecting specific clients below'
                }
              </p>
            </div>
          )}
          
          <div className="grid grid-cols-1 lg:grid-cols-4 gap-6 py-4" style={{ gridTemplateColumns: '0.8fr 0.8fr 1fr 1.2fr' }}>
            
            {/* User Selection */}
            <div className="space-y-4">
              <div>
                <Label className="text-sm font-medium">
                  Users
                  {isAdmin && campaignScope === 'all' && (
                    <span className="text-xs text-amber-600 ml-2">(Disabled - All campaigns selected)</span>
                  )}
                </Label>
                <p className="text-xs text-muted-foreground">
                  {isAdmin && campaignScope === 'all' 
                    ? 'All users included when viewing all campaigns'
                    : 'Select users to filter by'
                  }
                </p>
              </div>
              
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="select-all-users"
                    checked={isAdmin && campaignScope === 'all' ? true : allVisibleUsersSelected}
                    onCheckedChange={isAdmin && campaignScope === 'all' ? undefined : handleSelectAllUsers}
                    disabled={isAdmin && campaignScope === 'all'}
                  />
                  <Label htmlFor="select-all-users" className={`text-sm font-medium ${isAdmin && campaignScope === 'all' ? 'text-muted-foreground' : ''}`}>
                    Select All Users ({filteredUsers.length})
                  </Label>
                </div>
                
                {/* User Search */}
                <Input
                  placeholder="Search users..."
                  value={userSearchQuery}
                  onChange={(e) => setUserSearchQuery(e.target.value)}
                  className="text-sm"
                  disabled={isAdmin && campaignScope === 'all'}
                />
                
                <div className={`h-64 overflow-y-auto space-y-2 border rounded-md p-2 ${isAdmin && campaignScope === 'all' ? 'opacity-50 pointer-events-none' : ''}`}>
                  {loadingFilterData ? (
                    <div className="space-y-2">
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <div className="flex-1">
                          <Skeleton className="h-4 w-32 mb-1" />
                          <Skeleton className="h-3 w-48" />
                        </div>
                        <Skeleton className="h-4 w-8" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <div className="flex-1">
                          <Skeleton className="h-4 w-28 mb-1" />
                          <Skeleton className="h-3 w-40" />
                        </div>
                        <Skeleton className="h-4 w-8" />
                      </div>
                    </div>
                  ) : filteredUsers.length === 0 ? (
                    <Alert className="border-dashed">
                      <UserCheck />
                      <AlertTitle>No Users Found</AlertTitle>
                      <AlertDescription className="text-center ml-2">
                        {userSearchQuery ? 
                          `No users match "${userSearchQuery}". Try different keywords.` :
                          'No users found. Try refreshing or contact support.'
                        }
                      </AlertDescription>
                    </Alert>
                  ) : (
                    filteredUsers.map((user) => (
                      <div key={user.id} className="flex items-center space-x-2">
                        <Checkbox
                          id={`user-${user.id}`}
                          checked={isAdmin && campaignScope === 'all' ? true : selectedUsers.includes(user.id)}
                          onCheckedChange={isAdmin && campaignScope === 'all' ? undefined : () => handleUserToggle(user.id)}
                          disabled={isAdmin && campaignScope === 'all'}
                        />
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <Label 
                              htmlFor={`user-${user.id}`} 
                              className={`text-sm flex-1 cursor-pointer ${isAdmin && campaignScope === 'all' ? 'text-muted-foreground' : ''}`}
                            >
                              <div className="flex items-center justify-between">
                                <div className="flex flex-col min-w-0 flex-1">
                                  <span className="font-medium truncate">
                                    {user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : user.username}
                                  </span>
                                  <span className="text-xs text-muted-foreground truncate">
                                    {user.email}
                                  </span>
                                </div>
                                <div className="flex items-center gap-1.5 ml-2 flex-shrink-0">
                                  {user.role === 'Admin' && (
                                    <Badge variant="secondary" className="h-5 px-1.5 text-xs bg-orange-100 text-orange-700 hover:bg-orange-200">
                                      <Crown className="h-3 w-3 mr-1" />
                                      Admin
                                    </Badge>
                                  )}
                                  <Badge variant="outline" className="h-5 px-2 text-xs">
                                    {user.assignedClientIds?.length || 0}
                                  </Badge>
                                  {!user.isActive && (
                                    <Badge variant="destructive" className="h-5 px-1.5 text-xs">
                                      Inactive
                                    </Badge>
                                  )}
                                </div>
                              </div>
                            </Label>
                          </TooltipTrigger>
                          <TooltipContent>
                            <div className="text-sm">
                              <div className="font-medium">
                                {user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : user.username}
                              </div>
                              <div className="text-xs text-muted-foreground">{user.email}</div>
                              {user.role === 'Admin' && <div className="text-xs text-orange-600">Admin User</div>}
                              <div className="text-xs">Clients: {user.assignedClientIds?.length || 0}</div>
                              {!user.isActive && <div className="text-xs text-red-600">Status: Inactive</div>}
                            </div>
                          </TooltipContent>
                        </Tooltip>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>

            {/* Client Selection */}
            <div className="space-y-4">
              <div>
                <Label className="text-sm font-medium">
                  Clients
                  {isAdmin && campaignScope === 'all' && (
                    <span className="text-xs text-amber-600 ml-2">(Disabled - All campaigns selected)</span>
                  )}
                </Label>
                <p className="text-xs text-muted-foreground">
                  {isAdmin && campaignScope === 'all' 
                    ? 'All clients included when viewing all campaigns'
                    : selectedUsers.length > 0 
                      ? `Clients assigned to selected users (${filteredClientsFromUsers.length})`
                      : 'Select users first to see their assigned clients'
                  }
                </p>
              </div>
              
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="select-all-clients"
                    checked={isAdmin && campaignScope === 'all' ? true : selectAllClients}
                    onCheckedChange={isAdmin && campaignScope === 'all' ? undefined : handleSelectAllClients}
                    disabled={isAdmin && campaignScope === 'all'}
                  />
                  <Label htmlFor="select-all-clients" className={`text-sm font-medium ${isAdmin && campaignScope === 'all' ? 'text-muted-foreground' : ''}`}>
                    Select All Clients ({filteredClientsFromUsers.length})
                  </Label>
                </div>
                
                {/* Client Search */}
                <Input
                  placeholder="Search clients..."
                  className="text-sm"
                  disabled={isAdmin && campaignScope === 'all'}
                />
                
                <div className={`h-64 overflow-y-auto space-y-2 border rounded-md p-2 ${isAdmin && campaignScope === 'all' ? 'opacity-50 pointer-events-none' : ''}`}>
                  {loadingFilterData ? (
                    <div className="space-y-2">
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                    </div>
                  ) : selectedUsers.length === 0 ? (
                    <Alert className="border-dashed">
                      <UserCheck />
                      <AlertTitle>Select Users First</AlertTitle>
                      <AlertDescription className="text-center ml-2">
                        Please select users first to see their assigned clients
                      </AlertDescription>
                    </Alert>
                  ) : filteredClientsFromUsers.length === 0 ? (
                    <Alert className="border-dashed">
                      <Users />
                      <AlertTitle>No Clients Found</AlertTitle>
                      <AlertDescription className="text-center ml-2">
                        Selected users have no assigned clients. <span className="text-xs text-muted-foreground">Try selecting different users.</span>
                      </AlertDescription>
                    </Alert>
                  ) : (
                    filteredClientsFromUsers.map((client) => (
                      <div key={client.id} className="flex items-center space-x-2">
                        <Checkbox
                          id={`client-${client.id}`}
                          checked={isAdmin && campaignScope === 'all' ? true : selectedClients.includes(client.id)}
                          onCheckedChange={isAdmin && campaignScope === 'all' ? undefined : () => handleClientToggle(client.id)}
                          disabled={isAdmin && campaignScope === 'all'}
                        />
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <Label 
                              htmlFor={`client-${client.id}`} 
                              className={`text-sm flex-1 cursor-pointer ${isAdmin && campaignScope === 'all' ? 'text-muted-foreground' : ''}`}
                            >
                              <span className="truncate block max-w-[200px] font-medium">{client.name}</span>
                            </Label>
                          </TooltipTrigger>
                          <TooltipContent>
                            <div className="text-sm font-medium">{client.name}</div>
                          </TooltipContent>
                        </Tooltip>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
            
            {/* Campaign Selection */}
            <div className="space-y-4">
              <div>
                <Label className="text-sm font-medium">
                  Campaigns
                  {isAdmin && campaignScope === 'all' && (
                    <span className="text-xs text-amber-600 ml-2">(Disabled - All campaigns selected)</span>
                  )}
                </Label>
                <p className="text-xs text-muted-foreground">
                  {isAdmin && campaignScope === 'all' 
                    ? 'All campaigns included when viewing all campaigns'
                    : `Select campaigns to include ${selectedCampaigns.length > 0 ? `(${selectedCampaigns.length} selected)` : ''}`
                  }
                </p>
              </div>
              
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="select-all-campaigns"
                    checked={isAdmin && campaignScope === 'all' ? true : allVisibleCampaignsSelected}
                    onCheckedChange={isAdmin && campaignScope === 'all' ? undefined : handleSelectAllCampaigns}
                    disabled={isAdmin && campaignScope === 'all'}
                  />
                  <Label htmlFor="select-all-campaigns" className={`text-sm font-medium ${isAdmin && campaignScope === 'all' ? 'text-muted-foreground' : ''}`}>
                    Select All Visible ({filteredCampaigns.length})
                  </Label>
                </div>
                
                {/* Campaign Search */}
                <Input
                  placeholder="Search campaigns or clients..."
                  value={campaignSearchQuery}
                  onChange={(e) => setCampaignSearchQuery(e.target.value)}
                  className="text-sm"
                  disabled={isAdmin && campaignScope === 'all'}
                />
                
                <div className={`h-64 overflow-y-auto space-y-2 border rounded-md p-2 ${isAdmin && campaignScope === 'all' ? 'opacity-50 pointer-events-none' : ''}`}>
                  {loadingFilterData ? (
                    <div className="space-y-2">
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                    </div>
                  ) : filteredCampaigns.length === 0 ? (
                    <Alert className="border-dashed">
                      <Target />
                      <AlertTitle>No Campaigns Found</AlertTitle>
                      <AlertDescription className="text-center ml-2">
                        {campaignSearchQuery ? 
                          `No campaigns match "${campaignSearchQuery}". Try different keywords.` :
                          'No campaigns found for selected clients.'
                        }
                      </AlertDescription>
                    </Alert>
                  ) : (
                    filteredCampaigns.map((campaign) => (
                      <div key={campaign.id} className="flex items-center space-x-2 ">
                        <Checkbox
                          id={`campaign-${campaign.id}`}
                          checked={isAdmin && campaignScope === 'all' ? true : selectedCampaigns.includes(campaign.id)}
                          onCheckedChange={isAdmin && campaignScope === 'all' ? undefined : () => handleCampaignToggle(campaign.id)}
                          disabled={isAdmin && campaignScope === 'all'}
                        />
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <Label 
                              htmlFor={`campaign-${campaign.id}`} 
                              className={`text-sm flex-1 cursor-pointer ${isAdmin && campaignScope === 'all' ? 'text-muted-foreground' : ''}`}
                            >
                              <div className="flex items-center gap-2 min-w-0">
                                <span className="font-medium truncate max-w-[200px]">{campaign.name}</span>
                                <span className="text-xs text-muted-foreground shrink-0">
                                  ({campaign.clientName})
                                </span>
                              </div>
                            </Label>
                          </TooltipTrigger>
                          <TooltipContent>
                            <div className="text-sm">
                              <div className="font-medium">{campaign.name}</div>
                              <div className="text-xs text-muted-foreground">Client: {campaign.clientName}</div>
                            </div>
                          </TooltipContent>
                        </Tooltip>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
            
            {/* Email Accounts Selection */}
            <div className="space-y-4">
              <div>
                <Label className="text-sm font-medium">Email Accounts</Label>
                <p className="text-xs text-muted-foreground">Select email accounts</p>
              </div>
              
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="select-all-email-accounts"
                    checked={selectAllEmailAccounts}
                    onCheckedChange={handleSelectAllEmailAccounts}
                  />
                  <Label htmlFor="select-all-email-accounts" className="text-sm font-medium">
                    Select All Accounts ({allEmailAccounts.length})
                  </Label>
                </div>
                
                {/* Email Account Search */}
                <Input
                  placeholder="Search email accounts..."
                  className="text-sm"
                />
                
                <div className="h-64 overflow-y-auto space-y-2 border rounded-md p-2">
                  {loadingFilterData ? (
                    <div className="space-y-2">
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                      <div className="flex items-center space-x-2">
                        <Skeleton className="h-4 w-4" />
                        <Skeleton className="h-4 flex-1" />
                      </div>
                    </div>
                  ) : selectedCampaigns.length === 0 ? (
                    <Alert className="border-dashed">
                      <Mail />
                      <AlertTitle>Select Campaigns</AlertTitle>
                      <AlertDescription className="text-center ml-2">
                        Select campaigns first
                      </AlertDescription>
                    </Alert>
                  ) : allEmailAccounts.length === 0 ? (
                    <Alert className="border-dashed">
                      <Mail />
                      <AlertTitle>No Email Accounts Found</AlertTitle>
                      <AlertDescription className="text-center ml-2">
                        No email accounts found for selected campaigns. <span className="text-xs text-muted-foreground">Try different campaigns.</span>
                      </AlertDescription>
                    </Alert>
                  ) : (
                    allEmailAccounts.map((account) => (
                      <div key={account.id} className="flex items-center space-x-2 ">
                        <Checkbox
                          id={`email-account-${account.id}`}
                          checked={selectedEmailAccounts.includes(account.id)}
                          onCheckedChange={() => handleEmailAccountToggle(account.id)}
                        />
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <Label 
                              htmlFor={`email-account-${account.id}`} 
                              className="text-sm flex-1 cursor-pointer"
                            >
                              <div className="flex items-center gap-2 min-w-0">
                                <span className="font-medium truncate max-w-[180px]">{account.email}</span>
                                <span className="text-xs text-muted-foreground shrink-0">
                                  ({account.name || 'Unnamed'})
                                </span>
                              </div>
                            </Label>
                          </TooltipTrigger>
                          <TooltipContent>
                            <div className="text-sm">
                              <div className="font-medium">{account.email}</div>
                              <div className="text-xs text-muted-foreground">Name: {account.name || 'Unnamed'}</div>
                            </div>
                          </TooltipContent>
                        </Tooltip>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          </div>
          
          <DialogFooter className="flex justify-between">
            <Button variant="outline" onClick={clearFilters}>
              Clear All
            </Button>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setIsFilterOpen(false)}>
                Cancel
              </Button>
              <Button onClick={applyFilters}>
                Apply Filters
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      
      <Button 
        variant="outline" 
        size="sm" 
        onClick={onRefresh}
        disabled={refreshing}
      >
        {refreshing ? (
          <>
            <RefreshCw className="w-4 h-4 sm:mr-2 animate-spin" />
            <span className="hidden sm:inline">Refreshing...</span>
          </>
        ) : (
          <>
            <RefreshCw className="w-4 h-4 sm:mr-2" />
            <span className="hidden sm:inline">Refresh</span>
          </>
        )}
      </Button>
      </div>
    </TooltipProvider>
  );
}