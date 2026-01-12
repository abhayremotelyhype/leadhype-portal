import { useSidebar } from "@/components/ui/sidebar"

export function useSafeSidebar() {
  try {
    const sidebar = useSidebar()
    return {
      ...sidebar,
      isAvailable: true,
    }
  } catch (error) {
    // Return safe defaults when sidebar context is not available
    return {
      state: 'expanded' as const,
      open: true,
      setOpen: () => {},
      openMobile: false,
      setOpenMobile: () => {},
      isMobile: false,
      toggleSidebar: () => {},
      isAvailable: false,
    }
  }
}