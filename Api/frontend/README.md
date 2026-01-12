# Smartlead Frontend - Next.js

A modern Next.js frontend for the Smartlead email campaign management system.

## 🚀 Features

- **Modern Stack**: Next.js 14 with App Router, TypeScript, Tailwind CSS
- **Dual Sorting**: Advanced sorting with count/percentage modes for stats columns
- **Dark Mode**: System, light, and dark theme support
- **Responsive Design**: Mobile-friendly interface
- **Real-time Notifications**: Toast notifications for user feedback
- **Type Safety**: Full TypeScript implementation

## 📋 Prerequisites

- Node.js 18+ 
- npm or yarn
- ASP.NET Core API running on port 5000

## 🛠️ Setup Instructions

1. **Install dependencies:**
   ```bash
   cd frontend
   npm install
   ```

2. **Start the development server:**
   ```bash
   npm run dev
   ```

3. **Open your browser:**
   ```
   http://localhost:3000
   ```

## 🏗️ Project Structure

```
src/
├── app/                 # Next.js App Router pages
│   ├── campaigns/       # Campaigns page
│   ├── email-accounts/  # Email accounts page
│   ├── clients/         # Clients page
│   └── layout.tsx       # Root layout
├── components/          # Reusable components
│   ├── ui/             # Base UI components
│   ├── sidebar.tsx     # Navigation sidebar
│   └── dual-sort-header.tsx # Dual sorting component
├── lib/                # Utilities and API client
│   └── api.ts          # API client and utilities
└── types/              # TypeScript type definitions
    └── index.ts        # Common types
```

## 🎯 Key Components

### DualSortHeader
Advanced sorting component that supports:
- Regular sorting (asc/desc)
- Dual sorting (count vs percentage)
- Visual indicators and tooltips
- Smooth animations

### API Client
Type-safe API client with:
- Automatic request/response handling
- Error handling
- TypeScript integration
- URL proxy to ASP.NET Core backend

## 🎨 Styling

- **Tailwind CSS**: Utility-first CSS framework
- **CSS Variables**: Theme-aware color system
- **Dark Mode**: Automatic theme switching
- **Responsive**: Mobile-first design

## 📱 Pages Implemented

### ✅ Completed
- [x] **Campaigns page** - Full dual sorting (count/percentage), search, pagination
- [x] **Email Accounts page** - Dual sorting for all stats columns, visual indicators  
- [x] **Clients page** - Standard sorting, search, responsive design
- [x] **Analytics page** - Dashboard with metrics cards and chart placeholders
- [x] **Settings page** - Theme switching, data sync settings, live preview
- [x] **Index/Home page** - Smartlead account management with modals
- [x] **Sidebar navigation** - Collapsible, mobile-responsive, active states
- [x] **Theme switching** - Light/dark mode with system preference
- [x] **Toast notifications** - Success, error, info messages
- [x] **Dual Sort Header** - Reusable component with gradient badges
- [x] **API Client** - Type-safe with error handling and proxy setup

### 🎯 Key Features Implemented
- **Dual Sorting System**: Seamlessly switch between count and percentage sorting
- **Visual Feedback**: Gradient badges, animations, toast notifications  
- **Type Safety**: Full TypeScript implementation with proper interfaces
- **Mobile Responsive**: Works perfectly on all screen sizes
- **Modern UI**: Clean shadcn/ui design system with Tailwind CSS

## 🔄 API Integration

The frontend automatically proxies API requests to your ASP.NET Core backend:
- Development: http://localhost:5000
- Production: Configurable via environment variables

## 🧪 Scripts

```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run start        # Start production server
npm run lint         # Run ESLint
npm run type-check   # TypeScript type checking
```

## 🎯 Next Steps

1. ✅ ~~Complete all main pages~~ **DONE** 
2. Add advanced modal dialogs for CRUD operations
3. Implement Chart.js integration for Analytics page  
4. Add form validation and error handling
5. Create unit tests for components
6. Optimize for production deployment
7. Add keyboard shortcuts and accessibility features

## 🤝 Migration from Legacy

This Next.js app maintains full compatibility with your existing ASP.NET Core API while providing:
- Better developer experience
- Modern React patterns
- Type safety
- Performance optimizations
- Mobile responsiveness