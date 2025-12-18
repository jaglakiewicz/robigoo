/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, HostListener, OnDestroy } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { Subscription } from 'rxjs';
import { TranslationService } from './i18n/translation.service';
import { AuthService } from './services/auth.service';
import { LANGUAGES, LanguageCode } from './i18n/translations';

interface Tab { id: number; type: string; titleKey: string; icon?: string; pinned?: boolean }
interface MenuItem { type: string; titleKey: string; hintKey: string }

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnDestroy {
  isLoggedIn = false;
  sidebarOpen = false;
  tabs: Tab[] = [];
  activeIndex = 0;
  private nextId = 1;
  
  darkMode = false;
  currentUserName = '';
  currentUserLogin = '';
  userMenuOpen = false;
  userAvatar: string | null = null;
  userChevronSvg = '<svg viewBox="0 0 243.626667 151.893333" width="16" height="16"><path d="M121.813333 151.893333 0 30.2933333 30.2933333 0 121.813333 91.7333333 213.333333 0 243.626667 30.2933333 121.813333 151.893333" fill="currentColor"/></svg>';
  private currentUserSub: Subscription;

  menuItems: MenuItem[] = [
    { type: 'new', titleKey: 'menu.newInspection', hintKey: 'menu.newInspectionHint' },
    { type: 'inspections', titleKey: 'menu.inspections', hintKey: 'menu.inspectionsHint' },
    { type: 'clients', titleKey: 'menu.clients', hintKey: 'menu.clientsHint' },
    { type: 'marks', titleKey: 'menu.marks', hintKey: 'menu.marksHint' },
    { type: 'notifications', titleKey: 'menu.notifications', hintKey: 'menu.notificationsHint' },
    { type: 'types', titleKey: 'menu.types', hintKey: 'menu.typesHint' },
    { type: 'stats', titleKey: 'menu.stats', hintKey: 'menu.statsHint' },
    { type: 'settings', titleKey: 'menu.settings', hintKey: 'menu.settingsHint' }
  ];

  languages = LANGUAGES;
  currentLanguage: LanguageCode;
  private langSub: Subscription;

  iconMap: { [key: string]: string } = {
    'new': '<svg viewBox="0 0 341.333333 341.333333" width="18" height="18"><path d="M170.666667,1.42108547e-14 C264.923264,-3.10380131e-15 341.333333,76.4100694 341.333333,170.666667 C341.333333,264.923264 264.923264,341.333333 170.666667,341.333333 C76.4100694,341.333333 2.57539587e-14,264.923264 1.42108547e-14,170.666667 C2.6677507e-15,76.4100694 76.4100694,3.15255107e-14 170.666667,1.42108547e-14 Z M170.666667,42.6666667 C99.9742187,42.6666667 42.6666667,99.9742187 42.6666667,170.666667 C42.6666667,241.359115 99.9742187,298.666667 170.666667,298.666667 C241.359115,298.666667 298.666667,241.359115 298.666667,170.666667 C298.666667,99.9742187 241.359115,42.6666667 170.666667,42.6666667 Z M192,85.3333333 L191.999333,149.333333 L256,149.333333 L256,192 L191.999333,191.999333 L192,256 L149.333333,256 L149.333333,191.999333 L85.3333333,192 L85.3333333,149.333333 L149.333333,149.333333 L149.333333,85.3333333 L192,85.3333333 Z" fill="currentColor"/></svg>',
    'inspections': '<svg viewBox="0 0 384 402" width="18" height="18"><path d="M266.666667,128 C331.468077,128 384,180.531923 384,245.333333 C384,270.026519 376.372036,292.938098 363.343919,311.840261 L423.228475,371.725253 L393.058586,401.895142 L333.173594,342.010585 C314.271431,355.038703 291.359852,362.666667 266.666667,362.666667 C201.865256,362.666667 149.333333,310.134744 149.333333,245.333333 C149.333333,180.531923 201.865256,128 266.666667,128 Z M266.666667,170.666667 C225.429405,170.666667 192,204.096072 192,245.333333 C192,286.570595 225.429405,320 266.666667,320 C307.903928,320 341.333333,286.570595 341.333333,245.333333 C341.333333,204.096072 307.903928,170.666667 266.666667,170.666667 Z M128.404239,234.665576 C128.136379,238.186376 128,241.743928 128,245.333333 C128,256.34762 129.284152,267.061976 131.710904,277.334851 L7.10542736e-15,277.333333 L7.10542736e-15,234.666667 L128.404239,234.665576 Z M85.3333333,1.42108547e-14 L85.3333333,213.333333 L21.3333333,213.333333 L21.3333333,1.42108547e-14 L85.3333333,1.42108547e-14 Z M170.666667,85.3333333 L170.663947,145.273483 C151.733734,163.440814 137.948238,186.928074 131.710904,213.331815 L106.666667,213.333333 L106.666667,85.3333333 L170.666667,85.3333333 Z M256,42.6666667 L255.999596,107.070854 C232.554315,108.854436 210.738728,116.46829 191.999452,128.465799 L192,42.6666667 L256,42.6666667 Z M341.333333,64 L341.333983,128.465865 C322.594868,116.468435 300.779487,108.854588 277.334424,107.070906 L277.333333,64 L341.333333,64 Z" fill="currentColor"/></svg>',
    'clients': '<svg viewBox="0 0 384 384" width="18" height="18"><path d="M298.666667,170.666667 C345.813333,170.666667 384,208.853333 384,256 L384,256 L384,384 L1.42108547e-14,384 L1.42108547e-14,298.666667 C1.42108547e-14,251.52 38.1866667,213.333333 85.3333333,213.333333 L85.3333333,213.333333 L160.853333,213.333333 C175.573333,187.733333 203.093333,170.666667 234.666667,170.666667 L234.666667,170.666667 Z M298.666667,213.333333 L234.666667,213.333333 C211.2,213.333333 192,232.533333 192,256 L192,256 L192,341.333333 L341.333333,341.333333 L341.333333,256 C341.333333,232.533333 322.133333,213.333333 298.666667,213.333333 L298.666667,213.333333 Z M149.333333,256 L85.3333333,256 C61.8666667,256 42.6666667,275.2 42.6666667,298.666667 L42.6666667,298.666667 L42.6666667,341.333333 L149.333333,341.333333 L149.333333,256 Z M106.666667,64 C141.952,64 170.666667,92.7146667 170.666667,128 C170.666667,163.285333 141.952,192 106.666667,192 C71.3813333,192 42.6666667,163.285333 42.6666667,128 C42.6666667,92.7146667 71.3813333,64 106.666667,64 Z M106.666667,104 C93.44,104 82.6666667,114.752 82.6666667,128 C82.6666667,141.248 93.44,152 106.666667,152 C119.893333,152 130.666667,141.248 130.666667,128 C130.666667,114.752 119.893333,104 106.666667,104 Z M266.666667,1.42108547e-14 C307.84,1.42108547e-14 341.333333,33.4933333 341.333333,74.6666667 C341.333333,115.84 307.84,149.333333 266.666667,149.333333 C225.493333,149.333333 192,115.84 192,74.6666667 C192,33.4933333 225.493333,1.42108547e-14 266.666667,1.42108547e-14 Z M266.666667,42.6666667 C249.024,42.6666667 234.666667,57.024 234.666667,74.6666667 C234.666667,92.3093333 249.024,106.666667 266.666667,106.666667 C284.309333,106.666667 298.666667,92.3093333 298.666667,74.6666667 C298.666667,57.024 284.309333,42.6666667 266.666667,42.6666667 Z" fill="currentColor"/></svg>',
    'marks': '<svg viewBox="0 0 448 298.666667" width="18" height="18"><path d="M448,2.84217094e-14 L448,298.666667 L106.666667,298.666667 L3.55271368e-15,149.333333 L106.666667,2.84217094e-14 L448,2.84217094e-14 Z M405.333333,42.6666667 L128.597333,42.6666667 L52.416,149.333333 L128.618667,256 L405.333333,256 L405.333333,42.6666667 Z M138.666667,117.333333 C156.339779,117.333333 170.666667,131.660221 170.666667,149.333333 C170.666667,167.006445 156.339779,181.333333 138.666667,181.333333 C120.993555,181.333333 106.666667,167.006445 106.666667,149.333333 C106.666667,131.660221 120.993555,117.333333 138.666667,117.333333 Z M213.333333,170.666667 L362.666667,170.666667 L362.666667,213.333333 L213.333333,213.333333 L213.333333,170.666667 Z M213.333333,85.3333333 L362.666667,85.3333333 L362.666667,128 L213.333333,128 L213.333333,85.3333333 Z" fill="currentColor"/></svg>',
    'notifications': '<svg viewBox="0 0 426 426" width="18" height="18"><path d="M213.013854,21.020167 C319.052526,21.020167 405.013854,106.981495 405.013854,213.020167 C405.013854,265.317935 384.104488,312.732059 350.189566,347.358729 L383.321027,404.745351 L346.37061,426.078684 L316.681804,374.65549 C286.780409,393.873084 251.198902,405.020167 213.013854,405.020167 C174.771065,405.020167 139.139696,393.839346 109.210297,374.568255 L79.3135646,426.351941 L42.3631474,405.018608 L75.7203109,347.23832 C41.874796,312.622182 21.0138542,265.257342 21.0138542,213.020167 C21.0138542,106.981495 106.975182,21.020167 213.013854,21.020167 Z M213.013854,63.6868337 C130.539332,63.6868337 63.6805208,130.545644 63.6805208,213.020167 C63.6805208,295.49469 130.539332,362.3535 213.013854,362.3535 C295.488377,362.3535 362.347187,295.49469 362.347187,213.020167 C362.347187,130.545644 295.488377,63.6868337 213.013854,63.6868337 Z M234.347187,106.3535 L234.347187,225.500167 L292.098799,283.268556 L261.928909,313.438445 L191.680521,243.190056 L191.680521,106.3535 L234.347187,106.3535 Z M355.032367,7.10542736e-15 C383.117284,18.7598588 407.283905,42.9259293 426.044403,71.0103829 L390.537168,94.6761076 C374.907194,71.2792434 354.774678,51.1459711 331.378448,35.5151218 L355.032367,7.10542736e-15 Z M70.9851521,0.01209835 L94.6571811,35.5151218 C71.2632279,51.1444505 51.1323522,71.2753263 35.5030235,94.6692795 L-2.84217094e-14,70.9972504 C18.7552254,42.9244324 42.912334,18.7673237 70.9851521,0.01209835 Z" fill="currentColor"/></svg>',
    'types': '<svg viewBox="0 0 426.666667 426.666667" width="18" height="18"><path d="M213.333333,7.10542736e-15 C330.959705,7.10542736e-15 426.666667,95.7069604 426.666667,213.333333 C426.666667,330.959705 330.959705,426.666667 213.333333,426.666667 C95.7069604,426.666667 7.10542736e-15,330.959705 7.10542736e-15,213.333333 C7.10542736e-15,95.7069604 95.7069604,7.10542736e-15 213.333333,7.10542736e-15 Z M213.333333,42.6666667 C118.892964,42.6666667 42.6666667,118.892964 42.6666667,213.333333 C42.6666667,307.773704 118.892964,384 213.333333,384 C307.773704,384 384,307.773704 384,213.333333 C384,118.892964 307.773704,42.6666667 213.333333,42.6666667 Z M214.247352,115.448129 C230.38415,136.430834 235.828347,162.243841 230.657609,181.089155 C230.011918,184.190056 252.755616,208.371626 298.888703,253.633865 C313.973648,268.71881 313.973648,283.803754 298.888703,298.888699 C284.691108,313.086294 270.493513,313.921446 256.295918,301.394157 L181.426839,230.319924 C162.579882,235.491139 136.767529,230.046951 115.785814,213.909668 C100.396892,193.674631 95.0886432,167.225458 99.3755574,148.268642 L132.19607,181.089155 L165.016583,164.678898 L181.426839,131.858386 L148.606327,99.0378728 C167.562553,94.7513198 194.012816,100.058863 214.247352,115.448129 Z" fill="currentColor"/></svg>',
    'stats': '<svg viewBox="0 0 384 384" width="18" height="18"><path d="M42.6666667,1.42108547e-14 L42.666,341.333 L384,341.333333 L384,384 L1.42108547e-14,384 L1.42108547e-14,1.42108547e-14 L42.6666667,1.42108547e-14 Z M290.80086,91.7065484 L382.469976,176.324193 L353.530024,207.675807 L306.517333,164.288 L263.90925,265.523605 L197.653333,204.8 L140.664173,309.333333 L64,309.333333 L64,266.666667 L115.328,266.666667 L186.323324,136.522922 L248.085333,193.130667 L290.80086,91.7065484 Z" fill="currentColor"/></svg>',
    'settings': '<svg viewBox="0 0 446 426.666667" width="18" height="18"><path d="M299.946667,7.10542736e-15 L299.946667,51.5413333 C308.650667,55.6586667 316.992,60.4586667 324.906667,65.92 L324.906667,65.92 L332.544,61.4826667 L369.514667,40.1706667 L390.848,77.0986667 L424.96,136.234667 L446.293333,173.184 L409.365333,194.517333 L401.728,198.890667 C402.154667,203.733333 402.368,208.533333 402.368,213.333333 C402.368,218.133333 402.154667,222.933333 401.728,227.776 L401.728,227.776 L409.365333,232.149333 L446.293333,253.482667 L424.96,290.432 L390.848,349.568 L369.514667,386.496 L332.544,365.184 L324.906667,360.746667 C316.992,366.208 308.650667,371.008 299.946667,375.125333 L299.946667,375.125333 L299.946667,426.666667 L146.368,426.666667 L146.368,375.125333 C137.664,371.008 129.322667,366.208 121.408,360.746667 L121.408,360.746667 L113.749333,365.184 L76.8,386.496 L55.4666667,349.568 L21.3333333,290.432 L7.10542736e-15,253.482667 L36.9493333,232.149333 L44.5653333,227.776 C44.16,222.933333 43.9466667,218.133333 43.9466667,213.333333 C43.9466667,208.533333 44.16,203.733333 44.5653333,198.890667 L44.5653333,198.890667 L36.9493333,194.517333 L7.10542736e-15,173.184 L21.3333333,136.234667 L55.4666667,77.0986667 L76.8,40.1706667 L113.749333,61.4826667 L121.408,65.92 C129.322667,60.4586667 137.664,55.6586667 146.368,51.5413333 L146.368,51.5413333 L146.368,7.10542736e-15 L299.946667,7.10542736e-15 Z M257.28,42.6666667 L189.034667,42.6666667 L189.034667,81.28 C164.650667,87.552 142.890667,100.288 125.781333,117.717333 L125.781333,117.717333 L92.416,98.432 L58.2826667,157.568 L91.6906667,176.853333 C88.4906667,188.48 86.6133333,200.682667 86.6133333,213.333333 C86.6133333,225.984 88.4906667,238.186667 91.6906667,249.813333 L91.6906667,249.813333 L58.2826667,269.098667 L92.416,328.234667 L125.781333,308.949333 C142.890667,326.378667 164.650667,339.114667 189.034667,345.386667 L189.034667,345.386667 L189.034667,384 L257.28,384 L257.28,345.386667 C281.664,339.114667 303.424,326.378667 320.533333,308.949333 L320.533333,308.949333 L353.877333,328.234667 L388.032,269.098667 L354.624,249.813333 C357.824,238.186667 359.701333,225.984 359.701333,213.333333 C359.701333,200.682667 357.824,188.48 354.624,176.853333 L354.624,176.853333 L388.032,157.568 L353.877333,98.432 L320.533333,117.717333 C303.424,100.288 281.664,87.552 257.28,81.28 L257.28,81.28 L257.28,42.6666667 Z M223.1552,128 C270.286608,128 308.488533,166.201925 308.488533,213.333333 C308.488533,260.464741 270.286608,298.666667 223.1552,298.666667 C176.023792,298.666667 137.821867,260.464741 137.821867,213.333333 C137.821867,166.201925 176.023792,128 223.1552,128 Z M223.1552,170.666667 C199.587941,170.666667 180.488533,189.766075 180.488533,213.333333 C180.488533,236.900592 199.587941,256 223.1552,256 C246.722459,256 265.821867,236.900592 265.821867,213.333333 C265.821867,189.766075 246.722459,170.666667 223.1552,170.666667 Z" fill="currentColor"/></svg>'
  };

  constructor(private sanitizer: DomSanitizer, private translation: TranslationService, private authService: AuthService) {
    const saved = localStorage.getItem('theme');
    this.darkMode = saved === 'dark';
    this.applyTheme();
    this.currentLanguage = this.translation.currentLanguage;
    this.langSub = this.translation.language$.subscribe(lang => {
      this.currentLanguage = lang;
    });
    
    // Subscribe to auth service for user changes
    this.currentUserSub = this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUserName = `${user.firstName} ${user.lastName}`.trim();
        this.currentUserLogin = user.login;
        this.userAvatar = user.avatarBase64 ? `data:image/jpeg;base64,${user.avatarBase64}` : null;
        this.isLoggedIn = true;
      } else {
        this.isLoggedIn = false;
        this.currentUserName = '';
        this.currentUserLogin = '';
        this.userAvatar = null;
        this.userMenuOpen = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
    this.currentUserSub?.unsubscribe();
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

  toggle() { this.sidebarOpen = !this.sidebarOpen; }

  openTab(type: string, titleKey: string) {
    // Master admin can only open settings
    if (this.isMasterAdmin() && type !== 'settings') {
      console.log('[AppComponent] Master admin cannot access:', type);
      return;
    }
    
    console.log('[AppComponent] openTab called with type:', type, 'titleKey:', titleKey);
    const existing = this.tabs.findIndex(t => t.type === type);
    if (existing >= 0) { 
      console.log('[AppComponent] Tab already exists at index:', existing);
      this.activeIndex = existing; 
      return; 
    }
    console.log('[AppComponent] Creating new tab with type:', type);
    this.tabs.push({ id: this.nextId++, type, titleKey, icon: this.iconMap[type] });
    this.activeIndex = this.tabs.length - 1;
    console.log('[AppComponent] New activeIndex:', this.activeIndex, 'Total tabs:', this.tabs.length);
    console.log('[AppComponent] Current tabs:', this.tabs);
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
  activateTab(i: number) { this.activeIndex = i; }

  closeTab(i: number) {
    if (i < 0 || i >= this.tabs.length) return;
    this.tabs.splice(i,1);
    if (this.activeIndex >= this.tabs.length) this.activeIndex = this.tabs.length - 1;
  }

  changeLanguage(language: LanguageCode) {
    this.translation.setLanguage(language);
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
}
