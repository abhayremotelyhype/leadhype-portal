"use client"

import * as React from "react"
import {
  Home,
  Mail,
  Users,
  Activity,
  Zap,
  Building2,
  AtSign,
  Send,
  Palette,
  Settings,
  Webhook,
} from "lucide-react"

import { NavMain } from "@/components/nav-main"
import { NavUser } from "@/components/nav-user"
import { NavTheme } from "@/components/nav-theme"
import { TeamSwitcher } from "@/components/team-switcher"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarRail,
} from "@/components/ui/sidebar"
import { useAuth } from "@/contexts/auth-context"

// LeadHype navigation data
const getNavigationData = (isAdmin: boolean, user: any) => ({
  user: {
    name: user?.username || user?.email?.split('@')[0] || "User",
    email: user?.email || "",
    avatar: "",
  },
  teams: [
    {
      name: "LeadHype",
      logo: Zap,
      plan: isAdmin ? "Admin" : "User",
    },
  ],
  navMain: [
    {
      title: "Dashboard",
      url: "/",
      icon: Home,
    },
    {
      title: "Email Accounts",
      url: "/email-accounts",
      icon: AtSign,
    },
    {
      title: "Campaigns",
      url: "/campaigns",
      icon: Send,
    },
    {
      title: "Clients",
      url: "/clients",
      icon: Building2,
    },
    {
      title: "Webhooks",
      url: "/webhooks",
      icon: Webhook,
    },
    ...(isAdmin ? [{
      title: "LeadHype Accounts",
      url: "/accounts",
      icon: Settings,
    }] : []),
    ...(isAdmin ? [{
      title: "User Management",
      url: "/users",
      icon: Users,
    }] : []),
  ],
})

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { user, isAdmin } = useAuth()
  
  const data = getNavigationData(isAdmin, user)

  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <TeamSwitcher teams={data.teams} />
      </SidebarHeader>
      <SidebarContent>
        <NavMain items={data.navMain} />
        <NavTheme />
      </SidebarContent>
      <SidebarFooter>
        <NavUser user={data.user} />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
