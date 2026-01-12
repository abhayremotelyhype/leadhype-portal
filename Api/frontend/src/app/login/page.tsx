'use client';

import { useEffect } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { Zap } from "lucide-react"
import { LoginForm } from "@/components/login-form"
import { useAuth } from "@/contexts/auth-context"

export default function LoginPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const { isAuthenticated, isLoading } = useAuth()

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      const redirectUrl = searchParams.get('redirect')
      if (redirectUrl) {
        router.replace(decodeURIComponent(redirectUrl))
      } else {
        router.replace('/')
      }
    }
  }, [isAuthenticated, isLoading, router, searchParams])

  if (isLoading) {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (isAuthenticated) {
    return null
  }

  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-6 bg-muted p-6 md:p-10">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <div className="flex items-center gap-2 self-center font-medium">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <Zap className="size-5" />
          </div>
          <span className="text-xl font-semibold">LeadHype</span>
        </div>
        <LoginForm />
      </div>
    </div>
  )
}
