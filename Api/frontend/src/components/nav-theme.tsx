"use client"

import * as React from "react"
import { useTheme } from "next-themes"
import { Moon, Sun, Monitor, Palette, Check } from "lucide-react"
import { useColorTheme } from "@/hooks/use-color-theme"

import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
} from "@/components/ui/sidebar"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"

export function NavTheme() {
  const { theme, setTheme } = useTheme()
  const { currentTheme, switchTheme, colorThemes } = useColorTheme()
  const [mounted, setMounted] = React.useState(false)

  React.useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) return null

  const getThemeIcon = () => {
    switch (theme) {
      case 'light':
        return Sun
      case 'dark':
        return Moon
      default:
        return Monitor
    }
  }

  const cycleTheme = () => {
    if (theme === 'light') {
      setTheme('dark')
    } else if (theme === 'dark') {
      setTheme('system')
    } else {
      setTheme('light')
    }
  }

  const cycleColorTheme = () => {
    const currentIndex = colorThemes.findIndex(t => t.value === currentTheme)
    const nextIndex = (currentIndex + 1) % colorThemes.length
    switchTheme(colorThemes[nextIndex].value)
  }

  const ThemeIcon = getThemeIcon()
  const currentColorTheme = colorThemes.find(t => t.value === currentTheme)

  return (
    <SidebarGroup>
      <SidebarGroupLabel>Appearance</SidebarGroupLabel>
      <SidebarMenu>
        {/* Light/Dark Mode Toggle */}
        <SidebarMenuItem>
          <SidebarMenuButton
            onClick={cycleTheme}
            tooltip={`Current: ${theme === 'system' ? 'System' : theme === 'light' ? 'Light' : 'Dark'} mode`}
          >
            <ThemeIcon />
            <span>Mode: {theme === 'system' ? 'System' : theme === 'light' ? 'Light' : 'Dark'}</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
        
        {/* Color Theme Toggle */}
        <SidebarMenuItem>
          <SidebarMenuButton
            onClick={cycleColorTheme}
            tooltip={`Current: ${currentColorTheme?.name || 'Default'} theme`}
          >
            <Palette />
            <span>Color: {currentColorTheme?.name || 'Default'}</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    </SidebarGroup>
  )
}