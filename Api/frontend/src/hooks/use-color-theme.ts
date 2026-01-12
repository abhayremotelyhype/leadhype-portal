'use client'

import { useEffect, useState } from 'react'

export type ColorTheme = 'default' | 'mono'

export const colorThemes: { name: string; value: ColorTheme; description: string }[] = [
  { name: 'Default', value: 'default', description: 'Standard shadcn/ui theme' },
  { name: 'Mono', value: 'mono', description: 'Monospace and grayscale' },
]

export function useColorTheme() {
  const [currentTheme, setCurrentTheme] = useState<ColorTheme>('default')
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
    
    // Load theme from localStorage
    const savedTheme = (localStorage.getItem('color-theme') as ColorTheme) || 'default'
    if (colorThemes.some(theme => theme.value === savedTheme)) {
      setCurrentTheme(savedTheme)
      if (savedTheme !== 'default') {
        loadTheme(savedTheme)
      }
    }
  }, [])

  const loadTheme = (theme: ColorTheme) => {
    // Remove existing theme link
    const existingTheme = document.getElementById('color-theme-style')
    if (existingTheme) {
      existingTheme.remove()
    }

    // Add new theme if not default
    if (theme !== 'default') {
      const link = document.createElement('link')
      link.id = 'color-theme-style'
      link.rel = 'stylesheet'
      link.href = `/themes/${theme}.css`
      document.head.appendChild(link)
    }
  }

  const switchTheme = (theme: ColorTheme) => {
    setCurrentTheme(theme)
    loadTheme(theme)
    localStorage.setItem('color-theme', theme)
  }

  return {
    currentTheme: mounted ? currentTheme : 'default',
    switchTheme,
    colorThemes,
    mounted,
  }
}