'use client';

import React, { useState, useEffect } from 'react';
import { ProtectedRoute } from '@/components/protected-route';
import { PageHeader } from '@/components/page-header';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { toast } from 'sonner';
import { usePageTitle } from '@/hooks/use-page-title';
import { useAuth } from '@/contexts/auth-context';
import { Settings, Lock, Key, Eye, EyeOff, Copy, RefreshCw, AlertCircle, CheckCircle } from 'lucide-react';

interface ChangePasswordData {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export default function SettingsPage() {
  usePageTitle('Settings - LeadHype');
  const { user, refreshUser } = useAuth();
  const [passwordData, setPasswordData] = useState<ChangePasswordData>({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [passwordError, setPasswordError] = useState('');
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [apiKey, setApiKey] = useState<string>('');
  const [apiKeyCreatedAt, setApiKeyCreatedAt] = useState<Date | null>(null);
  const [isGeneratingKey, setIsGeneratingKey] = useState(false);

  useEffect(() => {
    // Load API key from user context
    if (user) {
      setApiKey(user.apiKey || '');
      setApiKeyCreatedAt(user.apiKeyCreatedAt ? new Date(user.apiKeyCreatedAt) : null);
    }
  }, [user]);

  const handlePasswordChange = async () => {
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setPasswordError('New passwords do not match');
      return;
    }

    if (passwordData.newPassword.length < 6) {
      setPasswordError('New password must be at least 6 characters long');
      return;
    }

    setIsChangingPassword(true);
    setPasswordError('');

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch('/api/users/change-password', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({
          currentPassword: passwordData.currentPassword,
          newPassword: passwordData.newPassword
        }),
      });

      if (response.ok) {
        toast.success('Password changed successfully');
        setPasswordData({
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        });
      } else {
        const errorData = await response.json();
        setPasswordError(errorData.message || 'Failed to change password');
      }
    } catch (error) {
      console.error('Failed to change password:', error);
      setPasswordError('Failed to change password');
    } finally {
      setIsChangingPassword(false);
    }
  };

