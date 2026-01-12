"use client"

import { ChevronRight, type LucideIcon } from "lucide-react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { useState } from "react"

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"
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

export function NavMain({
  items,
}: {
  items: {
    title: string
    url: string
    icon?: LucideIcon
    isActive?: boolean
    items?: {
      title: string
      url: string
      icon?: LucideIcon
    }[]
  }[]
}) {
  const pathname = usePathname()
  const router = useRouter()
  const [hoveredItem, setHoveredItem] = useState<string | null>(null)

  const isItemActive = (url: string) => {
    if (url === "/") return pathname === "/"
    return pathname.startsWith(url)
  }

  const hasActiveSubItem = (subItems?: { title: string; url: string; icon?: LucideIcon }[]) => {
    if (!subItems) return false
    return subItems.some(subItem => isItemActive(subItem.url))
  }

  // Preload route on hover for faster navigation
  const handleHover = (url: string) => {
    setHoveredItem(url)
    router.prefetch(url)
  }

  const handleMouseLeave = () => {
    setHoveredItem(null)
  }

  return (
    <SidebarGroup>
      <SidebarGroupLabel>Platform</SidebarGroupLabel>
      <SidebarMenu>
        {items.map((item) => {
          const isActive = isItemActive(item.url) || hasActiveSubItem(item.items)
          
          if (!item.items) {
            // Single item without submenu
            return (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton 
                  asChild 
                  isActive={isActive} 
                  tooltip={item.title}
                  className="transition-all duration-300 ease-out"
                >
                  <Link 
                    href={item.url}
                    onMouseEnter={() => handleHover(item.url)}
                    onMouseLeave={handleMouseLeave}
                    className="group relative"
                  >
                    {item.icon && <item.icon className="transition-transform duration-200 group-hover:scale-110" />}
                    <span className="transition-all duration-200">{item.title}</span>
                    {hoveredItem === item.url && (
                      <div className="absolute inset-0 bg-sidebar-accent/20 rounded-md -z-10 animate-pulse" />
                    )}
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            )
          }

          // Item with submenu
          return (
            <Collapsible
              key={item.title}
              asChild
              defaultOpen={isActive}
              className="group/collapsible"
            >
              <SidebarMenuItem>
                <CollapsibleTrigger asChild>
                  <SidebarMenuButton 
                    tooltip={item.title} 
                    isActive={isActive}
                    className="transition-all duration-300 ease-out"
                  >
                    {item.icon && <item.icon className="transition-transform duration-200 group-hover:scale-110" />}
                    <span className="transition-all duration-200">{item.title}</span>
                    <ChevronRight className="ml-auto transition-transform duration-300 ease-out group-data-[state=open]/collapsible:rotate-90 group-hover:translate-x-1" />
                  </SidebarMenuButton>
                </CollapsibleTrigger>
                <CollapsibleContent>
                  <SidebarMenuSub>
                    {item.items?.map((subItem, index) => (
                      <SidebarMenuSubItem 
                        key={subItem.title}
                        style={{ 
                          animationDelay: `${index * 50}ms`,
                          '--item-index': index 
                        } as React.CSSProperties}
                        className="animate-in slide-in-from-left-2 duration-300 ease-out"
                      >
                        <SidebarMenuSubButton 
                          asChild 
                          isActive={isItemActive(subItem.url)}
                          className="transition-all duration-200 ease-out hover:translate-x-1"
                        >
                          <Link 
                            href={subItem.url}
                            onMouseEnter={() => handleHover(subItem.url)}
                            onMouseLeave={handleMouseLeave}
                            className="group relative"
                          >
                            {subItem.icon && <subItem.icon className="w-4 h-4 transition-transform duration-200 group-hover:scale-110" />}
                            <span className="transition-all duration-200">{subItem.title}</span>
                            {hoveredItem === subItem.url && (
                              <div className="absolute inset-0 bg-sidebar-accent/20 rounded-md -z-10 animate-pulse" />
                            )}
                          </Link>
                        </SidebarMenuSubButton>
                      </SidebarMenuSubItem>
                    ))}
                  </SidebarMenuSub>
                </CollapsibleContent>
              </SidebarMenuItem>
            </Collapsible>
          )
        })}
      </SidebarMenu>
    </SidebarGroup>
  )
}
