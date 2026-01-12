'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { ProtectedRoute } from '@/components/protected-route';
import { useAuth } from '@/contexts/auth-context';
import { PageHeader } from '@/components/page-header';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';
import { Switch } from '@/components/ui/switch';
import { Checkbox } from '@/components/ui/checkbox';
import { Users, Plus, PenSquare, Trash2, Mail, User, Building, Shield, AlertCircle, CheckCircle, X, UserPlus, RotateCcw, Eye, Activity, Globe, Clock, Laptop, Smartphone, TabletSmartphone, EllipsisVertical, Settings, Key } from 'lucide-react';
import { apiClient, handleApiErrorWithToast } from '@/lib/api';

interface User {
  id: string;
  email: string;
  username: string;
  role: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  assignedClientIds: string[];
  lastLoginAt?: string;
}

interface Client {
  id: string;
  name: string;
}

interface UserSession {
  id: string;
  deviceName: string | null;
  ipAddress: string | null;
  createdAt: string;
  lastAccessedAt: string;
  isCurrent: boolean;
}

interface CreateUserData {
  email: string;
  password: string;
  role: string;
  firstName: string;
  lastName: string;
}

interface EditUserData {
  email: string;
  role: string;
  firstName: string;
  lastName: string;
}

export default function UsersPage() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createUserData, setCreateUserData] = useState<CreateUserData>({
    email: '',
    password: '',
    role: 'User',
    firstName: '',
    lastName: ''
  });
  const [createError, setCreateError] = useState('');
  const [isCreating, setIsCreating] = useState(false);

  // Set page title
  useEffect(() => {
    document.title = 'Users - LeadHype';
  }, []);
  const [clients, setClients] = useState<Client[]>([]);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [assignedClients, setAssignedClients] = useState<string[]>([]);
  const [showToggleConfirm, setShowToggleConfirm] = useState(false);
  const [userToToggle, setUserToToggle] = useState<User | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [userToDelete, setUserToDelete] = useState<User | null>(null);
  const [isUpdating, setIsUpdating] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editUserData, setEditUserData] = useState<EditUserData>({
    email: '',
    role: 'User',
    firstName: '',
    lastName: ''
  });
  const [editError, setEditError] = useState('');
  const [isEditing, setIsEditing] = useState(false);
  const [showPasswordResetModal, setShowPasswordResetModal] = useState(false);
  const [newPassword, setNewPassword] = useState('');
  const [isResettingPassword, setIsResettingPassword] = useState(false);
  const [showViewClientsModal, setShowViewClientsModal] = useState(false);
  const [userClients, setUserClients] = useState<Client[]>([]);
  const [isLoadingUserClients, setIsLoadingUserClients] = useState(false);
  const [isLoadingMoreClients, setIsLoadingMoreClients] = useState(false);
  const [clientsPage, setClientsPage] = useState(1);
  const [hasMoreClients, setHasMoreClients] = useState(true);
  const [totalClientsCount, setTotalClientsCount] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');
  const [filteredClients, setFilteredClients] = useState<Client[]>([]);
  const clientsScrollRef = useRef<HTMLDivElement>(null);
  const [tempAssignedClients, setTempAssignedClients] = useState<string[]>([]);
  const [isSavingClientAssignments, setIsSavingClientAssignments] = useState(false);
  const [showSessionsModal, setShowSessionsModal] = useState(false);
  const [userSessions, setUserSessions] = useState<UserSession[]>([]);
  const [isLoadingUserSessions, setIsLoadingUserSessions] = useState(false);

  useEffect(() => {
    loadUsers();
    loadClients();
  }, []);

  const loadUsers = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      // Don't try to load users if no token (user logged out)
      if (!token) {
        setLoading(false);
        return;
      }
      
      const userData = await apiClient.get('/api/users') as User[];
      setUsers(userData);
    } catch (error) {
      handleApiErrorWithToast(error, 'load users', toast);
    } finally {
      setLoading(false);
    }
  };

  const loadClients = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      // Don't try to load clients if no token (user logged out)
      if (!token) {
        return;
      }
      
      // Load all clients with a high limit to avoid pagination issues
      // In the future, this could be enhanced with proper pagination handling
      const response = await fetch('/api/clients?limit=10000', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const clientData = await response.json();
        const clientArray = clientData.clients || [];
        // Ensure we always set an array
        setClients(Array.isArray(clientArray) ? clientArray : []);
      }
    } catch (error) {
      console.error('Failed to load clients:', error);
      // Ensure clients remains an empty array on error
      setClients([]);
    }
  };

  const handleCreateUser = async () => {
    if (!createUserData.email || !createUserData.password) {
      setCreateError('Email and password are required');
      return;
    }

    setIsCreating(true);
    setCreateError('');

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch('/api/users', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(createUserData),
      });

      if (response.ok) {
        toast.success('User created successfully');
        setShowCreateModal(false);
        setCreateUserData({
          email: '',
          password: '',
          role: 'User',
          firstName: '',
          lastName: ''
        });
        loadUsers();
      } else {
        const errorData = await response.json();
        setCreateError(errorData.message || 'Failed to create user');
      }
    } catch (error) {
      console.error('Failed to create user:', error);
      setCreateError('Failed to create user');
    } finally {
      setIsCreating(false);
    }
  };

  const handleDeleteUser = (user: User) => {
    // Prevent deleting protected admin accounts
    if (isProtectedAdmin(user)) {
      const message = currentUser && user.id === currentUser.id 
        ? 'You cannot delete your own account'
        : 'Cannot delete admin accounts';
      
      toast.error(message);
      return;
    }

    setUserToDelete(user);
    setShowDeleteConfirm(true);
  };

  const confirmDeleteUser = async () => {
    if (!userToDelete) return;

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/users/${userToDelete.id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        toast.success('User deleted successfully');
        loadUsers();
      } else {
        const errorData = await response.json();
        toast.error(errorData.message || 'Failed to delete user');
      }
    } catch (error) {
      console.error('Failed to delete user:', error);
      toast.error('Failed to delete user');
    } finally {
      setShowDeleteConfirm(false);
      setUserToDelete(null);
    }
  };

  const handleAssignClients = (user: User) => {
    setSelectedUser(user);
    setAssignedClients(user.assignedClientIds || []);
    setShowAssignModal(true);
  };

  const handleUpdateUserClients = async () => {
    if (!selectedUser) return;

    setIsUpdating(true);
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/users/${selectedUser.id}/clients`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(assignedClients),
      });

      if (response.ok) {
        toast.success('Client assignments updated successfully');
        setShowAssignModal(false);
        loadUsers();
      } else {
        toast.error('Failed to update client assignments');
      }
    } catch (error) {
      console.error('Failed to update client assignments:', error);
      toast.error(
'Failed to update client assignments',
);
    } finally {
      setIsUpdating(false);
    }
  };

  const toggleClientAssignment = (clientId: string) => {
    setAssignedClients(prev =>
      prev.includes(clientId)
        ? prev.filter(id => id !== clientId)
        : [...prev, clientId]
    );
  };

  const handleEditUser = (user: User) => {
    setSelectedUser(user);
    setEditUserData({
      email: user.email,
      role: user.role,
      firstName: user.firstName || '',
      lastName: user.lastName || ''
    });
    setEditError('');
    setShowEditModal(true);
  };

  const handleUpdateUser = async () => {
    if (!selectedUser) return;
    if (!editUserData.email) {
      setEditError('Email is required');
      return;
    }

    setIsEditing(true);
    setEditError('');

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/users/${selectedUser.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(editUserData),
      });

      if (response.ok) {
        toast.success('User updated successfully');
        setShowEditModal(false);
        loadUsers();
      } else {
        const errorData = await response.json();
        setEditError(errorData.message || 'Failed to update user');
      }
    } catch (error) {
      console.error('Failed to update user:', error);
      setEditError('Failed to update user');
    } finally {
      setIsEditing(false);
    }
  };

  const handleToggleUserStatus = (user: User) => {
    // Prevent deactivating protected admin accounts
    if (isProtectedAdmin(user) && user.isActive) {
      const message = currentUser && user.id === currentUser.id 
        ? 'You cannot deactivate your own account'
        : 'Cannot deactivate admin accounts';
      
      toast.error(message);
      return;
    }

    setUserToToggle(user);
    setShowToggleConfirm(true);
  };

  const confirmToggleUserStatus = async () => {
    if (!userToToggle) return;

    const newStatus = !userToToggle.isActive;
    const action = newStatus ? 'activate' : 'deactivate';

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/users/${userToToggle.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({
          email: userToToggle.email,
          role: userToToggle.role,
          firstName: userToToggle.firstName || '',
          lastName: userToToggle.lastName || '',
          isActive: newStatus
        }),
      });

      if (response.ok) {
        toast.success(`User ${action}d successfully`);
        loadUsers();
      } else {
        const errorData = await response.json();
        toast.error(errorData.message || `Failed to ${action} user`);
      }
    } catch (error) {
      console.error(`Failed to ${action} user:`, error);
      toast.error(
`Failed to ${action} user`,
);
    } finally {
      setShowToggleConfirm(false);
      setUserToToggle(null);
    }
  };

  const handlePasswordReset = (user: User) => {
    setSelectedUser(user);
    setNewPassword('');
    setShowPasswordResetModal(true);
  };

  const handleResetPassword = async () => {
    if (!selectedUser || !newPassword) {
      return;
    }

    if (newPassword.length < 6) {
      toast.error(
'Password must be at least 6 characters long',
);
      return;
    }

    setIsResettingPassword(true);

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/users/${selectedUser.id}/reset-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({ newPassword }),
      });

      if (response.ok) {
        toast.success('Password reset successfully');
        setShowPasswordResetModal(false);
        setNewPassword('');
      } else {
        const errorData = await response.json();
        toast.error(errorData.message || 'Failed to reset password');
      }
    } catch (error) {
      console.error('Failed to reset password:', error);
      toast.error(
'Failed to reset password',
);
    } finally {
      setIsResettingPassword(false);
    }
  };

  const loadMoreClients = useCallback(async () => {
    if (isLoadingMoreClients || !hasMoreClients) return;
    
    console.log('Loading more clients...', { currentPage: clientsPage, hasMore: hasMoreClients });
    setIsLoadingMoreClients(true);

    try {
      const token = localStorage.getItem('accessToken');
      const nextPage = clientsPage + 1;
      const response = await fetch(`/api/clients?page=${nextPage}&limit=100`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const clientData = await response.json();
        const newClients = clientData.clients || [];
        console.log(`Got ${newClients.length} new clients for page ${nextPage}`);
        
        if (newClients.length > 0) {
          setUserClients(prev => [...prev, ...newClients]);
          setClientsPage(nextPage);
          // If we got less than 100, we're done
          setHasMoreClients(newClients.length === 100);
        } else {
          setHasMoreClients(false);
        }
      } else {
        console.error('API Error:', response.status, response.statusText);
        setHasMoreClients(false);
      }
    } catch (error) {
      console.error('Failed to load more clients:', error);
      setHasMoreClients(false);
    } finally {
      setIsLoadingMoreClients(false);
    }
  }, [clientsPage, hasMoreClients, isLoadingMoreClients]);

  const loadInitialClients = useCallback(async () => {
    setIsLoadingUserClients(true);
    setUserClients([]);
    setClientsPage(1);
    setHasMoreClients(true);
    setSearchQuery('');

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/clients?page=1&limit=100`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const clientData = await response.json();
        const newClients = clientData.clients || [];
        const total = clientData.total || clientData.totalCount || 0;
        
        console.log('Initial load:', { clients: newClients.length, total });
        
        setUserClients(newClients);
        setTotalClientsCount(total);
        setHasMoreClients(newClients.length === 100);
      }
    } catch (error) {
      console.error('Failed to load clients:', error);
      toast.error('Failed to load clients');
    } finally {
      setIsLoadingUserClients(false);
    }
  }, []);

  // Filter clients based on search query
  useEffect(() => {
    if (!searchQuery.trim()) {
      setFilteredClients(userClients);
    } else {
      const filtered = userClients.filter(client =>
        client.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        client.id.toLowerCase().includes(searchQuery.toLowerCase())
      );
      setFilteredClients(filtered);
    }
  }, [userClients, searchQuery]);

  const handleViewUserClients = async (user: User) => {
    setSelectedUser(user);
    setShowViewClientsModal(true);
    
    // Set initial temporary assignments based on user's current assignments
    setTempAssignedClients(user.assignedClientIds || []);

    // Load first page
    await loadInitialClients();
  };

  const toggleTempClientAssignment = (clientId: string) => {
    setTempAssignedClients(prev =>
      prev.includes(clientId)
        ? prev.filter(id => id !== clientId)
        : [...prev, clientId]
    );
  };

  const handleSaveClientAssignments = async () => {
    if (!selectedUser) return;

    setIsSavingClientAssignments(true);
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(`/api/users/${selectedUser.id}/clients`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(tempAssignedClients),
      });

      if (response.ok) {
        toast.success('Client assignments updated successfully');
        setShowViewClientsModal(false);
        loadUsers(); // Reload users to reflect the changes
      } else {
        toast.error('Failed to update client assignments');
      }
    } catch (error) {
      console.error('Failed to update client assignments:', error);
      toast.error(
'Failed to update client assignments',
);
    } finally {
      setIsSavingClientAssignments(false);
    }
  };

  const handleViewUserSessions = async (user: User) => {
    setSelectedUser(user);
    setShowSessionsModal(true);
    setIsLoadingUserSessions(true);
    setUserSessions([]);

    try {
      const sessionsData = await apiClient.get(`/api/auth/user-sessions/${user.id}`) as UserSession[];
      setUserSessions(sessionsData);
    } catch (error) {
      handleApiErrorWithToast(error, 'load user sessions', toast);
    } finally {
      setIsLoadingUserSessions(false);
    }
  };

  const handleRevokeUserSession = async (sessionId: string) => {
    if (!selectedUser) return;

    try {
      await apiClient.delete(`/api/auth/admin/users/${selectedUser.id}/sessions/${sessionId}`);
      toast.success('Session revoked successfully');
      // Refresh the sessions list
      handleViewUserSessions(selectedUser);
    } catch (error) {
      handleApiErrorWithToast(error, 'revoke session', toast);
    }
  };

  const handleRevokeAllUserSessions = async () => {
    if (!selectedUser) return;

    try {
      await apiClient.delete(`/api/auth/admin/users/${selectedUser.id}/sessions`);
      toast.success('All sessions revoked successfully');
      // Refresh the sessions list
      handleViewUserSessions(selectedUser);
    } catch (error) {
      handleApiErrorWithToast(error, 'revoke all sessions', toast);
    }
  };

  const getRoleBadge = (role: string) => {
    return role === 'Admin' ? (
      <Badge variant="secondary" className="gap-1 bg-amber-100 text-amber-800 hover:bg-amber-100">
        <Shield className="h-3 w-3" />
        Admin
      </Badge>
    ) : (
      <Badge variant="secondary" className="gap-1 bg-gray-100 text-gray-700 hover:bg-gray-100">
        <User className="h-3 w-3" />
        User
      </Badge>
    );
  };

  const getStatusBadge = (isActive: boolean) => {
    return isActive ? (
      <Badge variant="default" className="gap-1 bg-green-500 hover:bg-green-600">
        <CheckCircle className="h-3 w-3" />
        Active
      </Badge>
    ) : (
      <Badge variant="destructive" className="gap-1">
        <X className="h-3 w-3" />
        Inactive
      </Badge>
    );
  };

  const getDeviceIcon = (deviceName: string | null) => {
    if (!deviceName) return Activity;
    
    const device = deviceName.toLowerCase();
    
    if (device.includes('windows')) {
      return Activity; // Windows PC
    } else if (device.includes('mac')) {
      return Laptop; // Mac
    } else if (device.includes('linux')) {
      return Activity; // Linux PC
    } else if (device.includes('iphone')) {
      return Smartphone; // iPhone
    } else if (device.includes('ipad')) {
      return TabletSmartphone; // iPad
    } else if (device.includes('android')) {
      return Smartphone; // Android Device
    }
    
    return Activity; // Default for unknown devices
  };

  const getDeviceIconColor = (deviceName: string | null) => {
    if (!deviceName) return 'text-gray-500';
    
    const device = deviceName.toLowerCase();
    
    if (device.includes('windows')) {
      return 'text-blue-500'; // Windows blue
    } else if (device.includes('mac')) {
      return 'text-gray-600'; // Mac gray
    } else if (device.includes('linux')) {
      return 'text-orange-500'; // Linux orange
    } else if (device.includes('iphone')) {
      return 'text-blue-500'; // iPhone blue
    } else if (device.includes('ipad')) {
      return 'text-blue-500'; // iPad blue
    } else if (device.includes('android')) {
      return 'text-green-500'; // Android green
    }
    
    return 'text-gray-500'; // Default color
  };

  // Check if user is a protected admin that cannot be deleted or modified
  // Protected admins are either the current user (can't delete yourself) or the first admin in the system
  const isProtectedAdmin = (user: User) => {
    // Users cannot delete themselves
    if (currentUser && user.id === currentUser.id) {
      return true;
    }
    
    // For now, we'll protect any admin user to maintain system integrity
    // In the future, this could be extended to check for a specific "super admin" role
    // or the first created admin user in the system
    return user.role === 'Admin';
  };

  const formatLastActive = (lastLoginAt?: string) => {
    if (!lastLoginAt) {
      return (
        <span className="text-sm text-muted-foreground">Never</span>
      );
    }

    const lastLogin = new Date(lastLoginAt);
    const now = new Date();
    const diffMs = now.getTime() - lastLogin.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    let timeAgo = '';
    let colorClass = 'text-muted-foreground';

    if (diffMins < 5) {
      timeAgo = 'Just now';
      colorClass = 'text-green-600';
    } else if (diffMins < 60) {
      timeAgo = `${diffMins}m ago`;
      colorClass = 'text-green-600';
    } else if (diffHours < 24) {
      timeAgo = `${diffHours}h ago`;
      colorClass = diffHours < 2 ? 'text-green-600' : 'text-amber-600';
    } else if (diffDays < 7) {
      timeAgo = `${diffDays}d ago`;
      colorClass = 'text-amber-600';
    } else {
      timeAgo = lastLogin.toLocaleDateString();
      colorClass = 'text-muted-foreground';
    }

    return (
      <div className="space-y-1">
        <div className={`text-sm font-medium ${colorClass}`}>{timeAgo}</div>
        <div className="text-xs text-muted-foreground">
          {lastLogin.toLocaleString()}
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <ProtectedRoute requireAdmin={true}>
        <div className="flex h-full flex-col bg-background/95">
          <div className="space-y-2 p-4">
            <Skeleton className="h-8 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>

          <div className="flex-1 overflow-auto p-4">
            <Card className="bg-card/30 backdrop-blur-sm border-border/40 shadow-sm">
              <CardHeader>
                <Skeleton className="h-6 w-32" />
                <Skeleton className="h-4 w-64" />
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="min-w-[200px]">User</TableHead>
                      <TableHead className="hidden sm:table-cell">Role</TableHead>
                      <TableHead className="hidden md:table-cell">Active</TableHead>
                      <TableHead className="hidden lg:table-cell">Clients</TableHead>
                      <TableHead className="hidden xl:table-cell">Last Active</TableHead>
                      <TableHead className="w-[120px]">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {[...Array(5)].map((_, i) => (
                      <TableRow key={i}>
                        <TableCell className="min-w-[200px]">
                          <div className="flex items-center gap-3">
                            <div className="flex h-8 w-8 sm:h-10 sm:w-10 items-center justify-center rounded-lg flex-shrink-0">
                              <Skeleton className="h-full w-full rounded-lg" />
                            </div>
                            <div className="min-w-0 flex-1 space-y-2">
                              <Skeleton className="h-4 w-48" />
                              <Skeleton className="h-3 w-32" />
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="hidden sm:table-cell">
                          <Skeleton className="h-6 w-16 rounded-full" />
                        </TableCell>
                        <TableCell className="hidden md:table-cell">
                          <Skeleton className="h-5 w-10 rounded-full" />
                        </TableCell>
                        <TableCell className="hidden lg:table-cell">
                          <Skeleton className="h-4 w-12" />
                        </TableCell>
                        <TableCell className="hidden xl:table-cell">
                          <div className="space-y-1">
                            <Skeleton className="h-4 w-16" />
                            <Skeleton className="h-3 w-24" />
                          </div>
                        </TableCell>
                        <TableCell className="w-[120px]">
                          <div className="flex items-center gap-1">
                            <Skeleton className="h-8 w-8 rounded" />
                            <Skeleton className="h-8 w-8 rounded" />
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          </div>
        </div>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute requireAdmin={true}>
      <div className="flex h-full flex-col bg-background/95">
        <PageHeader 
          title="User Management"
          description="Manage system users and their access permissions"
          mobileDescription="User management"
          icon={Users}
          actions={
            <Button size="sm" onClick={() => setShowCreateModal(true)} className="h-8 w-8 sm:w-auto p-0 sm:px-3">
              <Plus className="h-4 w-4 sm:mr-2" />
              <span className="hidden sm:inline">Add User</span>
            </Button>
          }
        />

        <div className="flex-1 overflow-auto p-4">
          <Card className="bg-card/30 backdrop-blur-sm border-border/40 shadow-sm">
            <CardHeader>
              <CardTitle>System Users</CardTitle>
              <CardDescription>
                Manage user accounts and their roles in the system
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="min-w-[200px]">User</TableHead>
                      <TableHead className="hidden sm:table-cell">Role</TableHead>
                      <TableHead className="hidden md:table-cell">Active</TableHead>
                      <TableHead className="hidden lg:table-cell">Clients</TableHead>
                      <TableHead className="hidden xl:table-cell">Last Active</TableHead>
                      <TableHead className="w-[120px]">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {users.map((user) => (
                      <TableRow key={user.id}>
                        <TableCell className="min-w-[200px]">
                          <div className="flex items-center gap-3">
                            <div className={`flex h-8 w-8 sm:h-10 sm:w-10 items-center justify-center rounded-lg flex-shrink-0 ${
                              user.role === 'Admin' 
                                ? 'bg-secondary border' 
                                : 'bg-primary/10'
                            }`}>
                              {user.role === 'Admin' ? (
                                <Shield className="h-4 w-4 sm:h-5 sm:w-5 text-secondary-foreground" />
                              ) : (
                                <Mail className="h-4 w-4 sm:h-5 sm:w-5 text-primary" />
                              )}
                            </div>
                            <div className="min-w-0 flex-1">
                              <div className="text-sm font-medium truncate">
                                {user.email}
                              </div>
                              {(user.firstName || user.lastName) && (
                                <div className="text-xs text-muted-foreground truncate">
                                  {user.firstName} {user.lastName}
                                </div>
                              )}
                              <div className="sm:hidden flex flex-wrap gap-1 mt-2">
                                {getRoleBadge(user.role)}
                                {getStatusBadge(user.isActive)}
                              </div>
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="hidden sm:table-cell">{getRoleBadge(user.role)}</TableCell>
                        <TableCell className="hidden md:table-cell">
                          <Switch
                            checked={user.isActive}
                            onCheckedChange={() => handleToggleUserStatus(user)}
                            disabled={isProtectedAdmin(user) && user.isActive}
                            aria-label={`Toggle ${user.email} status`}
                          />
                        </TableCell>
                        <TableCell className="hidden lg:table-cell">
                          {user.role === 'Admin' ? (
                            <Badge variant="secondary" className="gap-1 bg-amber-100 text-amber-800 hover:bg-amber-100">
                              <Shield className="h-3 w-3" />
                              All clients
                            </Badge>
                          ) : (
                            <Badge 
                              variant="outline" 
                              className="gap-1 cursor-pointer hover:bg-accent"
                              onClick={() => handleViewUserClients(user)}
                            >
                              <Users className="h-3 w-3" />
                              {user.assignedClientIds && user.assignedClientIds.length > 0 ? (
                                <>
                                  {user.assignedClientIds.length} client{user.assignedClientIds.length !== 1 ? 's' : ''}
                                </>
                              ) : (
                                'No clients'
                              )}
                            </Badge>
                          )}
                        </TableCell>
                        <TableCell className="hidden xl:table-cell">{formatLastActive(user.lastLoginAt)}</TableCell>
                        <TableCell className="w-[120px]">
                          <div className="flex items-center gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleViewUserSessions(user)}
                              className="h-8 w-8 text-green-600 hover:text-green-700 hover:bg-green-50"
                              title="View sessions"
                            >
                              <Activity className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleEditUser(user)}
                              className="h-8 w-8 text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                              title="Edit user"
                            >
                              <PenSquare className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handlePasswordReset(user)}
                              className="h-8 w-8 text-purple-600 hover:text-purple-700 hover:bg-purple-50 hidden sm:flex"
                              title="Reset password"
                            >
                              <Key className="h-4 w-4" />
                            </Button>
                            {!isProtectedAdmin(user) && (
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleDeleteUser(user)}
                                className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10 hidden sm:flex"
                                title="Delete user"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            )}
                            {/* Mobile dropdown menu */}
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  className="h-8 w-8 sm:hidden"
                                >
                                  <EllipsisVertical className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end" className="w-48">
                                <DropdownMenuItem onClick={() => handleToggleUserStatus(user)} disabled={isProtectedAdmin(user) && user.isActive}>
                                  {user.isActive ? <X className="h-4 w-4 mr-2" /> : <CheckCircle className="h-4 w-4 mr-2" />}
                                  {user.isActive ? 'Deactivate' : 'Activate'} User
                                </DropdownMenuItem>
                                {user.role !== 'Admin' && (
                                  <DropdownMenuItem onClick={() => handleViewUserClients(user)}>
                                    <Eye className="h-4 w-4 mr-2" />
                                    View Clients
                                  </DropdownMenuItem>
                                )}
                                <DropdownMenuItem onClick={() => handlePasswordReset(user)}>
                                  <Key className="h-4 w-4 mr-2" />
                                  Reset Password
                                </DropdownMenuItem>
                                {!isProtectedAdmin(user) && (
                                  <DropdownMenuItem 
                                    onClick={() => handleDeleteUser(user)}
                                    className="text-destructive focus:text-destructive"
                                  >
                                    <Trash2 className="h-4 w-4 mr-2" />
                                    Delete User
                                  </DropdownMenuItem>
                                )}
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
            </CardContent>
          </Card>
        </div>

        {/* Create User Modal */}
        <Dialog open={showCreateModal} onOpenChange={setShowCreateModal}>
          <DialogContent className="mx-4 sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Create New User</DialogTitle>
              <DialogDescription>
                Add a new user to the system with specified role and permissions.
              </DialogDescription>
            </DialogHeader>
            
            <div className="space-y-4">
              {createError && (
                <Alert variant="destructive">
                  <AlertCircle />
                  <AlertTitle>User Creation Failed</AlertTitle>
                  <AlertDescription>{createError}</AlertDescription>
                </Alert>
              )}
              
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="firstName">First Name</Label>
                  <Input
                    id="firstName"
                    value={createUserData.firstName}
                    onChange={(e) => setCreateUserData({ ...createUserData, firstName: e.target.value })}
                    placeholder="John"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last Name</Label>
                  <Input
                    id="lastName"
                    value={createUserData.lastName}
                    onChange={(e) => setCreateUserData({ ...createUserData, lastName: e.target.value })}
                    placeholder="Doe"
                  />
                </div>
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="email">Email *</Label>
                <Input
                  id="email"
                  type="email"
                  value={createUserData.email}
                  onChange={(e) => setCreateUserData({ ...createUserData, email: e.target.value })}
                  placeholder="user@example.com"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="password">Password *</Label>
                <Input
                  id="password"
                  type="password"
                  value={createUserData.password}
                  onChange={(e) => setCreateUserData({ ...createUserData, password: e.target.value })}
                  placeholder="Enter password"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="role">Role</Label>
                <Select value={createUserData.role} onValueChange={(value) => setCreateUserData({ ...createUserData, role: value })}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="User">User</SelectItem>
                    <SelectItem value="Admin">Admin</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setShowCreateModal(false)}>
                Cancel
              </Button>
              <Button onClick={handleCreateUser} disabled={isCreating}>
                {isCreating ? (
                  <>
                    <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Creating...
                  </>
                ) : (
                  'Create User'
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Assign Clients Modal */}
        <Dialog open={showAssignModal} onOpenChange={setShowAssignModal}>
          <DialogContent className="mx-4 sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Assign Clients</DialogTitle>
              <DialogDescription>
                Select clients that {selectedUser?.email} can access.
              </DialogDescription>
            </DialogHeader>
            
            <div className="space-y-4">
              <div className="max-h-60 overflow-y-auto space-y-2">
                {Array.isArray(clients) && clients.map((client) => (
                  <div key={client.id} className="flex items-center gap-3 p-3 border rounded-lg hover:bg-muted/50 transition-colors">
                    <Checkbox
                      id={`client-${client.id}`}
                      checked={assignedClients.includes(client.id)}
                      onCheckedChange={() => toggleClientAssignment(client.id)}
                    />
                    <div className="flex h-6 w-6 items-center justify-center rounded-md bg-primary/10 flex-shrink-0">
                      <Building className="h-3 w-3 text-primary" />
                    </div>
                    <Label htmlFor={`client-${client.id}`} className="flex-1 cursor-pointer text-sm font-medium">
                      {client.name}
                    </Label>
                  </div>
                ))}
                {(!Array.isArray(clients) || clients.length === 0) && (
                  <div className="text-center py-8">
                    <Building className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                    <p className="text-sm text-muted-foreground">No clients available</p>
                  </div>
                )}
              </div>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setShowAssignModal(false)}>
                Cancel
              </Button>
              <Button onClick={handleUpdateUserClients} disabled={isUpdating}>
                {isUpdating ? (
                  <>
                    <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Updating...
                  </>
                ) : (
                  'Update Assignments'
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Edit User Modal */}
        <Dialog open={showEditModal} onOpenChange={setShowEditModal}>
          <DialogContent className="mx-4 sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Edit User</DialogTitle>
              <DialogDescription>
                Update user information and settings.
              </DialogDescription>
            </DialogHeader>
            
            <div className="space-y-4">
              {editError && (
                <Alert variant="destructive">
                  <AlertCircle />
                  <AlertTitle>User Update Failed</AlertTitle>
                  <AlertDescription>{editError}</AlertDescription>
                </Alert>
              )}
              
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="editFirstName">First Name</Label>
                  <Input
                    id="editFirstName"
                    value={editUserData.firstName}
                    onChange={(e) => setEditUserData({ ...editUserData, firstName: e.target.value })}
                    placeholder="John"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="editLastName">Last Name</Label>
                  <Input
                    id="editLastName"
                    value={editUserData.lastName}
                    onChange={(e) => setEditUserData({ ...editUserData, lastName: e.target.value })}
                    placeholder="Doe"
                  />
                </div>
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="editEmail">Email *</Label>
                <Input
                  id="editEmail"
                  type="email"
                  value={editUserData.email}
                  onChange={(e) => setEditUserData({ ...editUserData, email: e.target.value })}
                  placeholder="user@example.com"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="editRole">Role</Label>
                <div className="relative">
                  <Select
                    value={editUserData.role}
                    onValueChange={(value) => setEditUserData({ ...editUserData, role: value })}
                    disabled={selectedUser ? isProtectedAdmin(selectedUser) : false}
                  >
                    <SelectTrigger id="editRole" className="w-full">
                      <SelectValue placeholder="Select role" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="User">User</SelectItem>
                      <SelectItem value="Admin">Admin</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                {selectedUser && isProtectedAdmin(selectedUser) && (
                  <p className="text-xs text-muted-foreground">
                    {currentUser && selectedUser.id === currentUser.id
                      ? 'Cannot change your own role'
                      : 'Cannot change role of admin accounts'
                    }
                  </p>
                )}
              </div>

            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setShowEditModal(false)}>
                Cancel
              </Button>
              <Button onClick={handleUpdateUser} disabled={isEditing}>
                {isEditing ? (
                  <>
                    <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Updating...
                  </>
                ) : (
                  'Update User'
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Password Reset Modal */}
        <Dialog open={showPasswordResetModal} onOpenChange={setShowPasswordResetModal}>
          <DialogContent className="mx-4 sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Reset Password</DialogTitle>
              <DialogDescription>
                Set a new password for {selectedUser?.email}.
              </DialogDescription>
            </DialogHeader>
            
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="newPassword">New Password</Label>
                <Input
                  id="newPassword"
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="Enter new password"
                  required
                />
                <p className="text-xs text-muted-foreground">
                  Password must be at least 6 characters long
                </p>
              </div>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setShowPasswordResetModal(false)}>
                Cancel
              </Button>
              <Button 
                onClick={handleResetPassword} 
                disabled={isResettingPassword || !newPassword || newPassword.length < 6}
              >
                {isResettingPassword ? (
                  <>
                    <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Resetting...
                  </>
                ) : (
                  'Reset Password'
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* View User Clients Modal */}
        <Dialog open={showViewClientsModal} onOpenChange={setShowViewClientsModal}>
          <DialogContent className="w-[95vw] max-w-2xl h-[85vh] max-h-[600px] flex flex-col p-0">
            <div className="flex-shrink-0 px-4 py-3 border-b">
              <DialogHeader className="space-y-1">
                <DialogTitle className="flex items-center gap-2 text-base">
                  <Settings className="h-4 w-4" />
                  Manage Client Access
                </DialogTitle>
                <DialogDescription className="text-sm">
                  Select which clients {selectedUser?.email} can access
                  {totalClientsCount > 0 && ` (${totalClientsCount} clients available)`}
                </DialogDescription>
              </DialogHeader>
            </div>
            
            <div className="flex-1 min-h-0 px-4">
              {isLoadingUserClients ? (
                <div className="flex items-center justify-center py-12">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                  <span className="ml-2 text-sm text-muted-foreground">Loading clients...</span>
                </div>
              ) : userClients.length > 0 ? (
                <div className="space-y-2 h-full flex flex-col">
                  {/* Search Bar */}
                  <div className="relative">
                    <Input
                      placeholder="Search clients by name or ID..."
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      className="h-8 text-sm pr-8"
                    />
                    {searchQuery && (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setSearchQuery('')}
                        className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6 text-muted-foreground hover:text-foreground"
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                  
                  {/* Stats and Controls */}
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 -mt-1">
                    <div className="text-sm text-muted-foreground order-2 sm:order-1">
                      {searchQuery ? (
                        <>{tempAssignedClients.length} selected, {filteredClients.length} showing (of {userClients.length} loaded)</>
                      ) : (
                        <>{tempAssignedClients.length} of {userClients.length} clients selected{hasMoreClients && ' (more available)'}</>
                      )}
                    </div>
                    <div className="flex gap-2 order-1 sm:order-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setTempAssignedClients([])}
                        disabled={tempAssignedClients.length === 0}
                        className="text-xs px-2 h-7"
                      >
                        Clear All
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          const clientsToSelect = searchQuery ? filteredClients.map(c => c.id) : userClients.map(c => c.id);
                          setTempAssignedClients(prev => {
                            const uniqueSet = new Set([...prev, ...clientsToSelect]);
                            const newSelection = Array.from(uniqueSet);
                            return newSelection;
                          });
                        }}
                        disabled={isLoadingUserClients}
                        className="text-xs px-2 h-7"
                      >
                        {searchQuery ? 'Select Filtered' : 'Select All Loaded'}
                      </Button>
                    </div>
                  </div>
                  <div 
                    ref={clientsScrollRef}
                    className="flex-1 overflow-y-auto border rounded-md bg-background"
                    onScroll={(e) => {
                      const { scrollTop, scrollHeight, clientHeight } = e.currentTarget;
                      const scrollPercentage = (scrollTop + clientHeight) / scrollHeight;
                      
                      // Only trigger load more if not searching (we want to load more actual data)
                      if (!searchQuery && scrollPercentage > 0.8 && hasMoreClients && !isLoadingMoreClients) {
                        console.log('Scroll trigger:', { scrollPercentage, hasMore: hasMoreClients, loading: isLoadingMoreClients });
                        loadMoreClients();
                      }
                    }}
                  >
                    <div className="p-1">
                      {filteredClients.map((client) => (
                        <div key={client.id} className="flex items-center gap-2 p-2 rounded-md hover:bg-muted/50 transition-colors group">
                          <Checkbox
                            id={`temp-client-${client.id}`}
                            checked={tempAssignedClients.includes(client.id)}
                            onCheckedChange={() => toggleTempClientAssignment(client.id)}
                            className="flex-shrink-0"
                          />
                          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 flex-shrink-0 group-hover:bg-primary/20 transition-colors">
                            <Building className="h-4 w-4 text-primary" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <Label 
                              htmlFor={`temp-client-${client.id}`} 
                              className="text-sm font-medium cursor-pointer block truncate leading-4"
                            >
                              {client.name}
                            </Label>
                            <div className="text-xs text-muted-foreground truncate">
                              ID: {client.id}
                            </div>
                          </div>
                        </div>
                      ))}
                      {!searchQuery && isLoadingMoreClients && (
                        <div className="flex items-center justify-center py-3">
                          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-primary"></div>
                          <span className="ml-2 text-sm text-muted-foreground">Loading more clients...</span>
                        </div>
                      )}
                      {!searchQuery && !hasMoreClients && userClients.length > 0 && (
                        <div className="text-center py-1 text-xs text-muted-foreground border-t mt-1 pt-1">
                          All clients loaded ({userClients.length} total)
                        </div>
                      )}
                      {searchQuery && filteredClients.length === 0 && userClients.length > 0 && (
                        <div className="text-center py-6">
                          <Building className="h-10 w-10 text-muted-foreground/50 mx-auto mb-2" />
                          <p className="text-sm font-medium text-muted-foreground">No clients found</p>
                          <p className="text-xs text-muted-foreground mt-1">Try adjusting your search query</p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ) : (
                <div className="text-center py-12">
                  <Building className="h-12 w-12 text-muted-foreground/50 mx-auto mb-3" />
                  <p className="text-sm font-medium text-muted-foreground">No clients available</p>
                  <p className="text-xs text-muted-foreground mt-1">There are currently no clients in the system to assign.</p>
                </div>
              )}
            </div>

            <div className="flex-shrink-0 border-t px-4 pt-3 pb-3">
              <DialogFooter className="gap-2">
                <Button 
                  variant="outline" 
                  onClick={() => setShowViewClientsModal(false)}
                  className="min-w-[80px]"
                >
                  Cancel
                </Button>
                <Button 
                  onClick={handleSaveClientAssignments} 
                  disabled={isSavingClientAssignments}
                  className="min-w-[100px]"
                >
                  {isSavingClientAssignments ? (
                    <>
                      <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      Saving...
                    </>
                  ) : (
                    'Save Changes'
                  )}
                </Button>
              </DialogFooter>
            </div>
          </DialogContent>
        </Dialog>

        {/* View User Sessions Modal */}
        <Dialog open={showSessionsModal} onOpenChange={setShowSessionsModal}>
          <DialogContent className="mx-4 sm:max-w-2xl">
            <DialogHeader>
              <DialogTitle>User Sessions</DialogTitle>
              <DialogDescription>
                Active sessions for {selectedUser?.email}
              </DialogDescription>
            </DialogHeader>
            
            <div className="space-y-4">
              {isLoadingUserSessions ? (
                <div className="flex items-center justify-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                  <span className="ml-2 text-sm text-muted-foreground">Loading sessions...</span>
                </div>
              ) : userSessions.length > 0 ? (
                <div className="space-y-3 max-h-96 overflow-y-auto">
                  {userSessions.map((session) => {
                    const DeviceIcon = getDeviceIcon(session.deviceName);
                    const iconColor = getDeviceIconColor(session.deviceName);
                    
                    return (
                      <div key={session.id} className="border rounded-lg p-4 space-y-3 hover:bg-muted/50 transition-colors">
                        <div className="flex items-start justify-between">
                          <div className="flex items-start gap-3 flex-1 min-w-0">
                            <div className="w-10 h-10 bg-primary/10 rounded-lg flex items-center justify-center flex-shrink-0">
                              <DeviceIcon className={`h-5 w-5 ${iconColor}`} />
                            </div>
                            <div className="space-y-2 min-w-0 flex-1">
                              <div className="text-sm font-medium truncate">
                                {session.deviceName || 'Unknown Device'}
                              </div>
                              <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4 text-xs text-muted-foreground">
                                <div className="flex items-center gap-1">
                                  <Globe className="h-3 w-3 flex-shrink-0" />
                                  <span className="truncate">{session.ipAddress || 'Unknown IP'}</span>
                                </div>
                                <div className="flex items-center gap-1">
                                  <Clock className="h-3 w-3 flex-shrink-0" />
                                  <span>Last active: {new Date(session.lastAccessedAt).toLocaleString()}</span>
                                </div>
                              </div>
                              <div className="text-xs text-muted-foreground">
                                Created: {new Date(session.createdAt).toLocaleString()}
                              </div>
                            </div>
                          </div>
                          <div className="flex items-center gap-2 flex-shrink-0">
                            {new Date(session.lastAccessedAt).getTime() > Date.now() - 3600000 && (
                              <Badge variant="default" className="bg-green-500 hover:bg-green-600 text-xs">
                                Active
                              </Badge>
                            )}
                            {selectedUser && !isProtectedAdmin(selectedUser) && (
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleRevokeUserSession(session.id)}
                                className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                                title="Revoke session"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              ) : (
                <div className="text-center py-8">
                  <Activity className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <p className="text-sm font-medium text-muted-foreground">No active sessions</p>
                  <p className="text-xs text-muted-foreground mt-2">
                    This user hasn't logged in recently or has no active sessions
                  </p>
                </div>
              )}
            </div>

            <DialogFooter className="flex justify-between">
              <div>
                {selectedUser && !isProtectedAdmin(selectedUser) && userSessions.length > 0 && (
                  <Button 
                    variant="destructive" 
                    onClick={handleRevokeAllUserSessions}
                    className="gap-2"
                  >
                    <Trash2 className="h-4 w-4" />
                    Revoke All Sessions
                  </Button>
                )}
              </div>
              <Button variant="outline" onClick={() => setShowSessionsModal(false)}>
                Close
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Toggle User Status Confirmation */}
        <AlertDialog open={showToggleConfirm} onOpenChange={setShowToggleConfirm}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>
                {userToToggle?.isActive ? 'Deactivate' : 'Activate'} User
              </AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to {userToToggle?.isActive ? 'deactivate' : 'activate'} user "{userToToggle?.email}"?
                {userToToggle?.isActive && (
                  <span className="block mt-2 text-amber-600">
                    This user will no longer be able to access the system.
                  </span>
                )}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={() => {
                setShowToggleConfirm(false);
                setUserToToggle(null);
              }}>
                Cancel
              </AlertDialogCancel>
              <AlertDialogAction onClick={confirmToggleUserStatus}>
                {userToToggle?.isActive ? 'Deactivate' : 'Activate'}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>

        {/* Delete User Confirmation */}
        <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle className="text-destructive">Delete User</AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to delete user "{userToDelete?.email}"?
                <span className="block mt-2 text-destructive font-medium">
                  This action cannot be undone. The user will be permanently removed from the system.
                </span>
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={() => {
                setShowDeleteConfirm(false);
                setUserToDelete(null);
              }}>
                Cancel
              </AlertDialogCancel>
              <AlertDialogAction
                onClick={confirmDeleteUser}
                className="bg-destructive hover:bg-destructive/90 focus:ring-destructive"
              >
                Delete User
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </ProtectedRoute>
  );
}