  const handleGenerateApiKey = async () => {
    setIsGeneratingKey(true);

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch('/api/users/generate-api-key', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        
        // Directly set the API key from the response
        if (data && data.apiKey) {
          setApiKey(data.apiKey);
          setApiKeyCreatedAt(new Date());
          
          // Refresh user data in context to keep everything in sync
          await refreshUser();
          
          toast.success('API key generated successfully. Make sure to copy and save it securely.');
        } else {
          toast.error('Failed to generate API key properly');
        }
      } else {
        toast.error('Failed to generate API key');
      }
    } catch (error) {
      console.error('Failed to generate API key:', error);
      toast.error('Failed to generate API key');
    } finally {
      setIsGeneratingKey(false);
    }
  };


  const copyApiKey = async () => {
    if (!apiKey) return;
    
    try {
      // Check if Clipboard API is available
      if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
        await navigator.clipboard.writeText(apiKey);
        toast.success('API key copied to clipboard');
      } else {
        // Fallback for browsers that don't support Clipboard API
        const textArea = document.createElement('textarea');
        textArea.value = apiKey;
        textArea.style.position = 'fixed';
        textArea.style.top = '-9999px';
        textArea.style.left = '-9999px';
        document.body.appendChild(textArea);
        textArea.select();
        textArea.setSelectionRange(0, 99999); // For mobile devices
        
        try {
          const successful = document.execCommand('copy');
          if (successful) {
            toast.success('API key copied to clipboard');
          } else {
            throw new Error('Copy command failed');
          }
        } finally {
          document.body.removeChild(textArea);
        }
      }
    } catch (error) {
      console.error('Failed to copy API key:', error);
      toast.error('Failed to copy API key to clipboard. Please copy manually.');
    }
  };

  return (
    <ProtectedRoute>
      <div className="flex h-full flex-col">
        <PageHeader 
          title="Account Settings"
          description="Manage your account preferences and security settings"
          mobileDescription="Account settings"
          icon={Settings}
        />

        <div className="flex-1 p-3 sm:p-4 space-y-4 sm:space-y-6">
          {/* Password Change Section */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Lock className="w-4 h-4" />
                Change Password
              </CardTitle>
              <CardDescription className="text-sm">
                Update your account password to keep your account secure
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 p-4 sm:p-6">
              {passwordError && (
                <Alert variant="destructive">
                  <AlertCircle />
                  <AlertTitle>Password Change Failed</AlertTitle>
                  <AlertDescription>{passwordError}</AlertDescription>
                </Alert>
              )}
              
              <div className="space-y-2">
                <Label htmlFor="currentPassword" className="text-sm font-medium">Current Password</Label>
                <div className="relative">
                  <Input
                    id="currentPassword"
                    type={showCurrentPassword ? 'text' : 'password'}
                    value={passwordData.currentPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, currentPassword: e.target.value })}
                    placeholder="Enter current password"
                    className="h-10 sm:h-9 text-base sm:text-sm pr-10"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-10 sm:h-9 px-3 py-2 hover:bg-transparent"
                    onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                  >
                    {showCurrentPassword ? (
                      <EyeOff className="h-3 w-3" />
                    ) : (
                      <Eye className="h-3 w-3" />
                    )}
                  </Button>
                </div>
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="newPassword" className="text-sm font-medium">New Password</Label>
                <div className="relative">
                  <Input
                    id="newPassword"
                    type={showNewPassword ? 'text' : 'password'}
                    value={passwordData.newPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                    placeholder="Enter new password"
                    className="h-10 sm:h-9 text-base sm:text-sm pr-10"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-10 sm:h-9 px-3 py-2 hover:bg-transparent"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                  >
                    {showNewPassword ? (
                      <EyeOff className="h-3 w-3" />
                    ) : (
                      <Eye className="h-3 w-3" />
                    )}
                  </Button>
                </div>
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="confirmPassword" className="text-sm font-medium">Confirm New Password</Label>
                <div className="relative">
                  <Input
                    id="confirmPassword"
                    type={showConfirmPassword ? 'text' : 'password'}
                    value={passwordData.confirmPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
                    placeholder="Confirm new password"
                    className="h-10 sm:h-9 text-base sm:text-sm pr-10"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-10 sm:h-9 px-3 py-2 hover:bg-transparent"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  >
                    {showConfirmPassword ? (
                      <EyeOff className="h-3 w-3" />
                    ) : (
                      <Eye className="h-3 w-3" />
                    )}
                  </Button>
                </div>
              </div>

              <div className="pt-2">
                <Button 
                  onClick={handlePasswordChange} 
                  disabled={isChangingPassword || !passwordData.currentPassword || !passwordData.newPassword || !passwordData.confirmPassword}
                  className="w-full sm:w-auto h-11 sm:h-9"
                  size="sm"
                >
                {isChangingPassword ? (
                  <>
                    <div className="mr-2 h-3 w-3 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Changing...
                  </>
                ) : (
                  'Change Password'
                )}
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* API Key Section */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Key className="w-4 h-4" />
                API Key
              </CardTitle>
              <CardDescription className="text-sm">
                Generate and manage your API key for programmatic access
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 p-4 sm:p-6">
              {apiKey ? (
                <div className="space-y-3">
                  <Alert className="py-2">
                    <CheckCircle />
                    <AlertTitle>API Key Active</AlertTitle>
                    <AlertDescription className="text-xs">
                      API key is active and ready to use
                      {apiKeyCreatedAt && (
                        <span className="block text-xs text-muted-foreground mt-1">
                          Created on {apiKeyCreatedAt.toLocaleDateString()} at {apiKeyCreatedAt.toLocaleTimeString()}
                        </span>
                      )}
                    </AlertDescription>
                  </Alert>

                  <div className="space-y-2">
                    <Label className="text-sm font-medium">Your API Key</Label>
                    <div className="flex items-center gap-2">
                      <Input
                        value={apiKey}
                        readOnly
                        className="font-mono h-10 sm:h-9 text-sm break-all"
                      />
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={copyApiKey}
                            className="flex-shrink-0 h-10 sm:h-9 px-3 sm:px-2"
                          >
                            <Copy className="w-3 h-3" />
                          </Button>
                        </TooltipTrigger>
                        <TooltipContent>
                          <p>Copy API key to clipboard</p>
                        </TooltipContent>
                      </Tooltip>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Click the copy button to copy the full API key to your clipboard
                    </p>
                  </div>

                  <Button
                    variant="outline"
                    onClick={handleGenerateApiKey}
                    disabled={isGeneratingKey}
                    size="sm"
                    className="h-11 sm:h-9 w-full sm:w-auto"
                  >
                    {isGeneratingKey ? (
                      <>
                        <div className="mr-2 h-3 w-3 animate-spin rounded-full border-2 border-current border-t-transparent" />
                        Generating...
                      </>
                    ) : (
                      <>
                        <RefreshCw className="w-3 h-3 mr-2" />
                        Regenerate Key
                      </>
                    )}
                  </Button>
                </div>
              ) : (
                <div className="space-y-3">
                  <Alert className="py-2">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription className="text-xs">
                      You don't have an active API key. Generate one to access the API programmatically.
                    </AlertDescription>
                  </Alert>

                  <Button
                    onClick={handleGenerateApiKey}
                    disabled={isGeneratingKey}
                    size="sm"
                    className="h-11 sm:h-9 w-full sm:w-auto"
                  >
                    {isGeneratingKey ? (
                      <>
                        <div className="mr-2 h-3 w-3 animate-spin rounded-full border-2 border-current border-t-transparent" />
                        Generating...
                      </>
                    ) : (
                      <>
                        <Key className="w-3 h-3 mr-2" />
                        Generate API Key
                      </>
                    )}
                  </Button>
                </div>
              )}

              <div className="bg-muted p-4 rounded-lg">
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-3">
                  <h4 className="font-medium text-sm">API Usage Guidelines</h4>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 px-3 text-xs self-start sm:self-auto"
                    onClick={() => window.open('/api/docs', '_blank')}
                  >
                    View API Docs
                  </Button>
                </div>
                <ul className="text-xs sm:text-xs text-muted-foreground space-y-2 leading-relaxed">
                  <li>• Keep your API key secure and never share it publicly</li>
                  <li>• Use the API key in the Authorization header: <code className="bg-background px-1.5 py-0.5 rounded text-xs break-all">Bearer YOUR_API_KEY</code></li>
                  <li>• Regenerating your key will replace the old one and invalidate it</li>
                  <li>• API keys inherit your account permissions and assigned clients</li>
                </ul>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </ProtectedRoute>
  );
}