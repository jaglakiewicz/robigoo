/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Location } from '@angular/common';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { Subscription } from 'rxjs';
import { AuthService } from './services/auth.service';
import { NavigationService } from './services/navigation.service';
import { SVG_ICONS } from './shared/svg-icons';

interface Tab { id: number; type: string; titleKey: string; icon?: string; pinned?: boolean; instance?: number }
interface MenuItem { type: string; titleKey: string; hintKey: string }

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  isLoggedIn = false;
  sidebarOpen = false;
  private sidebarManuallyExpanded = false; // Track if user manually expanded sidebar
  tabs: Tab[] = [];
  activeIndex = -1; // -1 means home is active (no tab selected)
  private nextId = 1;
  
  darkMode = false;
  currentUserName = '';
  currentUserLogin = '';
  currentUserPermissionNumber = '';
  userMenuOpen = false;
  userAvatar: string | null = null;
  userChevronSvg = SVG_ICONS.chevronDown;
  SVG_ICONS = SVG_ICONS; // Make SVG_ICONS available in template
  private currentUserSub: Subscription;
  private navigationSub: Subscription | null = null;
  private sessionHeartbeatId: number | null = null;

  menuItems: MenuItem[] = [
    { type: 'new', titleKey: 'menu.newInspection', hintKey: 'menu.newInspectionHint' },
    { type: 'inspections', titleKey: 'menu.inspections', hintKey: 'menu.inspectionsHint' },
    { type: 'clients', titleKey: 'menu.clients', hintKey: 'menu.clientsHint' },
    { type: 'marks', titleKey: 'menu.marks', hintKey: 'menu.marksHint' },
    { type: 'notifications', titleKey: 'menu.notifications', hintKey: 'menu.notificationsHint' },
    { type: 'types', titleKey: 'menu.types', hintKey: 'menu.typesHint' },
    { type: 'settings', titleKey: 'menu.settings', hintKey: 'menu.settingsHint' }
  ];

  iconMap: { [key: string]: string } = SVG_ICONS;

  constructor(
    private sanitizer: DomSanitizer,
    private authService: AuthService,
    private navigationService: NavigationService,
    private location: Location
  ) {
    const saved = localStorage.getItem('theme');
    this.darkMode = saved === 'dark';
    this.applyTheme();
    this.sidebarOpen = this.shouldSidebarBeOpen();
    
    // Subscribe to auth service for user changes
    this.currentUserSub = this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUserName = `${user.firstName} ${user.lastName}`.trim();
        this.currentUserLogin = user.login;
        this.userAvatar = user.avatarBase64 ? `data:image/jpeg;base64,${user.avatarBase64}` : null;
        this.currentUserPermissionNumber = user.permissionNumber ?? '';
        this.isLoggedIn = true;
        this.sidebarOpen = this.shouldSidebarBeOpen();
        this.initializeHomeTabs();
        this.startSessionHeartbeat();
      } else {
        this.isLoggedIn = false;
        this.currentUserName = '';
        this.currentUserLogin = '';
        this.userAvatar = null;
        this.userMenuOpen = false;
        this.currentUserPermissionNumber = '';
        this.sidebarOpen = false;
        this.tabs = [];
        this.activeIndex = 0;
        this.stopSessionHeartbeat();
      }
    });
  }

  ngOnInit(): void {
    if (this.isLoggedIn) {
      this.startSessionHeartbeat();
    }

    // Listen for cross-component navigation requests
    this.navigationSub = this.navigationService.navigation$.subscribe(req => {
      this.openTab(req.tabType, req.tabTitleKey);
    });
  }

  ngOnDestroy(): void {
    this.currentUserSub?.unsubscribe();
    this.navigationSub?.unsubscribe();
    this.stopSessionHeartbeat();
  }

  @HostListener('document:click', ['$event'])
  handleDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.user-menu')) {
      this.userMenuOpen = false;
    }
  }

  applyTheme() {
    const html = document.documentElement;
    if (this.darkMode) {
      html.setAttribute('data-theme', 'dark');
    } else {
      html.setAttribute('data-theme', 'light');
    }
    localStorage.setItem('theme', this.darkMode ? 'dark' : 'light');
  }

  getSafeHtml(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }

  toggleTheme() {
    this.darkMode = !this.darkMode;
    this.applyTheme();
  }

  checkLogin() {
    // Already handled by authService subscription
  }

  handleLoginSuccess() {
    // Already handled by authService subscription
  }

  onLogout() {
    this.authService.logout();
  }

  toggle() { 
    this.sidebarOpen = !this.sidebarOpen;
    // Track if user manually expanded the sidebar
    if (this.sidebarOpen) {
      this.sidebarManuallyExpanded = true;
    } else {
      this.sidebarManuallyExpanded = false;
    }
  }

  /**
   * Initialize - start with home view (no tabs open)
   */
  initializeHomeTabs() {
    this.tabs = [];
    this.activeIndex = -1; // -1 means home view
  }

  /**
   * Navigate to home view
   */
  goHome() {
    this.activeIndex = -1;
    this.location.replaceState('/');
  }

  /**
   * Check if home view is active
   */
  isHomeActive(): boolean {
    return this.activeIndex === -1;
  }

  openTab(type: string, titleKey: string) {
    if (this.isMasterAdmin() && type !== 'settings') return;
    if (type === 'settings') {
      const existing = this.tabs.findIndex(t => t.type === 'settings');
      if (existing >= 0) { this.activeIndex = existing; this.location.replaceState('/settings'); return; }
    }
    const sameType = this.tabs.filter(t => t.type === type);
    const instance = sameType.length + 1;
    this.tabs.push({ id: this.nextId++, type, titleKey, icon: this.iconMap[type], instance });
    this.activeIndex = this.tabs.length - 1;
    this.location.replaceState('/' + type);
  }

  /**
   * Obsługa kliknięcia pozycji menu w sidebarze.
   * - Jeśli sidebar jest rozwinięty (open=true): zwija go do stanu przypięcia
   * - Jeśli sidebar jest przypięty (open=false): pozostaje przypięty
   */
  handleMenuClick(type: string, titleKey: string) {
    this.openTab(type, titleKey);

    try {
      const width = window.innerWidth;
      if (width && width <= 600) {
        // Mobile: zawsze zwijamy całkowicie
        this.sidebarOpen = false;
        this.sidebarManuallyExpanded = false;
      } else {
        // Desktop: jeśli sidebar jest rozwinięty, zwijamy go do stanu przypięcia
        if (this.sidebarOpen) {
          this.sidebarOpen = false;
          this.sidebarManuallyExpanded = false;
        }
        // Jeśli sidebar jest już przypięty (open=false), nie robimy nic
      }
    } catch {
      // Jeśli z jakiegoś powodu window nie jest dostępne, po prostu ignorujemy
    }
  }

  hasDuplicateType(type: string): boolean {
    return this.tabs.filter(t => t.type === type).length > 1;
  }

  isMasterAdmin(): boolean {
    return this.currentUserLogin === 'admin';
  }

  togglePinTab(i: number, event: Event) {
    event.stopPropagation();
    if (i >= 0 && i < this.tabs.length) {
      this.tabs[i].pinned = !this.tabs[i].pinned;
    }
  }
  activateTab(i: number) {
    this.activeIndex = i;
    if (i >= 0 && i < this.tabs.length) {
      this.location.replaceState('/' + this.tabs[i].type);
    }
  }

  closeTab(i: number) {
    if (i < 0 || i >= this.tabs.length) return;
    this.tabs.splice(i, 1);
    if (this.activeIndex >= this.tabs.length) {
      this.activeIndex = this.tabs.length > 0 ? this.tabs.length - 1 : -1;
    }
    const active = this.tabs[this.activeIndex];
    this.location.replaceState(active ? '/' + active.type : '/');
  }

  toggleUserMenu() {
    this.userMenuOpen = !this.userMenuOpen;
  }

  editUser() {
    this.userMenuOpen = false;
    const existing = this.tabs.findIndex(t => t.type === 'settings');
    if (existing >= 0) {
      this.activeIndex = existing;
      // Wysłanie sygnału do komponentu Settings aby otworzyć tab 1 (User Settings)
      const event = new CustomEvent('editUserSettings', { detail: { tab: 1 } });
      window.dispatchEvent(event);
    } else {
      this.tabs.push({ id: this.nextId++, type: 'settings', titleKey: 'menu.settings', icon: this.iconMap['settings'] });
      this.activeIndex = this.tabs.length - 1;
      // Opóźnienie aby component się załadował
      setTimeout(() => {
        const event = new CustomEvent('editUserSettings', { detail: { tab: 1 } });
        window.dispatchEvent(event);
      }, 100);
    }
  }

  get userInitials(): string {
    const source = this.currentUserName || this.currentUserLogin;
    if (!source) {
      return '?';
    }
    const parts = source.split(' ').filter(Boolean);
    if (!parts.length) {
      return source.substring(0, 2).toUpperCase();
    }
    return parts.slice(0, 2).map(p => p.charAt(0).toUpperCase()).join('');
  }

  get displayUserName(): string {
    return this.currentUserName || this.currentUserLogin || '';
  }

  reorderTabs(event: CdkDragDrop<Tab[]>) {
    if (!event || event.previousIndex === event.currentIndex) {
      return;
    }
    const activeTabId = this.tabs[this.activeIndex]?.id;
    moveItemInArray(this.tabs, event.previousIndex, event.currentIndex);
    if (activeTabId != null) {
      const newIndex = this.tabs.findIndex(t => t.id === activeTabId);
      this.activeIndex = newIndex >= 0 ? newIndex : 0;
    } else {
      this.activeIndex = event.currentIndex;
    }
  }

  private startSessionHeartbeat(): void {
    if (this.sessionHeartbeatId != null) {
      return;
    }

    try {
      this.sessionHeartbeatId = window.setInterval(() => {
        if (!this.isLoggedIn) {
          return;
        }

        // To wywołanie trafi na backend z aktualnym tokenem.
        // Jeśli sesja została przejęta / unieważniona, backend zwróci 401,
        // a AuthInterceptor zajmie się wylogowaniem i komunikatem.
        try {
          this.authService.me().subscribe({
            next: () => {},
            error: () => {}
          });
        } catch {
          // Ignoruj błędy na poziomie przeglądarki
        }
      }, 10000); // co 10 sekund
    } catch {
      this.sessionHeartbeatId = null;
    }
  }

  private stopSessionHeartbeat(): void {
    if (this.sessionHeartbeatId != null) {
      try {
        clearInterval(this.sessionHeartbeatId);
      } catch {
        // ignore
      }
      this.sessionHeartbeatId = null;
    }
  }

  private shouldSidebarBeOpen(): boolean {
    try {
      // On phones we treat the sidebar as a drawer; default closed there.
      const width = window.innerWidth;
      return !!width && width > 600;
    } catch {
      return false;
    }
  }

}
